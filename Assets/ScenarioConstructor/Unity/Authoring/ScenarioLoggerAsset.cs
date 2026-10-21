using ScenarioConstructor.Execution;
using ScenarioConstructor.Journal;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    public abstract class ScenarioLoggerAsset : ScriptableObject
    {
        public abstract IScenarioLogger CreateLogger(ExecutionPlan plan, string runId);
    }
}
