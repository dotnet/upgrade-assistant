// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public class UpgradeStepOrderer : IUpgradeStepOrderer
    {
        private readonly ILogger<UpgradeStepOrderer> _logger;

        private readonly List<UpgradeStep> _steps;

        public IEnumerable<UpgradeStep> UpgradeSteps => _steps;

        public UpgradeStepOrderer(IEnumerable<UpgradeStep> steps, ILogger<UpgradeStepOrderer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _steps = Order(steps);
        }

        public bool TryAddStep(UpgradeStep newStep)
        {
            if (newStep is null)
            {
                throw new ArgumentNullException(nameof(newStep));
            }

            _logger.LogDebug("Attempting to add upgrade step {Id}", newStep.Id);

            // Find a place in the order after all dependencies
            var dependencies = newStep.DependsOn.ToList();
            var index = 0;
            for (index = 0; index < _steps.Count && dependencies.Any(); index++)
            {
                var currentId = _steps[index].Id;
                if (dependencies.Contains(currentId))
                {
                    dependencies.RemoveAll(d => d.Equals(currentId, StringComparison.Ordinal));
                }

                if (newStep.DependencyOf.Contains(currentId, StringComparer.Ordinal))
                {
                    _logger.LogError("Could not add dependency {UpgradeStep} because its dependent step {Dependent} is executed before all of its dependencies are satisfied", newStep.Id, currentId);
                    return false;
                }
            }

            if (dependencies.Any())
            {
                _logger.LogError("Could not add dependency {UpgradeStep} because dependencies were not satisfied: {Dependencies}", newStep.Id, string.Join(", ", dependencies));
                return false;
            }

            _steps.Insert(index, newStep);
            _logger.LogDebug("Inserted upgrade step {UpgradeStep} at index {Index}", newStep.Id, index);

            return true;
        }

        public bool TryRemoveStep(string stepId)
        {
            var step = _steps.Find(s => s.Id.Equals(stepId, StringComparison.Ordinal));

            if (step is null)
            {
                _logger.LogWarning("Cannot remove step {UpgradeStep} because the step was not found", stepId);
                return false;
            }

            var dependents = _steps.Where(s => s.DependsOn.Contains(stepId, StringComparer.Ordinal)).Select(s => s.Id);
            if (dependents.Any())
            {
                _logger.LogError("Cannot remove step {UpgradeStep} because later steps depend on it: {Dependents}", stepId, string.Join(", ", dependents));
                return false;
            }

            var ret = _steps.Remove(step);

            if (ret)
            {
                _logger.LogDebug("Removed upgrade step {UpgradeStep}", stepId);
            }
            else
            {
                _logger.LogError("There was an unexpected error removing upgrade step {UpgradeStep}", stepId);
            }

            return ret;
        }

        private List<UpgradeStep> Order(IEnumerable<UpgradeStep> steps)
        {
            foreach (var step in steps)
            {
                _logger.LogDebug("Using {Step} upgrade step", step.Id);
            }

            // Kahn's algorithm
            var orderedSteps = new List<UpgradeStep>();
            var inputSteps = steps.ToList();
            var dependencies = GetDependencies(steps);
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
                    if (inputSteps.Any(s => s.Id.Equals(missingDependency.Dependency, StringComparison.Ordinal)))
                    {
                        _logger.LogCritical("Upgrade step {UpgradeStep1} cannot run because it's dependency {UpgradeStep2} cannot run", missingDependency.Dependent, missingDependency.Dependency);
                    }
                    else
                    {
                        _logger.LogCritical("Upgrade step {DependentStep} cannot run because its dependency {DependencyStep} is not present", missingDependency.Dependent, missingDependency.Dependency);
                    }
                }

                throw new UpgradeException($"Cannot order upgrade steps due to {dependencies.Count} unsatisfiable dependencies: {string.Join(", ", dependencies)}");
            }
            else
            {
                _logger.LogDebug("Finished ordering upgrade steps: {UpgradeStepList}", string.Join(", ", orderedSteps.Select(s => s.Id)));
                return orderedSteps;
            }
        }

        private static List<UpgradeStepDependency> GetDependencies(IEnumerable<UpgradeStep> steps) =>
            steps.SelectMany(s => s.DependsOn.Select(d => new UpgradeStepDependency(d, s.Id)))
            .Concat(steps.SelectMany(s => s.DependencyOf.Select(d => new UpgradeStepDependency(s.Id, d))))
            .Distinct()
            .ToList();

        private static IEnumerable<UpgradeStep> GetStepsWithNoDependencies(List<UpgradeStep> steps, List<UpgradeStepDependency> dependencies) =>
            steps.Where(s => !dependencies.Any(d => d.Dependent.Equals(s.Id, StringComparison.Ordinal)));

        private record UpgradeStepDependency(string Dependency, string Dependent)
        {
            public override string ToString() => $"{Dependent}->{Dependency}";
        }
    }
}
