using ScenarioConstructor.Analysis;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Analysis/Training")]
    public sealed class TrainingAnalyzerAsset : ScenarioAnalyzerAsset
    {
        public override IScenarioAnalyzer CreateAnalyzer() => new TrainingAnalyzer();
    }
}
