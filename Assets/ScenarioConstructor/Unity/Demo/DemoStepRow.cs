using ScenarioConstructor.Execution;
using ScenarioConstructor.Unity.Authoring;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioConstructor.Unity.Demo
{
    public sealed class DemoStepRow : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Text detail;
        [SerializeField] private Image background;

        public ScenarioStepAsset Step { get; private set; }

        public void Bind(ScenarioStepAsset step)
        {
            Step = step;
            title.text = $"{step.StepId}  {step.Title}";
        }

        public void Refresh(StepProgress progress)
        {
            detail.text = $"{progress.Status}   |   Attempt {progress.AttemptNumber}   |   Errors {progress.Violations}";
            detail.color = ColorFor(progress.Status);
            background.color = progress.Status == StepStatus.Active
                ? new Color(0.12f, 0.23f, 0.33f) : new Color(0.075f, 0.12f, 0.19f);
        }

        public static Color ColorFor(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Active: return new Color(0.35f, 0.80f, 1f);
                case StepStatus.Completed: return new Color(0.35f, 0.87f, 0.65f);
                case StepStatus.Failed: return new Color(1f, 0.48f, 0.43f);
                case StepStatus.Skipped: return new Color(1f, 0.75f, 0.37f);
                default: return new Color(0.57f, 0.65f, 0.76f);
            }
        }
    }
}
