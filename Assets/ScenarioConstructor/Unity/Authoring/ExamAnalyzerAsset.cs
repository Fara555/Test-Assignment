using ScenarioConstructor.Analysis;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Analysis/Exam")]
    public sealed class ExamAnalyzerAsset : ScenarioAnalyzerAsset
    {
        public override IScenarioAnalyzer CreateAnalyzer() => new ExamAnalyzer();
    }
}
