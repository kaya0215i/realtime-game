using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.UI;
using static GameManager;

public class GameUIManager : MonoBehaviour {
    [SerializeField] private GameManager gameManager;

    // ゲームUI
    [SerializeField] private GameObject gameUI;
    // プレイヤーUI
    [SerializeField] private GameObject playerUI;
    // リザルトUI
    [SerializeField] private GameObject resultUI;

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
    [SerializeField] private TextMeshProUGUI rankingText;
    [SerializeField] private GameObject scoreBoadHeader;
    [SerializeField] private GameObject scoreBoadPanel;
    [SerializeField] private GameObject scorePrefab;
    [SerializeField] private Transform scoreParent;
    private Dictionary<Guid, GameObject> scoreObjectUIList = new Dictionary<Guid, GameObject>();

    // キルログ用
    [SerializeField] private GameObject killLogPrefab;
    [SerializeField] private Transform killLogParent;
    [SerializeField] private DeathCauseSO deathCauseSO;
    

    // リザルト用
    [SerializeField] private GameObject resultRankingPrefab;
    [SerializeField] private Transform resultRankingParent;

    [SerializeField] private GameObject gameSetText;

    // スマホ用スクリーンパッド
    [SerializeField] private GameObject screenPadUI;

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
                    gameTimerText.gameObject.SetActive(false);
                    scoreBoadPanel.SetActive(true);
                }
                if (Input.GetKeyUp(KeyCode.Tab)) {
                    gameTimerText.gameObject.SetActive(true);
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

        screenPadUI.SetActive(false);
        gameTimerText.gameObject.SetActive(false);
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

        screenPadUI.SetActive(true);
        gameTimerText.gameObject.SetActive(true);
        bulletAmountText.gameObject.SetActive(true);
        shotCoolTimeImage.gameObject.SetActive(true);
        hitPercentText.gameObject.SetActive(true);
    }

    /// <summary>
    /// キルログにプレイヤーを追加
    /// </summary>
    public void AddKillLog(Guid connectionId, Guid killerPlayerConnectionId, Death_Cause deathCause) {
        GameObject createdUI = Instantiate(killLogPrefab, parent: killLogParent);

        // テキストを設定
        Text[] texts = createdUI.GetComponentsInChildren<Text>(true);
        texts.First(text => text.gameObject.name == "KillerNameText").text = gameManager.CharacterList[killerPlayerConnectionId].joinedData.UserData.Display_Name.ToString();
        texts.First(text => text.gameObject.name == "DeathNameText").text = gameManager.CharacterList[connectionId].joinedData.UserData.Display_Name.ToString();

        // 死因画像設定
        Image image = createdUI.GetComponentsInChildren<Image>(true).First(text => text.gameObject.name == "DeathCauseImage");
        image.sprite = deathCauseSO.deathCauseDataList.FirstOrDefault(_ => _.name == deathCause.ToString()).image;

        Destroy(createdUI, 10);
    }

    /// <summary>
    /// スコアボードにプレイヤーを追加
    /// </summary>
    public void AddPlayerToScoreBoad(Guid connectionId) {
        GameObject createdUI = Instantiate(scorePrefab, parent: scoreParent);
        scoreObjectUIList[connectionId] = createdUI;

        // テキストを設定
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
        string myRnkText = "";
        foreach (var text in scoreObjectUIList) {
            Text[] texts = text.Value.GetComponentsInChildren<Text>(true);

            if (text.Key == gameManager.mySelf.ConnectionId) {
                myRnkText = (gameManager.ScoreList.FindIndex(ps => ps.ConnectionId == text.Key) + 1).ToString();
            }
            
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

        // ヘッダーのランキング反映
        if (myRnkText == "1") {
            myRnkText += "st";
        }
        else if (myRnkText == "2") {
            myRnkText += "nd";

        }
        else if (myRnkText == "3") {
            myRnkText += "rd";

        }
        else {
            myRnkText += "th";
        }
        rankingText.text = myRnkText;
    }

    /// <summary>
    /// スコアボードからプレイヤーを削除
    /// </summary>
    public void DeletePlayerFromScoreBoad(Guid connectionId) {
        Destroy(scoreObjectUIList[connectionId]);
        scoreObjectUIList.Remove(connectionId);

        UpdateScoreBoad();
    }

    /// <summary>
    /// ゲームセット
    /// </summary>
    public void GameSetUI() {
        gameUI.SetActive(false);
        playerUI.SetActive(false);
        scoreBoadPanel.SetActive(false);
        gameTimerText.gameObject.SetActive(false);
        screenPadUI.SetActive(false);

        gameSetText.SetActive(true);
    }

    /// <summary>
    /// ゲームリザルトUI表示
    /// </summary>
    public void GameResultUI() {
        gameSetText.GetComponent<RectTransform>().position = new Vector3(-400, 300, 0);
        resultUI.SetActive(true);

        // スコアを生成
        int index = 1;
        foreach (var item in gameManager.ScoreList) {
            GameObject createdUI = Instantiate(resultRankingPrefab, parent: resultRankingParent);
            TextMeshProUGUI rankingText = createdUI.GetComponentInChildren<TextMeshProUGUI>();
            Text[] texts = createdUI.GetComponentsInChildren<Text>(true);

            string rank = "";
            if (index == 1) {
                rank = index + "st";
            }
            else if(index == 2) {
                rank = index + "nd";
            }
            else if(index == 3) {
                rank = index + "rd";
            }
            else {
                rank = index + "th";
            }

            rankingText.text = rank;
            texts.First(_ => _.gameObject.name == "PlayerName").text = gameManager.CharacterList[item.ConnectionId].joinedData.UserData.Display_Name;
            texts.First(_ => _.gameObject.name == "ScoreText").text = item.Score.ToString();
            index++;
        }
    }
}
