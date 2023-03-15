// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.VisualBasic
{
    /// <summary>
    /// This analyzer will add &lt;VBRuntime&gt;Embed&lt;/VBRuntime&gt; to the vbproj to resolve compilation errors.
    /// </summary>
    /// Per https://github.com/dotnet/runtime/issues/30478#issuecomment-521270193
    public class EnableMyDotSupportSubStep : UpgradeStep
    {
        private readonly VisualBasicProjectUpdaterStep _parent;

        public override string Id => typeof(EnableMyDotSupportSubStep).FullName!;

        public override string Title => "Update vbproj to support \"My.\" namespace";

        public override string Description => "Modifies the vbproj file to address compilation errors.";

        public EnableMyDotSupportSubStep(VisualBasicProjectUpdaterStep vbProjectUpdaterStep, ILogger logger)
            : base(logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            ParentStep = _parent = vbProjectUpdaterStep ?? throw new ArgumentNullException(nameof(vbProjectUpdaterStep));
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            // VB updates don't apply until a project is selected
            if (context?.CurrentProject is null || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Task.FromResult(false);
            }

            if (context.CurrentProject.Language != Language.VisualBasic)
            {
                return Task.FromResult(false);
            }

            // applicable to Unit test projects and class libraries
            return Task.FromResult(context.CurrentProject.OutputType == ProjectOutputType.Library);
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var file = context.CurrentProject!.GetFile();
            if (DoesThisProjectEmbedVbRuntime(file))
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, $"VB updater \"{Id}\" has already been applied to {context.CurrentProject!.FileInfo}", BuildBreakRisk.None));
            }

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"VB updater \"{Id}\" needs to be applied to {context.CurrentProject!.FileInfo}", BuildBreakRisk.Unknown));
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var file = context.CurrentProject!.GetFile();

            // resolves errors
            // 1>vbc : error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.User' is not defined.
            // 1>vbc : error BC30002: Type 'Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue' is not defined.
            // 1>vbc : error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.User' is not defined.
            // 1>vbc : error BC30002: Type 'Global.Microsoft.VisualBasic.Devices.Computer' is not defined.
            // 1>vbc : error BC30002: Type 'Global.Microsoft.VisualBasic.ApplicationServices.ApplicationBase' is not defined.
            file.SetPropertyValue("VBRuntime", "Embed");
            await file.SaveAsync(token).ConfigureAwait(false);

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, string.Empty);
        }

        private static bool DoesThisProjectEmbedVbRuntime(IProjectFile file)
        {
            return string.IsNullOrWhiteSpace(file.GetPropertyValue("VBRuntime"));
        }

        /// <summary>
        /// Apply upgrade and update status as necessary.
        /// </summary>
        /// <param name="context">The upgrade context to apply this step to.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>True if the upgrade step was successfully applied or false if upgrade failed.</returns>
        public override async Task<bool> ApplyAsync(IUpgradeContext context, CancellationToken token)
        {
            var result = await base.ApplyAsync(context, token).ConfigureAwait(false);

            // Normally, the upgrader will apply steps one at a time at the user's instruction.
            // In the case of parent and child steps, the parent has any top-level application
            // done after the children. In the case of the VisualBasicProjectUpdaterStep, the
            // parent (this step's parent) doesn't need to apply anything.
            // Therefore, automatically apply the parent VisualBasicProjectUpdaterStep
            // once all its children have been applied.
            if (_parent.SubSteps.All(s => s.IsDone))
            {
                await _parent.ApplyAsync(context, token).ConfigureAwait(false);
            }

            return result;
        }
    }
}
