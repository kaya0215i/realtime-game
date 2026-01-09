using UnityEngine;
using UnityEngine.Events;

public class ColliderChecker : MonoBehaviour {
    public UnityEvent<Collision> onColliderEnter;
    public UnityEvent<Collision> onColliderExit;
    public UnityEvent<Collider> onTriggerEnter;
    public UnityEvent<Collider> onTriggerStay;
    public UnityEvent<Collider> onTriggerExit;

    private void OnCollisionEnter(Collision collision) {
        onColliderEnter?.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision) {
        onColliderExit?.Invoke(collision);
    }

    private void OnTriggerEnter(Collider other) {
        onTriggerEnter?.Invoke(other);
    }

    private void OnTriggerStay(Collider other) {
        onTriggerStay?.Invoke(other);
    }

    private void OnTriggerExit(Collider other) {
        onTriggerExit?.Invoke(other);
    }
}
