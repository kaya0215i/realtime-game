using Cysharp.Threading.Tasks;
using DG.Tweening.Core.Easing;
using realtime_game.Shared.Models.Entities;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour {
    [SerializeField] private TitleUIManager titleUIManager;
    private SaveManager saveManager;

    private async void Start() {
        saveManager = this.GetComponent<SaveManager>();

        titleUIManager.IsLoading(true);

        // データを読み込む
        await LoadDatas();

        // MagicOnionに接続
        await ConnectServer();

        titleUIManager.IsLoading(false);

        // 音量調整
        AudioManager.Instance.SetAllAudioSouceVolume();
    }

    /// <summary>
    /// データを読み込む
    /// </summary>
    private async UniTask LoadDatas() {
        await AudioManager.Instance.LoadData();
        await CharacterSettings.Instance.LoadData();
    }

    /// <summary>
    /// MagicOnionに接続
    /// </summary>
    private async UniTask ConnectServer() {
        // 接続
        await UserModel.Instance.CreateUserModel();
        await RoomModel.Instance.ConnectAsync();

        // データがあるか
        if (saveManager.LoadData() != null) {
            SaveData data = saveManager.LoadData();
            // 自動ログインが有効だったらする
            if (data.AutoLogin) {
                bool result = await UserModel.Instance.LoginUserAsync(data.LoginId, data.HashedPassword, true);

                if (result) {
                    Debug.Log("自動ログイン成功");
                    // ロビーシーンに移動
                    SceneManager.LoadScene("LobyScene");
                    return;
                }
                else {
                    Debug.Log("自動ログイン失敗");
                }
            }
        }
    }
}