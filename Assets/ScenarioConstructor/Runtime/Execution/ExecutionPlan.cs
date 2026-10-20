using System;
using System.Collections.Generic;

namespace ScenarioConstructor.Execution
{
    public enum PendingActionBehavior
    {
        RecordViolation,
        Ignore
    }

    public sealed class ExecutionPlan
    {
        public string ScenarioId { get; }
        public IReadOnlyList<StepDefinition> Steps { get; }
        public IReadOnlyList<StepSet> Groups { get; }
        public IReadOnlyList<StepSet> ActivationBatches { get; }
        public PendingActionBehavior PendingActions { get; }

        public ExecutionPlan(string scenarioId, IEnumerable<StepDefinition> steps,
            IEnumerable<StepSet> groups, IEnumerable<StepSet> activationBatches,
            PendingActionBehavior pendingActions = PendingActionBehavior.RecordViolation)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
                throw new ArgumentException("A scenario identifier is required.", nameof(scenarioId));
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));
            if (activationBatches == null)
                throw new ArgumentNullException(nameof(activationBatches));
            if (!Enum.IsDefined(typeof(PendingActionBehavior), pendingActions))
                throw new ArgumentOutOfRangeException(nameof(pendingActions));

            var stepList = new List<StepDefinition>(steps);
            var groupList = new List<StepSet>(groups);
            var batchList = new List<StepSet>(activationBatches);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in stepList)
            {
                if (step == null || !ids.Add(step.Id))
                    throw new ArgumentException("Steps must be nonnull with unique identifiers.", nameof(steps));
            }
            if (ids.Count == 0)
                throw new ArgumentException("A scenario must contain at least one step.", nameof(steps));

            ValidatePartition(groupList, ids, nameof(groups));
            ValidatePartition(batchList, ids, nameof(activationBatches));
            ValidateBatchOrder(groupList, batchList);

            ScenarioId = scenarioId;
            Steps = stepList.AsReadOnly();
            Groups = groupList.AsReadOnly();
            ActivationBatches = batchList.AsReadOnly();
            PendingActions = pendingActions;
        }

        private static void ValidatePartition(List<StepSet> sets, HashSet<string> expected, string parameter)
        {
            var setIds = new HashSet<string>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var set in sets)
            {
                if (set == null || !setIds.Add(set.Id))
                    throw new ArgumentException("Sets must be nonnull with unique identifiers.", parameter);
                foreach (var id in set.StepIds)
                {
                    if (!expected.Contains(id) || !seen.Add(id))
                        throw new ArgumentException($"Step '{id}' must appear exactly once in {parameter}.", parameter);
                }
            }
            if (!seen.SetEquals(expected))
                throw new ArgumentException($"Every step must appear in {parameter}.", parameter);
        }

        private static void ValidateBatchOrder(List<StepSet> groups, List<StepSet> batches)
        {
            var groupByStep = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < groups.Count; i++)
            {
                foreach (var id in groups[i].StepIds)
                    groupByStep.Add(id, i);
            }

            var previousGroup = -1;
            foreach (var batch in batches)
            {
                var group = groupByStep[batch.StepIds[0]];
                if (group < previousGroup)
                    throw new ArgumentException("Activation batches must follow group order.", nameof(batches));
                foreach (var id in batch.StepIds)
                {
                    if (groupByStep[id] != group)
                        throw new ArgumentException("An activation batch cannot span sequential groups.", nameof(batches));
                }
                previousGroup = group;
            }
        }
    }
}
