using ScenarioConstructor.Execution;
using ScenarioConstructor.Unity.Authoring;
using ScenarioConstructor.Unity.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioConstructor.Unity.Demo
{
    public sealed class DemoActionButton : MonoBehaviour
    {
        [SerializeField] private ScenarioStepAsset step;
        [SerializeField] private bool usesPhysics;
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        public void Refresh(ScenarioSession session, bool physicsBusy)
        {
            if (button == null)
                button = GetComponent<Button>();
            if (!session.TryGetProgress(step, out var progress))
            {
                button.interactable = false;
                return;
            }
            var status = progress.Status;
            button.interactable = (status == StepStatus.Active || status == StepStatus.Pending)
                && (!usesPhysics || !physicsBusy);
            var colors = button.colors;
            colors.normalColor = status == StepStatus.Active
                ? new Color(0.15f, 0.30f, 0.43f) : new Color(0.13f, 0.18f, 0.27f);
            button.colors = colors;
        }
    }
}
