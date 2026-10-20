using System;
using System.Collections.Generic;

namespace ScenarioConstructor.Journal
{
    public sealed class ScenarioJournal
    {
        public const int CurrentSchemaVersion = 1;

        public string ScenarioId { get; }
        public string RunId { get; }
        public int SchemaVersion { get; }
        public IReadOnlyList<string> StepIds { get; }
        public IReadOnlyList<ScenarioEvent> Events { get; }

        public ScenarioJournal(string scenarioId, string runId, IEnumerable<string> stepIds,
            IEnumerable<ScenarioEvent> events, int schemaVersion = CurrentSchemaVersion)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
                throw new ArgumentException("A scenario identifier is required.", nameof(scenarioId));
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("A run identifier is required.", nameof(runId));
            if (stepIds == null)
                throw new ArgumentNullException(nameof(stepIds));
            if (events == null)
                throw new ArgumentNullException(nameof(events));
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));

            var ids = new List<string>();
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in stepIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !uniqueIds.Add(id))
                    throw new ArgumentException("Step identifiers must be nonempty and unique.", nameof(stepIds));
                ids.Add(id);
            }
            if (ids.Count == 0)
                throw new ArgumentException("A scenario must contain at least one step.", nameof(stepIds));

            var entries = new List<ScenarioEvent>();
            foreach (var entry in events)
            {
                if (entry == null)
                    throw new ArgumentException("Journal entries cannot be null.", nameof(events));
                if (!uniqueIds.Contains(entry.StepId))
                    throw new ArgumentException($"Unknown step '{entry.StepId}'.", nameof(events));
                entries.Add(entry);
            }

            ScenarioId = scenarioId;
            RunId = runId;
            SchemaVersion = schemaVersion;
            StepIds = ids.AsReadOnly();
            Events = entries.AsReadOnly();
        }
    }
}
