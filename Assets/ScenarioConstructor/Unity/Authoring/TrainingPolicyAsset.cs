using ScenarioConstructor.Execution;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Execution/Training")]
    public sealed class TrainingPolicyAsset : ExecutionPolicyAsset
    {
        public override IExecutionPolicy CreatePolicy() => new TrainingExecutionPolicy();
    }
}
