using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using MSBuild.Abstractions;

namespace MSBuild.Conversion.Project
{
    public class Differ
    {
        private readonly IProject _project1;
        private readonly IProject _project2;

        public Differ(IProject project1, IProject project2)
        {
            _project1 = project1 ?? throw new ArgumentNullException(nameof(project1));
            _project2 = project2 ?? throw new ArgumentNullException(nameof(project2));
        }

        public PropertiesDiff GetPropertiesDiff()
        {
            var defaultedProps = ImmutableArray.CreateBuilder<IProjectProperty>();
            var notDefaultedProps = ImmutableArray.CreateBuilder<IProjectProperty>();
            var changedProps = ImmutableArray.CreateBuilder<(IProjectProperty, IProjectProperty)>();

            var propertiesInFile = _project1.Properties.Where(p => p.IsDefinedInProject).Select(p => p.Name).Distinct();

            foreach (var propInFile in propertiesInFile)
            {
                var originalEvaluatedProp = _project1.GetProperty(propInFile);
                if (originalEvaluatedProp == null)
                {
                    continue;
                }

                var newEvaluatedProp = _project2.GetProperty(propInFile);
                if (newEvaluatedProp is { })
                {
                    if (!originalEvaluatedProp.EvaluatedValue.Equals(newEvaluatedProp.EvaluatedValue, StringComparison.OrdinalIgnoreCase))
                    {
                        changedProps.Add((originalEvaluatedProp, newEvaluatedProp));
                    }
                    else
                    {
                        defaultedProps.Add(newEvaluatedProp);
                    }
                }
                else
                {
                    notDefaultedProps.Add(originalEvaluatedProp);
                }
            }

            return new PropertiesDiff(defaultedProps.ToImmutable(), notDefaultedProps.ToImmutable(), changedProps.ToImmutable());
        }

        public ImmutableArray<ItemsDiff> GetItemsDiff()
        {
            var oldItemGroups = from oldItem in _project1.Items group oldItem by oldItem.ItemType;
            var newItemGroups = from newItem in _project2.Items group newItem by newItem.ItemType;

            var addedRemovedGroups = from og in oldItemGroups
                                     from ng in newItemGroups
                                     where og.Key.Equals(ng.Key, StringComparison.OrdinalIgnoreCase)
                                     select new
                                     {
                                         ItemType = og.Key,
                                         DefaultedItems = ng.Intersect(og, ProjectItemComparer.MetadataComparer),
                                         IntroducedItems = ng.Except(og, ProjectItemComparer.IncludeComparer),
                                         NotDefaultedItems = og.Except(ng, ProjectItemComparer.IncludeComparer),
                                         ChangedItems = GetChangedItems(og, ng),
                                     };

            var builder = ImmutableArray.CreateBuilder<ItemsDiff>();

            foreach (var group in addedRemovedGroups)
            {
                var defaultedItems = group.DefaultedItems.ToImmutableArray();
                var notDefaultedItems = group.NotDefaultedItems.ToImmutableArray();
                var introducedItems = group.IntroducedItems.ToImmutableArray();
                var changedItems = group.ChangedItems.ToImmutableArray();

                var diff = new ItemsDiff(group.ItemType, defaultedItems, notDefaultedItems, introducedItems, changedItems);
                builder.Add(diff);
            }

            return builder.ToImmutable();
        }

        private IEnumerable<IProjectItem> GetChangedItems(IGrouping<string, IProjectItem> oldGroup, IGrouping<string, IProjectItem> newGroup)
        {
            var itemsWithSameInclude = newGroup.Intersect(oldGroup, ProjectItemComparer.IncludeComparer);
            var itemsWithSameMetadata = newGroup.Intersect(oldGroup, ProjectItemComparer.MetadataComparer);

            return itemsWithSameInclude.Except(itemsWithSameMetadata);
        }

        public void GenerateReport(string reportFilePath)
        {
            var report = new List<string>();
            report.AddRange(GetPropertiesDiff().GetDiffLines());

            var itemDiffs = GetItemsDiff();
            foreach (var diff in itemDiffs)
            {
                // Items that start with _ are private items. Not much value in reporting them.
                if (diff.ItemType.StartsWith("_"))
                {
                    continue;
                }

                report.AddRange(diff.GetDiffLines());
            }

            reportFilePath = string.IsNullOrWhiteSpace(reportFilePath) ? Directory.GetCurrentDirectory() : reportFilePath;
            var filePath = Path.Combine(reportFilePath, "report.txt");

            File.WriteAllLines(filePath, report);
        }
    }
}
