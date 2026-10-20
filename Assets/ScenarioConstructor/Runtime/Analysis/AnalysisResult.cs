using System.Collections.Generic;

namespace ScenarioConstructor.Analysis
{
    public sealed class AnalysisResult
    {
        public int Score { get; }
        public int MaximumScore { get; }
        public bool IsComplete { get; }
        public IReadOnlyList<string> Warnings { get; }

        internal AnalysisResult(int score, int maximumScore, bool isComplete, List<string> warnings)
        {
            Score = score;
            MaximumScore = maximumScore;
            IsComplete = isComplete;
            Warnings = new List<string>(warnings).AsReadOnly();
        }
    }
}
