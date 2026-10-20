using System;
using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Analysis
{
    public sealed class ExamAnalyzer : IScenarioAnalyzer
    {
        public AnalysisResult Analyze(ScenarioJournal journal)
        {
            var resolved = new ResolvedSteps(journal);
            long penalty = 0;

            foreach (var entry in resolved.Entries.Values)
            {
                if (entry.Outcome == StepOutcome.Failed)
                    penalty += 10;
                else if (entry.Outcome == StepOutcome.Skipped)
                    penalty += 5;
            }

            return new AnalysisResult((int)Math.Max(0, 100 - penalty), 100,
                resolved.IsComplete, resolved.Warnings);
        }
    }
}
