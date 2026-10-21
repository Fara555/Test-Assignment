using System;
using System.Collections;
using UnityEngine;

namespace ScenarioConstructor.Unity.Demo
{
    public sealed class DemoPhysicsRig : MonoBehaviour
    {
        [SerializeField] private Rigidbody actor;
        [SerializeField] private Transform home;
        [SerializeField] private Transform zoneA;
        [SerializeField] private Transform zoneB;
        [SerializeField] private Transform wallContact;
        private Coroutine movement;

        public bool IsBusy { get; private set; }
        public event Action BusyChanged;

        public void EnterA() => MoveTo(zoneA);
        public void EnterB() => MoveTo(zoneB);
        public void TouchWall() => MoveTo(wallContact);

        public void ResetRig()
        {
            ResetState();
            BusyChanged?.Invoke();
        }

        private void ResetState()
        {
            if (movement != null)
                StopCoroutine(movement);
            movement = null;
            IsBusy = false;
            Place(home.position);
        }

        private void OnDisable()
        {
            if (actor != null && home != null)
                ResetState();
        }

        private void MoveTo(Transform destination)
        {
            if (!isActiveAndEnabled || IsBusy)
                return;
            movement = StartCoroutine(Move(destination.position));
        }

        private IEnumerator Move(Vector3 destination)
        {
            IsBusy = true;
            BusyChanged?.Invoke();
            // Leave the previous collider for a physics tick so repeated visits generate a new entry.
            Place(home.position);
            yield return new WaitForFixedUpdate();
            Place(destination);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            movement = null;
            IsBusy = false;
            BusyChanged?.Invoke();
        }

        private void Place(Vector3 position)
        {
            actor.linearVelocity = Vector3.zero;
            actor.angularVelocity = Vector3.zero;
            actor.position = position;
            Physics.SyncTransforms();
        }
    }
}
