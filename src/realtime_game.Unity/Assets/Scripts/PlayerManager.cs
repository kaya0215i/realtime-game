using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class PlayerManager : MonoBehaviour {
    [SerializeField] private float moveSpeed;

    private void FixedUpdate() {
        float horizontal = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
        float vertical = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        transform.Translate(horizontal, 0, vertical);
    }
}