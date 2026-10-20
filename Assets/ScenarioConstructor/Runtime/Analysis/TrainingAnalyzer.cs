using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Analysis
{
    public sealed class TrainingAnalyzer : IScenarioAnalyzer
    {
        public AnalysisResult Analyze(ScenarioJournal journal)
        {
            var resolved = new ResolvedSteps(journal);
            var score = 0;

            foreach (var entry in resolved.Entries.Values)
            {
                if (entry.Outcome != StepOutcome.Completed)
                    continue;
                if (!entry.AttemptNumber.HasValue)
                {
                    resolved.Warnings.Add($"Step '{entry.StepId}' has no attempt number; it earns zero points.");
                    continue;
                }

                score += entry.AttemptNumber.Value == 1 ? 10 : entry.AttemptNumber.Value == 2 ? 5 : 0;
            }

            return new AnalysisResult(score, checked(journal.StepIds.Count * 10),
                resolved.IsComplete, resolved.Warnings);
        }
    }
}
