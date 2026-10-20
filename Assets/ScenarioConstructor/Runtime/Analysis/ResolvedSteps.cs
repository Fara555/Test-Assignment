using System;
using System.Collections.Generic;
using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Analysis
{
    internal sealed class ResolvedSteps
    {
        public Dictionary<string, ScenarioEvent> Entries { get; } =
            new Dictionary<string, ScenarioEvent>(StringComparer.Ordinal);
        public List<string> Warnings { get; } = new List<string>();
        public bool IsComplete { get; private set; }

        public ResolvedSteps(ScenarioJournal journal)
        {
            if (journal == null)
                throw new ArgumentNullException(nameof(journal));

            if (journal.SchemaVersion != ScenarioJournal.CurrentSchemaVersion)
                Warnings.Add($"Journal schema {journal.SchemaVersion} is not supported; verify the result.");

            foreach (var entry in journal.Events)
            {
                if (entry.EventType != ScenarioEvent.StepResolved)
                    continue;
                if (entry.Outcome == StepOutcome.None)
                {
                    Warnings.Add($"Step '{entry.StepId}' has a resolution without an outcome.");
                    continue;
                }
                if (Entries.ContainsKey(entry.StepId))
                {
                    Warnings.Add($"Step '{entry.StepId}' has multiple resolutions; the first is used.");
                    continue;
                }
                Entries.Add(entry.StepId, entry);
            }

            IsComplete = Entries.Count == journal.StepIds.Count;
            if (!IsComplete)
                Warnings.Add("The journal contains unresolved steps; the score is provisional.");
        }
    }
}
