using UnityEngine;

namespace ScenarioConstructor.Unity.Scene
{
    [DisallowMultipleComponent]
    public sealed class TriggerActionSource : MonoBehaviour
    {
        [SerializeField] private ScenarioActionRelay action;
        [SerializeField] private ColliderFilter filter = new ColliderFilter();

        private void OnTriggerEnter(Collider other)
        {
            if (isActiveAndEnabled && action != null && filter.Accepts(other))
                action.Raise();
        }
    }
}
