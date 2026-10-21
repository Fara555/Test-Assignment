using UnityEngine;
using UnityEngine.UI;

namespace ScenarioConstructor.Unity.Scene
{
    [RequireComponent(typeof(Button)), DisallowMultipleComponent]
    public sealed class ButtonActionSource : MonoBehaviour
    {
        [SerializeField] private ScenarioActionRelay action;
        private Button button;

        private void OnEnable()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            if (isActiveAndEnabled && action != null)
                action.Raise();
        }
    }
}
