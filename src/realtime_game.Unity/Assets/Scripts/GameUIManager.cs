using MessagePack.Resolvers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    // ヒットパーセントテキスト
    [SerializeField] private TextMeshProUGUI hitPercentText;


    // デスカメラ用
    [SerializeField] private GameObject deathCameraUI;
    [SerializeField] private Text killerNameText;
    [SerializeField] private Text reSpownBtnText;
    [NonSerialized] public bool isDeath = false;
    [NonSerialized] public float reSpownCoolTimer = 1;
    [NonSerialized] public int reSpownCoolTimeCountDown = 4;

    // スコアボード用
    [SerializeField] private GameObject scoreBoadHeader;
    [SerializeField] private GameObject scoreBoadPanel;
    [SerializeField] private GameObject scorePrefab;
    [SerializeField] private Transform scoreParent;
    private Dictionary<Guid, GameObject> scoreObjectUIList = new Dictionary<Guid, GameObject>();

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
            else {
                // スコアボード表示非表示
                if (Input.GetKeyDown(KeyCode.Tab)) {
                    scoreBoadPanel.SetActive(true);
                }
                if (Input.GetKeyUp(KeyCode.Tab)) {
                    scoreBoadPanel.SetActive(false);
                }

                // スコアボード更新
                UpdateScoreBoad();
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

        // スコアボード非表示
        scoreBoadHeader.SetActive(false);
        scoreBoadPanel.SetActive(false);

        // 自分を殺したプレイヤー名設定
        killerNameText.text = killerName;

        bulletAmountText.gameObject.SetActive(false);
        shotCoolTimeImage.gameObject.SetActive(false);
        hitPercentText.gameObject.SetActive(false);
    }

    /// <summary>
    /// デスカメラ非表示
    /// </summary>
    public void HideDeathCameraUI() {
        deathCameraUI.SetActive(false);

        // スコアボード表示
        scoreBoadHeader.SetActive(true);

        bulletAmountText.gameObject.SetActive(true);
        shotCoolTimeImage.gameObject.SetActive(true);
        hitPercentText.gameObject.SetActive(true);
    }

    /// <summary>
    /// スコアボードにプレイヤーを追加
    /// </summary>
    public void AddPlayerToScoreBoad(Guid connectionId) {
        GameObject createdUI = Instantiate(scorePrefab, parent: scoreParent);
        scoreObjectUIList[connectionId] = createdUI;

        Text[] texts = createdUI.GetComponentsInChildren<Text>(true);
        texts.First(text => text.gameObject.name == "RankingText").text = "1";
        texts.First(text => text.gameObject.name == "PlayerNameText").text = gameManager.CharacterList[connectionId].joinedData.UserData.Display_Name;
        texts.First(text => text.gameObject.name == "PlayerHitPercentText").text = "0%";
        texts.First(text => text.gameObject.name == "PlayerScoreText").text = "0";

        UpdateScoreBoad();
    }

    /// <summary>
    /// ヒットパーセントテキスト更新
    /// </summary>
    public void UpdateHitPercentText(float hitPercent) {
        hitPercentText.text = hitPercent + "%";
    }

    /// <summary>
    /// スコアボードを更新
    /// </summary>
    public void UpdateScoreBoad() {
        foreach (var text in scoreObjectUIList) {
            Text[] texts = text.Value.GetComponentsInChildren<Text>(true);
            texts.First(text => text.gameObject.name == "RankingText").text = (gameManager.ScoreList.FindIndex(ps => ps.ConnectionId == text.Key) + 1).ToString();
            if (gameManager.CharacterList[text.Key].playerObject != null) {
                texts.First(text => text.gameObject.name == "PlayerHitPercentText").text = gameManager.CharacterList[text.Key].playerObject.GetComponent<PlayerManager>().HitPercent + "%";
            }
            else {
                texts.First(text => text.gameObject.name == "PlayerHitPercentText").text = "0%";
            }
            texts.First(text => text.gameObject.name == "PlayerScoreText").text = gameManager.ScoreList.First(ps => ps.ConnectionId == text.Key).Score.ToString();

            text.Value.transform.SetSiblingIndex(gameManager.ScoreList.FindIndex(ps => ps.ConnectionId == text.Key));
        }
    }

    /// <summary>
    /// スコアボードからプレイヤーを削除
    /// </summary>
    public void DeletePlayerFromScoreBoad(Guid connectionId) {
        Destroy(scoreObjectUIList[connectionId]);
        scoreObjectUIList.Remove(connectionId);

        UpdateScoreBoad();
    }
}
