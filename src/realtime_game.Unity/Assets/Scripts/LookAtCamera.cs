using UnityEngine;

public class LookAtCamera : MonoBehaviour {
    private void Update() {
        this.transform.LookAt(Camera.main.transform);
    }
}
