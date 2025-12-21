using MessagePack.Resolvers;
using System;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour {
    [SerializeField] private GameManager gameManager;

    // ゲームのスタートタイマーテキスト
    [SerializeField] private Text gameStartTimerText; 

    // プレイヤー待ちテキスト
    [SerializeField] private Text waitPlayerJoinText;
    private int updateWaitPlayerJoinTextCount = 0;
    private float updateWaitPlayerJoinTextTimer = 0;


    // ゲームタイマーテキスト
    [SerializeField] private Text gameTimerText;


    // 弾の残弾
    [SerializeField] private Text bulletAmountText;
    // クールタイム
    [SerializeField] private Image shotCoolTimeImage;


    // デスカメラ用
    [SerializeField] private GameObject deathCameraUI;
    [SerializeField] private Text killerNameText;
    [SerializeField] private Text reSpownBtnText;
    [NonSerialized] public bool isDeath = false;
    [NonSerialized] public float reSpownCoolTimer = 1;
    [NonSerialized] public int reSpownCoolTimeCountDown = 4;

    private void Update() {
        // ゲームスタート中
        if (gameManager.IsGameStartShared) {
            // 死んでたら
            if (isDeath &&
                reSpownCoolTimeCountDown >= 1) {
                reSpownCoolTimer += Time.deltaTime;


                // リスポーンボタンのテキスト
                if (reSpownCoolTimer >= 1f) {
                    reSpownCoolTimer = 0;
                    reSpownCoolTimeCountDown--;

                    string reSpownBtnTextString = "復活まで : 3";

                    if (reSpownCoolTimeCountDown == 0) {
                        reSpownBtnTextString = "復活";
                    }
                    else {
                        reSpownBtnTextString = $"復活まで : {reSpownCoolTimeCountDown}";
                    }

                    // テキストに反映
                    reSpownBtnText.text = reSpownBtnTextString;
                }
            }
        }
        // ゲームスタート前
        else {
            // ゲームスタートタイマー更新
            UpdateGameStartTimer();

            // プレイヤー待ちテキスト
            UpdateWaitPlayerJoinText();
        }
    }

    /// <summary>
    /// ゲームスタートタイマー更新
    /// </summary>
    private void UpdateGameStartTimer() {
        if (gameManager.CharacterList.Count >= 2) {
            gameStartTimerText.gameObject.SetActive(true);
            gameStartTimerText.text = Mathf.FloorToInt(gameManager.StartTimer).ToString();
        }
        else {
            gameStartTimerText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// プレイヤー待ちテキスト更新
    /// </summary>
    private void UpdateWaitPlayerJoinText() {
        string text = "他のプレイヤーを待っています";

        updateWaitPlayerJoinTextTimer += Time.deltaTime;
        if (updateWaitPlayerJoinTextTimer >= 1f) {
            updateWaitPlayerJoinTextTimer = 0;
            updateWaitPlayerJoinTextCount++;
        }

        for (int i = 0; i < updateWaitPlayerJoinTextCount; i++) {
            text += ".";
        }

        text += $"({gameManager.CharacterList.Count} / 10)";

        // テキスト反映
        waitPlayerJoinText.text = text;

        if(updateWaitPlayerJoinTextCount == 3) {
            updateWaitPlayerJoinTextCount = 0;
        }
    }

    /// <summary>
    /// プレイヤー待ち中のUIを隠す
    /// </summary>
    public void HideWaitPlayerUI() {
        gameStartTimerText.gameObject.SetActive(false);
        waitPlayerJoinText.gameObject.SetActive(false);
    }

    /// <summary>
    /// ゲームタイマー更新
    /// </summary>
    public void UpdateGameTimer(float timer) {
        string timerText = "";

        if (Mathf.FloorToInt(timer / 60) < 10) {
            timerText += "0";
        }

        timerText += Mathf.FloorToInt(timer / 60).ToString();
        timerText += ":";

        if (Mathf.FloorToInt(timer % 60) < 10) {
            timerText += "0";
        }

        timerText += Mathf.FloorToInt(timer % 60).ToString();

        gameTimerText.text = timerText;
    }

    /// <summary>
    /// 弾数を更新
    /// </summary>
    public void UpdateBulletAmountText(int bulletAmount, int maxAmountText) {
        bulletAmountText.text = bulletAmount + " / " + maxAmountText;
    }

    /// <summary>
    /// 射撃間隔の画像
    /// </summary>
    public void UpdateShotCoolTimeImage(float fillAmount) {
        if(fillAmount >= 1) {
            shotCoolTimeImage.gameObject.SetActive(false);
        }
        else {
            if(!shotCoolTimeImage.gameObject.activeSelf) {
                shotCoolTimeImage.gameObject.SetActive(true);
            }
            shotCoolTimeImage.fillAmount = fillAmount;
        }
    }

    /// <summary>
    /// デスカメラ表示
    /// </summary>
    public void ShowDeathCameraUI(string killerName) {
        deathCameraUI.SetActive(true);

        // 自分を殺したプレイヤー名設定
        killerNameText.text = killerName;

        bulletAmountText.gameObject.SetActive(false);
        shotCoolTimeImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// デスカメラ非表示
    /// </summary>
    public void HideDeathCameraUI() {
        deathCameraUI.SetActive(false);

        bulletAmountText.gameObject.SetActive(true);
        shotCoolTimeImage.gameObject.SetActive(true);
    }
}
