using System;
using System.Collections.Generic;

namespace ScenarioConstructor.Analysis
{
    public sealed class AnalysisResult
    {
        public int Score { get; }
        public int MaximumScore { get; }
        public bool IsComplete { get; }
        public IReadOnlyList<string> Warnings { get; }

        public AnalysisResult(int score, int maximumScore, bool isComplete, IEnumerable<string> warnings = null)
        {
            if (maximumScore < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumScore));
            if (score < 0 || score > maximumScore)
                throw new ArgumentOutOfRangeException(nameof(score));
            Score = score;
            MaximumScore = maximumScore;
            IsComplete = isComplete;
            Warnings = new List<string>(warnings ?? Array.Empty<string>()).AsReadOnly();
        }
    }
}
