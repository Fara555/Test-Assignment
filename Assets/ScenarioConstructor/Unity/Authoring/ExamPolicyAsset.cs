using ScenarioConstructor.Execution;
using UnityEngine;

namespace ScenarioConstructor.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Scenario Constructor/Execution/Exam")]
    public sealed class ExamPolicyAsset : ExecutionPolicyAsset
    {
        [SerializeField, Min(1)] private int groupViolationLimit = 2;

        public override IExecutionPolicy CreatePolicy() => new ExamExecutionPolicy(groupViolationLimit);
    }
}
