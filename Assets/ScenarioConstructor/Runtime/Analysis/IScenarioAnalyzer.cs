using ScenarioConstructor.Journal;

namespace ScenarioConstructor.Analysis
{
    public interface IScenarioAnalyzer
    {
        AnalysisResult Analyze(ScenarioJournal journal);
    }
}
