// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Migrator
{
    public class MigrationStepOrderer : IMigrationStepOrderer
    {
        private readonly ILogger<MigrationStepOrderer> _logger;

        private readonly List<MigrationStep> _migrationSteps;

        public IEnumerable<MigrationStep> MigrationSteps => _migrationSteps;

        public MigrationStepOrderer(IEnumerable<MigrationStep> migrationSteps, ILogger<MigrationStepOrderer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _migrationSteps = Order(migrationSteps);
        }

        public bool TryAddStep(MigrationStep step)
        {
            if (step is null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            _logger.LogDebug("Attempting to add migration step {Id}", step.Id);

            // Find a place in the order after all dependencies
            var dependencies = step.DependsOn.ToList();
            var index = 0;
            for (index = 0; index < _migrationSteps.Count && dependencies.Any(); index++)
            {
                var currentId = _migrationSteps[index].Id;
                if (dependencies.Contains(currentId))
                {
                    dependencies.RemoveAll(d => d.Equals(currentId, StringComparison.Ordinal));
                }

                if (step.DependencyOf.Contains(currentId, StringComparer.Ordinal))
                {
                    _logger.LogError("Could not add dependency {MigrationStep} because its dependent step {Dependent} is executed before all of its dependencies are satisfied", step.Id, currentId);
                    return false;
                }
            }

            if (dependencies.Any())
            {
                _logger.LogError("Could not add dependency {MigrationStep} because dependencies were not satisfied: {Dependencies}", step.Id, string.Join(", ", dependencies));
                return false;
            }

            _migrationSteps.Insert(index, step);
            _logger.LogDebug("Inserted migration step {MigrationStep} at index {Index}", step.Id, index);

            return true;
        }

        public bool TryRemoveStep(string stepId)
        {
            var step = _migrationSteps.Find(s => s.Id.Equals(stepId, StringComparison.Ordinal));

            if (step is null)
            {
                _logger.LogWarning("Cannot remove step {MigrationStep} because the step was not found", stepId);
                return false;
            }

            var dependents = _migrationSteps.Where(s => s.DependsOn.Contains(stepId, StringComparer.Ordinal)).Select(s => s.Id);
            if (dependents.Any())
            {
                _logger.LogError("Cannot remove step {MigrationStep} because later steps depend on it: {Dependents}", stepId, string.Join(", ", dependents));
                return false;
            }

            var ret = _migrationSteps.Remove(step);

            if (ret)
            {
                _logger.LogDebug("Removed migration step {MigrationStep}", stepId);
            }
            else
            {
                _logger.LogError("There was an unexpected error removing migration step {MigrationStep}", stepId);
            }

            return ret;
        }

        private List<MigrationStep> Order(IEnumerable<MigrationStep> migrationSteps)
        {
            foreach (var step in migrationSteps)
            {
                _logger.LogDebug("Using {Step} migration step", step.Id);
            }

            // Kahn's algorithm
            var orderedSteps = new List<MigrationStep>();
            var inputSteps = migrationSteps.ToList();
            var dependencies = GetDependencies(migrationSteps);
            var stepsToAdd = GetStepsWithNoDependencies(inputSteps, dependencies);

            while (stepsToAdd.Any())
            {
                foreach (var step in stepsToAdd.OrderBy(s => s.Id))
                {
                    // Add the steps in alphabetical order
                    orderedSteps.Add(step);

                    // Remove steps from the inputSteps list once they've been added
                    inputSteps.Remove(step);

                    // Remove dependencies that depend on the added steps
                    dependencies.RemoveAll(d => d.Dependency.Equals(step.Id, StringComparison.Ordinal));
                }

                stepsToAdd = GetStepsWithNoDependencies(inputSteps, dependencies);
            }

            if (dependencies.Any())
            {
                // If an input steps aren't ordered, then either their dependencies are missing or there is a cycle
                foreach (var missingDependency in dependencies)
                {
                    if (inputSteps.Any(s => s.Id.Equals(missingDependency.Dependency)))
                    {
                        _logger.LogCritical("Migration step {MigrationStep1} cannot run because it's dependency {MigrationStep2} cannot run", missingDependency.Dependent, missingDependency.Dependency);
                    }
                    else
                    {
                        _logger.LogCritical("Migration step {DependentStep} cannot run because its dependency {DependencyStep} is not present", missingDependency.Dependent, missingDependency.Dependency);
                    }
                }

                throw new MigrationException($"Cannot order migration steps due to {dependencies.Count} unsatisfiable dependencies: {string.Join(", ", dependencies)}");
            }
            else
            {
                _logger.LogDebug("Finished ordering migration steps: {MigrationStepList}", string.Join(", ", orderedSteps.Select(s => s.Id)));
                return orderedSteps;
            }
        }

        private static List<MigrationStepDependency> GetDependencies(IEnumerable<MigrationStep> migrationSteps) =>
            migrationSteps.SelectMany(s => s.DependsOn.Select(d => new MigrationStepDependency(d, s.Id)))
            .Concat(migrationSteps.SelectMany(s => s.DependencyOf.Select(d => new MigrationStepDependency(s.Id, d))))
            .Distinct()
            .ToList();

        private static IEnumerable<MigrationStep> GetStepsWithNoDependencies(List<MigrationStep> steps, List<MigrationStepDependency> dependencies) =>
            steps.Where(s => !dependencies.Any(d => d.Dependent.Equals(s.Id, StringComparison.Ordinal)));

        private record MigrationStepDependency(string Dependency, string Dependent)
        {
            public override string ToString() => $"{Dependent}->{Dependency}";
        }
    }
}
