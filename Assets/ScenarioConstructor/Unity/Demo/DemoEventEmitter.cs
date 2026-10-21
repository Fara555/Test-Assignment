using UnityEngine;
using UnityEngine.Events;

namespace ScenarioConstructor.Unity.Demo
{
    public sealed class DemoEventEmitter : MonoBehaviour
    {
        [SerializeField] private UnityEvent onRaised = new UnityEvent();

        public UnityEvent OnRaised => onRaised;
        public void Raise() => onRaised.Invoke();
    }
}
