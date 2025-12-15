using DG.Tweening.Core.Easing;
using realtime_game.Shared.Models.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour {
    [SerializeField] private TitleUIManager titleUIManager;
    private SaveManager saveManager;

    private void Start() {
        saveManager = this.GetComponent<SaveManager>();
        ConnectServer();
    }

    /// <summary>
    /// MagicOnionに接続
    /// </summary>
    private async void ConnectServer() {
        titleUIManager.IsLoading(true);

        // 接続
        await UserModel.Instance.CreateUserModel();
        await RoomModel.Instance.ConnectAsync();

        // データがあるか
        if(saveManager.LoadData() != null) {
            SaveData data = saveManager.LoadData();
            // 自動ログインが有効だったらする
            if (data.AutoLogin) {
                bool result = await UserModel.Instance.LoginUserAsync(data.LoginId, data.HashedPassword, true);

                if (result) {
                    Debug.Log("自動ログイン成功");
                    SceneManager.LoadScene("LobyScene");
                    return;
                }
                else {
                    Debug.Log("自動ログイン失敗");
                }
            }
        }

        titleUIManager.IsLoading(false);
    }
}