using ScenarioConstructor.Analysis;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    public abstract class ScenarioAnalyzerAsset : ScriptableObject
    {
        public abstract IScenarioAnalyzer CreateAnalyzer();
    }
}
