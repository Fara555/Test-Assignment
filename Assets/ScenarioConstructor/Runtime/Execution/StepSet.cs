using System;
using System.Collections.Generic;

namespace ScenarioConstructor.Execution
{
    public sealed class StepSet
    {
        public string Id { get; }
        public IReadOnlyList<string> StepIds { get; }

        public StepSet(string id, params string[] stepIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A set identifier is required.", nameof(id));
            if (stepIds == null)
                throw new ArgumentNullException(nameof(stepIds));
            if (stepIds.Length == 0)
                throw new ArgumentException("A set must contain at least one step.", nameof(stepIds));

            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stepId in stepIds)
            {
                if (string.IsNullOrWhiteSpace(stepId) || !uniqueIds.Add(stepId))
                    throw new ArgumentException("Step identifiers must be nonempty and unique.", nameof(stepIds));
            }

            Id = id;
            StepIds = Array.AsReadOnly((string[])stepIds.Clone());
        }
    }
}
