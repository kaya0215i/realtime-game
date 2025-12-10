using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour {
    private void Start() {
        ConnectServer();
    }

    /// <summary>
    /// MagicOnion‚ÉÚ‘±
    /// </summary>
    private async void ConnectServer() {
        // Ú‘±
        await RoomModel.Instance.ConnectAsync();

        SceneManager.LoadScene("GameScene");
    }
}