using Cysharp.Threading.Tasks;
using DG.Tweening;
using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CharacterData;
using static NetworkObject;

public class GameManager : MonoBehaviour {
    // プレイヤーを生成する親Transform
    [SerializeField] private Transform playerObjectParent;
    // プレイヤーキャラのプレハブ
    [SerializeField] private GameObject characterPrefab;
    // プレイヤーキャラのリスト
    public Dictionary<Guid, UserDataAndObject> CharacterList { get; private set; } = new Dictionary<Guid, UserDataAndObject>();
    // オブジェクトのプレハブ
    [SerializeField] public ObjectDataSO objectDataSO;
    // オブジェクトのリスト
    private Dictionary<Guid, GameObject> objectList = new Dictionary<Guid, GameObject>();

    // 自分のプレイヤーキャラ
    private GameObject myCharacter;

    [SerializeField] private GameUIManager gameUIManager;

    private PlayerManager playerManager;
    private PlayerController playerController;

    // マウスカーソルを表示するか
    private bool isShowMouseCursor = false;

    [NonSerialized] public JoinedUser mySelf = new JoinedUser(); // 自分のユーザー情報を保存
    [NonSerialized] public bool isJoined = false;

    private float startTime = 30f;
    public float StartTimer { get; private set; }

    // サーバー通信用CancellationTokenSource
    private CancellationTokenSource serverAsyncCTS = new CancellationTokenSource();

    // プレイヤー待機時のスポーンポイント
    private Vector3 firstSpownPoint = Vector3.zero;

    // 自分を殺したプレイヤーのCinemachineCamera
    private CinemachineCamera killedPlyaerCamera;
    // 死因の列挙型
    public enum Death_Cause {
        None,
        Fall,
        Shot,
    }

    // プレイヤーのスコアリスト
    public List<PlayerScore> ScoreList { get; private set; } = new List<PlayerScore>();

    // ゲームプレイ可能か
    public bool IsPlaying { get; private set; } = true;

    // 勝者のコネクションID
    private Guid winnerConnectionId = Guid.Empty;

    private bool isSendStartAsync = false;
    private bool isSendEndAsync = false;

    /*
     * 共有用フィールド
     */
    public bool IsGameStartShared { get; private set; } = false;
    private float gameTimerShared = 0;

    private void Awake() {
        HideMouseCursor();

        mySelf.UserData = new User();

        StartTimer = startTime;
    }

    private async void Start() {
        // ユーザーが入退室したときにメソッドを実行するよう、モデルに登録しておく
        RoomModel.Instance.OnJoinedRoomUser += this.OnJoinedRoomUser;
        RoomModel.Instance.OnLeavedRoomUser += this.OnLeavedRoomUser;

        // ゲームの開始/終了通知
        RoomModel.Instance.OnGameStarted += this.OnGameStarted;
        RoomModel.Instance.OnGameEnded += this.OnGameEnded;

        // ゲームタイマー更新通知
        RoomModel.Instance.OnUpdatedGameTimer += this.OnUpdatedGameTimer;

        // ユーザーのTransfromを反映
        RoomModel.Instance.OnUpdatedTransformUser += this.OnUpdatedTransformUser;

        // オブジェクトが作成されたら
        RoomModel.Instance.OnCreatedObject += this.OnCreatedObject;

        // オブジェクトが破棄されたら
        RoomModel.Instance.OnDestroyedObject += this.OnDestroyedObject;

        // オブジェクトのTransformを反映
        RoomModel.Instance.OnUpdatedObjectTransform += this.OnUpdatedObjectTransform;

        // オブジェクトのIsInteractingをfalseに
        RoomModel.Instance.OnFalsedObjectInteracting += this.OnFalsedObjectInteracting;

        // キャラクタータイプ変更通知
        RoomModel.Instance.OnChangedCharacterTypeUser += this.OnChangedCharacterTypeUser;

        // プレイヤーのリスポーン/死亡通知
        RoomModel.Instance.OnReSpownedPlayer += this.OnReSpownedPlayer;
        RoomModel.Instance.OnDeadPlayer += this.OnDeadPlayer;

        // プレイヤーのヒットパーセント通知
        RoomModel.Instance.OnHitedPercentUser += this.OnHitedPercentUser;

        // 自分のキャラクターを作成
        myCharacter = Instantiate(characterPrefab, parent: playerObjectParent); // プレイヤーインスタンス生成
        myCharacter.transform.position = firstSpownPoint;
        playerManager = myCharacter.GetComponent<PlayerManager>();
        playerController = myCharacter.GetComponent<PlayerController>();
        playerController.cinemachineCamera.Priority = 10;

        // ルームに参加
        JoinRoom();
    }

    private void OnDisable() {
        if (RoomModel.Instance != null) {
            // 通知関連の登録解除
            RoomModel.Instance.OnJoinedRoomUser -= this.OnJoinedRoomUser;
            RoomModel.Instance.OnLeavedRoomUser -= this.OnLeavedRoomUser;
            RoomModel.Instance.OnGameStarted -= this.OnGameStarted;
            RoomModel.Instance.OnGameEnded -= this.OnGameEnded;
            RoomModel.Instance.OnUpdatedGameTimer -= this.OnUpdatedGameTimer;
            RoomModel.Instance.OnUpdatedTransformUser -= this.OnUpdatedTransformUser;
            RoomModel.Instance.OnCreatedObject -= this.OnCreatedObject;
            RoomModel.Instance.OnDestroyedObject -= this.OnDestroyedObject;
            RoomModel.Instance.OnUpdatedObjectTransform -= this.OnUpdatedObjectTransform;
            RoomModel.Instance.OnFalsedObjectInteracting -= this.OnFalsedObjectInteracting;
            RoomModel.Instance.OnChangedCharacterTypeUser -= this.OnChangedCharacterTypeUser;
            RoomModel.Instance.OnReSpownedPlayer -= this.OnReSpownedPlayer;
            RoomModel.Instance.OnDeadPlayer -= this.OnDeadPlayer;
            RoomModel.Instance.OnHitedPercentUser -= this.OnHitedPercentUser;
        }
    }

    private void OnDestroy() {
        OnDisable();
    }

    /// <summary>
    /// カーソル非表示
    /// </summary>
    public void HideMouseCursor() {
        // カーソルを画面中央にロックする
        Cursor.lockState = CursorLockMode.Locked;
        // カーソル非表示
        Cursor.visible = false;
    }

    /// <summary>
    /// カーソル表示
    /// </summary>
    public void ShowMouseCursor() {
        // カーソルのロックを解除
        Cursor.lockState = CursorLockMode.None;
        // カーソル表示
        Cursor.visible = true;
    }

    private async void Update() {
        // デバッグ用
        if (Input.GetKeyDown(KeyCode.F)) {
            string teamPlayerInfo = "{\n";
            foreach (var item in CharacterList) {
                teamPlayerInfo += "    [\n";
                teamPlayerInfo += "        ID : " + item.Value.joinedData.UserData.Id + "\n";
                teamPlayerInfo += "        名前 : " + item.Value.joinedData.UserData.Display_Name + "\n";
                teamPlayerInfo += "        接続ID : " + item.Value.joinedData.ConnectionId + "\n";
                teamPlayerInfo += "        参加順番 : " + item.Value.joinedData.JoinOrder + "\n";
                teamPlayerInfo += "        キャラクターオブジェクト : " + (item.Value.playerObject == null ? "None Object." : item.Value.playerObject.name) + "\n";
                teamPlayerInfo += "    ]\n";
            }
            teamPlayerInfo += "}\n";

            Debug.Log(
                "ルームID : " + "" + "\n" +
                "ルームの人数 : " + CharacterList.Count + "\n" +
                "ルームのプレイヤーのリスト \n" + teamPlayerInfo
                );

            string scoreList = "";
            foreach ( var item in ScoreList ) {
                scoreList += item.ConnectionId + "\n";
                scoreList += item.Score + "\n";
                scoreList += "\n";
            }
            Debug.Log(scoreList);
        }

        // マウスカーソルを表示非表示
        if (Input.GetKeyDown(KeyCode.Escape)) {
            isShowMouseCursor = isShowMouseCursor ? false : true;
            if (isShowMouseCursor) {
                ShowMouseCursor();
            }
            else {
                HideMouseCursor();
            }
        }

        // 2人以上ならスタートタイマー起動
        if (CharacterList.Count >= 2 &&
            !IsGameStartShared) {
            StartTimer -= Time.deltaTime;
        }
        else {
            StartTimer = startTime;
        }

        // マスタープレイヤー用の処理
        MasterPlayer();
    }

    /// <summary>
    /// マスタープレイヤー用の処理
    /// </summary>
    public async void MasterPlayer() {
        // マスタープレイヤー(JoinOrderが1)なら先の処理にいく
        if (mySelf.JoinOrder != 1) {
            return;
        }

        // スタートタイマーが0になったら
        if (StartTimer <= 0 &&
            !isSendStartAsync) {
            isSendStartAsync = true;

            StartTimer = startTime;
            await RoomModel.Instance.GameStartAsync();
        }

        // ゲームがスタートしてたら先の処理にいく
        if (!IsGameStartShared) {
            return;
        }

        // サーバーに一定間隔で送り続ける処理
        await SendServerAsync();

        // タイマーが0になったらゲーム終了
        if (gameTimerShared <= 0 &&
            !isSendEndAsync) {
            isSendEndAsync = true;

            await RoomModel.Instance.GameEndAsync();
        }
    }

    /// <summary>
    /// サーバーに一定間隔で送り続ける処理
    /// </summary>
    private async UniTask SendServerAsync() {
        // ゲームタイマー
        await RoomModel.Instance.UpdateGameTimerAsync(Time.deltaTime);

        // 0.2秒待つ
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: serverAsyncCTS.Token).SuppressCancellationThrow();
        if (serverAsyncCTS.Token.IsCancellationRequested) {

        }
    }

    /// <summary>
    /// キャラクターオブジェクトをコネクションIDで検索して返す
    /// </summary>
    public GameObject FindCharacterObject(Guid connectionId) {
         return CharacterList.FirstOrDefault( _ => _.Key == connectionId ).Value.playerObject;
    }

    /// <summary>
    /// 入室処理
    /// </summary>
    private async void JoinRoom() {
        try {
            // ユーザー情報を取得
            mySelf.UserData = await UserModel.Instance.GetUserByIdAsync(UserModel.Instance.UserId);
        }
        catch (Exception e) {
            Debug.LogError("GetUser failed");
            Debug.LogException(e);
            return;
        }

        // 自分のコネクションIDを保存
        mySelf.ConnectionId = await RoomModel.Instance.GetConnectionIdAsync();
        playerManager.thisCharacterConnectionId = mySelf.ConnectionId;

        // 参加
        await RoomModel.Instance.JoinRoomAsync();

        // インゲームの情報取得
        InGameData inGameData = await RoomModel.Instance.GetInGameDataAsync();
        if (inGameData != null) {
            // 始まってたら
            if (inGameData.isGameStart) {
                IsGameStartShared = true;
                gameUIManager.HideWaitPlayerUI();

                gameTimerShared = inGameData.gameTimer;
                // ゲームタイマー更新
                gameUIManager.UpdateGameTimer(gameTimerShared);
            }
        }

        // 現在のプレイヤーのステータスを取得して反映
        var userBattleDatas = await RoomModel.Instance.GetUserBattleDataAsync();
        foreach (var item in userBattleDatas) {
            CharacterList[item.Key].playerObject.GetComponent<PlayerManager>().HitPercent = item.Value.HitPercent;
            ScoreList.Find(_=> _.ConnectionId == item.Key).Score = item.Value.Score;
        }
        ScoreList = ScoreList.OrderBy(ps => -ps.Score).ToList();
    }

    /// <summary>
    /// 退室処理
    /// </summary>
    public async void LeaveRoom() {
        if (!isJoined) {
            return;
        }

        isJoined = false;

        await RoomModel.Instance.LeaveRoomAsync();

        // 自分以外のキャラクターを削除
        foreach (var character in CharacterList) {
            if (character.Key != mySelf.ConnectionId) {
                Destroy(character.Value.playerObject);
            }
        }

        mySelf = new JoinedUser();
        CharacterList = new Dictionary<Guid, UserDataAndObject>();

        // マウスカーソルを表示
        ShowMouseCursor();

        // ロビーシーンに戻る
        SceneManager.LoadScene("LobyScene");
    }


    /// <summary>
    /// [サーバー通知]
    /// ユーザーが入室したときの処理
    /// </summary>
    private void OnJoinedRoomUser(JoinedUser user) {
        // すでに表示済みのユーザーは追加しない
        if (CharacterList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分は生成しない
        if (user.UserData.Id == mySelf.UserData.Id) {
            isJoined = true;

            // フィールドで保持
            mySelf = user;

            myCharacter.name = "Player_" + user.JoinOrder;
            UserDataAndObject myDataAndObject = new UserDataAndObject() { joinedData = user, playerObject = myCharacter };
            CharacterList.Add(user.ConnectionId, myDataAndObject);

            // 頭上ヒットパーセントのレイヤーを変更
            myCharacter.GetComponentInChildren<Canvas>().gameObject.layer = LayerMask.NameToLayer("MyHitPercentUI");

            // スコアリストに追加
            ScoreList.Add(new PlayerScore() { ConnectionId = user.ConnectionId });
            gameUIManager.AddPlayerToScoreBoad(user.ConnectionId);

            return;
        }

        GameObject characterObject = Instantiate(characterPrefab, parent: playerObjectParent); // インスタンス生成
        characterObject.transform.position = firstSpownPoint;
        characterObject.GetComponent<PlayerManager>().thisCharacterConnectionId = user.ConnectionId;
        characterObject.name = "Player_" + user.JoinOrder;

        // フィールドで保持
        UserDataAndObject userDataAndObject = new UserDataAndObject() { joinedData = user, playerObject = characterObject };
        CharacterList.Add(user.ConnectionId, userDataAndObject);

        // スコアリストに追加
        ScoreList.Add(new PlayerScore() { ConnectionId = user.ConnectionId });
        gameUIManager.AddPlayerToScoreBoad(user.ConnectionId);

        ScoreList = ScoreList.OrderBy(ps => -ps.Score).ToList();

        Debug.Log("接続ID : " + user.ConnectionId + ", ユーザーID : " + user.UserData.Id + ", ユーザー名 : " + user.UserData.Display_Name + ", 参加順番 : " + user.JoinOrder);
    }

    /// <summary>
    /// [サーバー通知]
    /// ユーザーが退出したときの処理
    /// </summary>
    private void OnLeavedRoomUser(Guid connectionId, int joinOrder) {
        if (mySelf.ConnectionId == connectionId) {
            return;
        }

        // 参加順番を繰り下げ
        if (mySelf.JoinOrder > joinOrder) {
            mySelf.JoinOrder -= 1;
            myCharacter.name = "Player_" + joinOrder;
            CharacterList[mySelf.ConnectionId].playerObject.name = "Player_" + joinOrder;
        }

        // スコアリストから削除
        ScoreList.Remove(ScoreList.First(ps=>ps.ConnectionId == connectionId));
        gameUIManager.DeletePlayerFromScoreBoad(connectionId);

        Destroy(CharacterList[connectionId].playerObject);
        DOTween.Kill(CharacterList[connectionId]);
        CharacterList.Remove(connectionId);
    }

    /// <summary>
    /// [サーバー通知]
    /// ゲームスタート通知
    /// </summary>
    private void OnGameStarted() {
        Debug.Log("ゲームスタート");
        IsGameStartShared = true;
        gameUIManager.HideWaitPlayerUI();
    }

    /// <summary>
    /// [サーバー通知]
    /// ゲーム終了通知
    /// </summary>
    private void OnGameEnded() {
        Debug.Log("ゲーム終了");
        IsGameStartShared = false;
        serverAsyncCTS.Cancel();

        // マウスカーソルを表示
        ShowMouseCursor();

        IsPlaying = false;

        GameResult();
    }

    /// <summary>
    /// [サーバー通知]
    /// ゲームタイマーの更新通知
    /// </summary>
    private void OnUpdatedGameTimer(float timer) {
        gameTimerShared = timer;
        // ゲームタイマー更新
        gameUIManager.UpdateGameTimer(timer);
    }

    /// <summary>
    /// [サーバー通知]
    /// ユーザーのTransfromを反映
    /// </summary>
    private void OnUpdatedTransformUser(Guid connectionId, Vector3 pos, Quaternion rotate, Quaternion cameraRotate) {
        if (CharacterList.ContainsKey(connectionId) &&
            CharacterList[connectionId].playerObject != null) {
            CharacterList[connectionId].playerObject.transform.DOMove(pos, 0.2f).SetEase(Ease.InOutQuad);
            CharacterList[connectionId].playerObject.transform.DORotateQuaternion(rotate, 0.2f).SetEase(Ease.InOutQuad);

            CharacterList[connectionId].playerObject.GetComponent<PlayerManager>().cameraRotate = cameraRotate;
        }
    }


    /// <summary>
    /// オブジェクトをリストに追加
    /// </summary>
    public void AddObjectList(Guid objectId, GameObject obj) {
        objectList[objectId] = obj;
    }

    /// <summary>
    /// [サーバー通知]
    /// オブジェクトを作成
    /// </summary>
    private void OnCreatedObject(Guid connectionId, Guid objectId, int objectDataId, Vector3 pos, Quaternion rotate, UpdateObjectTypes updateType) {
        GameObject objData = objectDataSO.objectDataList.FirstOrDefault(_ => _.id == objectDataId).objectData;

        GameObject obj;

        if (objData.GetComponent<NetworkObject>().parentTransformName == "") {
            obj = Instantiate(objData, pos, rotate); // インスタンス生成
        }
        else {
            Transform parent = GameObject.Find(objData.GetComponent<NetworkObject>().parentTransformName).GetComponent<Transform>();
            obj = Instantiate(objData, pos, rotate, parent); // インスタンス生成
        }

        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        netObj.myObjectId = objectId;
        netObj.createrConnectionId = connectionId;
        netObj.updateType = updateType;

        objectList[objectId] = obj; // フィールドで保持
    }


    /// <summary>
    /// [サーバー通知]
    /// オブジェクトのフィールドを変更
    /// </summary>
    public void OnFalsedObjectInteracting(Guid objectId) {
        if(!objectList.ContainsKey(objectId)) {
            return;
        }

        objectList[objectId].GetComponent<NetworkObject>().FalseIsInteracting();
    }


    /// <summary>
    /// [サーバー通知]
    /// オブジェクトをリストから削除
    /// </summary>
    public void RemoveObjectList(Guid objectId) {
        if (!objectList.ContainsKey(objectId)) {
            return;
        }

        objectList.Remove(objectId);
    }

    /// <summary>
    /// [サーバー通知]
    /// オブジェクトを破棄
    /// </summary>
    private void OnDestroyedObject(Guid objectId) {
        if (!objectList.ContainsKey(objectId)) {
            return;
        }

        objectList[objectId].gameObject.GetComponent<NetworkObject>().sendDestroyMessage = false;
        Destroy(objectList[objectId].gameObject);
        objectList.Remove(objectId);
    }


    /// <summary>
    /// [サーバー通知]
    /// オブジェクトのTransformを反映
    /// </summary>
    private void OnUpdatedObjectTransform(Guid objectId, Vector3 pos, Quaternion rotate) {
        if (objectList.ContainsKey(objectId) &&
            objectList[objectId] != null) {
            objectList[objectId].transform.DOMove(pos, 0.2f).SetEase(Ease.InOutQuad);
            objectList[objectId].transform.DORotateQuaternion(rotate, 0.2f).SetEase(Ease.InOutQuad);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// キャラクタータイプ変更通知
    /// </summary>
    private void OnChangedCharacterTypeUser(Guid connectionId, PLAYER_CHARACTER_TYPE type) {
        if (!CharacterList.ContainsKey(connectionId)) {
            return;
        }

        // タイプを変更
        PlayerManager playerManager = CharacterList[connectionId].playerObject.GetComponent<PlayerManager>();
        playerManager.characterType = type;

        foreach (var player in CharacterList.Values) {
            Debug.Log(player.playerObject.name + " : " + player.playerObject.GetComponent<PlayerManager>().characterType);
        }
    }

    /// <summary>
    /// プレイヤーのリスポーン処理
    /// </summary>
    public async void ReSpown() {
        if (myCharacter != null ||
            gameUIManager.reSpownCoolTimeCountDown != 0) {
            return;
        }

        if (killedPlyaerCamera != null) {
            // 自分を殺したプレイヤーのカメラ優先度を元に戻す
            killedPlyaerCamera.Priority = 0;
        }

        HideMouseCursor();

        // ゲームUIの設定
        gameUIManager.isDeath = false;

        // デスカメラUIの非表示
        gameUIManager.HideDeathCameraUI();

        myCharacter = Instantiate(characterPrefab, parent: playerObjectParent); // プレイヤーインスタンス生成
        myCharacter.transform.position = Vector3.one;
        playerManager = myCharacter.GetComponent<PlayerManager>();
        playerController = myCharacter.GetComponent<PlayerController>();
        playerController.cinemachineCamera.Priority = 10;
        playerManager.thisCharacterConnectionId = mySelf.ConnectionId;
        myCharacter.name = "Player_" + mySelf.JoinOrder;

        // フィールドで保持
        CharacterList[mySelf.ConnectionId].playerObject = myCharacter;

        await RoomModel.Instance.ReSpownPlayerAsync();
    }

    /// <summary>
    /// [サーバー通知]
    /// プレイヤーのリスポーン通知
    /// </summary>
    private void OnReSpownedPlayer(Guid connectionId) {
        GameObject characterObject = Instantiate(characterPrefab, parent: playerObjectParent); // インスタンス生成
        characterObject.transform.position = Vector3.zero;
        characterObject.GetComponent<PlayerManager>().thisCharacterConnectionId = connectionId;
        characterObject.name = "Player_" + CharacterList[connectionId].joinedData.JoinOrder;

        // フィールドで保持
        CharacterList[connectionId].playerObject = characterObject;
    }

    /// <summary>
    /// プレイヤーの死亡処理
    /// </summary>
    public async void Dead(Guid killerPlayerConnectionId, Death_Cause deathCause) {
        ShowMouseCursor();

        // ゲームUIの設定
        gameUIManager.reSpownCoolTimer = 1;
        gameUIManager.reSpownCoolTimeCountDown = 4;
        gameUIManager.isDeath = true;

        // デスカメラ　滑らかに移動するように
        CinemachineBrain cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        cinemachineBrain.DefaultBlend = new(CinemachineBlendDefinition.Styles.EaseInOut, 2f);

        if (killerPlayerConnectionId != Guid.Empty) {
            // 自分を殺したプレイヤーの視点になるように優先度を設定
            killedPlyaerCamera = CharacterList[killerPlayerConnectionId].playerObject.GetComponent<PlayerController>().cinemachineCamera;
            killedPlyaerCamera.Priority = 3;
        }
        else {
            killerPlayerConnectionId = mySelf.ConnectionId;
        }

        // キルログに追加
        gameUIManager.AddKillLog(mySelf.ConnectionId, killerPlayerConnectionId, deathCause);

        // キャラクターを削除
        Destroy(myCharacter);

        // スコアを増やす
        AddScore(killerPlayerConnectionId, 1);
        // スコアを減らす
        SubScore(mySelf.ConnectionId, 1);

        // サーバーに送信
        await RoomModel.Instance.DeathPlayerAsync(killerPlayerConnectionId, deathCause);

        // カメラの移動を元に戻す
        cinemachineBrain.DefaultBlend = new(CinemachineBlendDefinition.Styles.Cut, 0f);

        // デスカメラUIの表示
        gameUIManager.ShowDeathCameraUI(CharacterList[killerPlayerConnectionId].joinedData.UserData.Display_Name);
    }

    /// <summary>
    /// [サーバー通知]
    /// プレイヤー死亡通知
    /// </summary>
    private void OnDeadPlayer(Guid connectionId, Guid killerPlayerConnectionId, Death_Cause deathCause) {
        // キルログに追加
        gameUIManager.AddKillLog(connectionId, killerPlayerConnectionId, deathCause);

        // キャラクターを削除
        Destroy(CharacterList[connectionId].playerObject);

        // スコアを増やす
        AddScore(killerPlayerConnectionId,1);
        // スコアを減らす
        SubScore(connectionId,1);
    }

    /// <summary>
    /// [サーバー通知]
    /// プレイヤーのヒットパーセント通知
    /// </summary>
    private void OnHitedPercentUser(Guid connectionId, float value) {
        if (!CharacterList.ContainsKey(connectionId)) {
            return;
        }

        // ヒットパーセントを変更
        PlayerManager playerManager = CharacterList[connectionId].playerObject.GetComponent<PlayerManager>();
        playerManager.HitPercent = value;
    }

    /// <summary>
    /// スコアを増やす
    /// </summary>
    public void AddScore(Guid connectionId, int value) {
        ScoreList.First(ps => ps.ConnectionId == connectionId).Score += value;
        ScoreList = ScoreList.OrderBy(ps => -ps.Score).ToList();
    }

    /// <summary>
    /// スコアを減らす
    /// </summary>
    public void SubScore(Guid connectionId, int value) {
        ScoreList.First(ps => ps.ConnectionId == connectionId).Score -= value;
        ScoreList = ScoreList.OrderBy(ps => -ps.Score).ToList();
    }

    /// <summary>
    /// ゲームリザルト表示
    /// </summary>
    private async void GameResult() {
        gameUIManager.GameSetUI();

        await FinalWinner();

        gameUIManager.GameResultUI();

        if (myCharacter != null) {
            myCharacter.transform.eulerAngles = Vector3.zero;
            playerController.resultCinemachineCamera.Priority = 100;
            playerController._head.eulerAngles = Vector3.zero;
        }
    }

    /// <summary>
    /// 勝者確定
    /// </summary>
    private async UniTask FinalWinner() {
        // 少し待ってから確定
        await UniTask.Delay(TimeSpan.FromSeconds(3));

        ScoreList = ScoreList.OrderBy(ps => -ps.Score).ToList();
        winnerConnectionId = ScoreList[0].ConnectionId;
    }
}
