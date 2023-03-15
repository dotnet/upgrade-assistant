// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Razor
{
    /// <summary>
    /// Upgrade step that updates cshtml source using sub-steps that
    /// analyze RazorCodeDocument objects and make necessary updates
    /// to cshtml files.
    /// </summary>
    public class RazorUpdaterStep : UpgradeStep
    {
        private RazorProjectEngine? _razorEngine;

        public override string Title => "Update Razor files";

        public override string Description => "Update Razor files using registered Razor updaters";

        public override string Id => WellKnownStepIds.RazorUpdaterStepId;

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            // Project should be backed up before changing source
            WellKnownStepIds.BackupStepId,

            // Template files should be added prior to changing source (since some code fixers will change added templates)
            WellKnownStepIds.TemplateInserterStepId,

            // Project should have correct TFM
            WellKnownStepIds.SetTFMStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.NextProjectStepId,
        };

        private Dictionary<string, RazorCodeDocument> _razorDocuments;

        public ImmutableArray<RazorCodeDocument> RazorDocuments => ImmutableArray.CreateRange(_razorDocuments.Values);

        public RazorUpdaterStep(IEnumerable<IUpdater<RazorCodeDocument>> razorUpdaters, ILogger<RazorUpdaterStep> logger)
            : base(logger)
        {
            if (razorUpdaters is null)
            {
                throw new ArgumentNullException(nameof(razorUpdaters));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _razorDocuments = new Dictionary<string, RazorCodeDocument>();

            // Add a sub-step for each Razor updater
            SubSteps = new List<UpgradeStep>(razorUpdaters.Select(updater => new RazorUpdaterSubStep(this, updater, logger)));
        }

        /// <summary>
        /// Determines whether the RazorUpdaterStep applies to a given context. This doesn't
        /// consider whether there's actually any work to be done by Razor updaters. Rather, it
        /// determines whether it makes sense to even initialize the Razor updaters. So, it
        /// will return false if there are no cshtml files in the current project or if there
        /// are no Razor updaters available.
        /// </summary>
        /// <param name="context">The context to evaluate.</param>
        /// <param name="token">A token that can be used to cancel execution.</param>
        /// <returns>True if the Razor updater step might apply, false otherwise.</returns>
        protected override async Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // The RazorUpdaterStep is only applicable when a project is loaded
            if (context?.CurrentProject is null || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            // The RazorUpdaterStep is only applicable when the project contans Razor files to update
            if (!GetRazorFileSystem(context.CurrentProject.Required()).EnumerateItems("/").Any())
            {
                return false;
            }

            // The RazorUpdaterStep is applicable if it contains at least one applicable substep
            foreach (var subStep in SubSteps)
            {
                if (await subStep.IsApplicableAsync(context, token).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        protected override async Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _razorEngine = RazorProjectEngine.Create(RazorConfiguration.Default, GetRazorFileSystem(context.CurrentProject.Required()), builder =>
            {
                // Add useful "MVC View-isms" like _ViewImports support, page models, etc.
                // https://github.com/dotnet/aspnetcore/blob/main/src/Razor/Microsoft.AspNetCore.Mvc.Razor.Extensions/src/RazorExtensions.cs
                RazorExtensions.Register(builder);
            });

            // Process all Razor documents initially
            // SubSteps can call ProcessRazorDocuments with specific files that have changed later to re-process specific documents
            ProcessRazorDocuments(null);

            foreach (var step in SubSteps)
            {
                await step.InitializeAsync(context, token).ConfigureAwait(false);
            }

            var incompleteSubSteps = SubSteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "No Razor updaters need applied", BuildBreakRisk.None)
                : new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} Razor updaters need applied", SubSteps.Where(s => !s.IsDone).Max(s => s.Risk));
        }

        public override UpgradeStepInitializeResult Reset()
        {
            _razorEngine = null;
            _razorDocuments.Clear();
            return base.Reset();
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // Updates are made in sub-steps, so no changes need made in this apply step.
            // Just check that sub-steps executed correctly.
            var incompleteSubSteps = SubSteps.Count(s => !s.IsDone);

            return incompleteSubSteps == 0
                ? Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty))
                : Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Incomplete, $"{incompleteSubSteps} Razor updaters need applied"));
        }

        /// <summary>
        /// Processes Razor documents, generating Razor syntax trees and generated code.
        /// </summary>
        /// <param name="pathsToProcess">Absolute paths of the Razor documents to process or null to process all Razor documents in the project.</param>
        public void ProcessRazorDocuments(IEnumerable<string>? pathsToProcess)
        {
            if (_razorEngine is null)
            {
                Logger.LogError("Razor documents cannot be retrieved prior to initializing RazorUpdaterStep");
                throw new InvalidOperationException("Razor documents cannot be retrieved prior to initializing RazorUpdaterStep");
            }

            var files = _razorEngine.FileSystem.EnumerateItems("/");
            if (pathsToProcess is not null)
            {
                files = files.Where(f => pathsToProcess.Contains(f.PhysicalPath));
            }

            Logger.LogTrace("Generating Razor code documents for {RazorCount} files", files.Count());

            foreach (var file in files)
            {
                _razorDocuments[file.RelativePhysicalPath] = _razorEngine.Process(file);
            }
        }

        private static RazorProjectFileSystem GetRazorFileSystem(IProject project) =>
            RazorProjectFileSystem.Create(project.FileInfo.DirectoryName);
    }
}
