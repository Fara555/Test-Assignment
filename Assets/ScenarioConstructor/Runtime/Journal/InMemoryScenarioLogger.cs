using System;
using System.Collections.Generic;

namespace ScenarioConstructor.Journal
{
    public sealed class InMemoryScenarioLogger : IScenarioLogger
    {
        private readonly ScenarioJournal definition;
        private readonly HashSet<string> stepIds;
        private readonly List<ScenarioEvent> entries = new List<ScenarioEvent>();

        public InMemoryScenarioLogger(string scenarioId, string runId, IEnumerable<string> stepIds)
        {
            definition = new ScenarioJournal(scenarioId, runId, stepIds, Array.Empty<ScenarioEvent>());
            this.stepIds = new HashSet<string>(definition.StepIds, StringComparer.Ordinal);
        }

        public void Record(ScenarioEvent entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (!stepIds.Contains(entry.StepId))
                throw new ArgumentException($"Unknown step '{entry.StepId}'.", nameof(entry));

            entries.Add(entry);
        }

        public ScenarioJournal Capture()
        {
            return new ScenarioJournal(definition.ScenarioId, definition.RunId, definition.StepIds, entries);
        }
    }
}
