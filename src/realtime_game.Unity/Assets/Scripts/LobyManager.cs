using Cysharp.Threading.Tasks;
using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class LobyManager : MonoBehaviour {
    [SerializeField] private LobyUIManager lobyUIManager;

    [NonSerialized] public JoinedUser mySelf; // 自分のユーザー情報を保存

    // ロビー内のユーザーのリスト
    private Dictionary<Guid, LobyUserData> lobyUserList = new Dictionary<Guid, LobyUserData>();

    // プレイヤーを生成する親Transform
    private Transform playerObjectParent;
    // ロビーに生成するプレイヤープレハブ
    [SerializeField] private GameObject playerPrefab;
    // プレイヤーを生成する場所の土台
    [SerializeField] private List<Transform> playerStandList;

    // チームid
    public Guid MyTeamId {  get; private set; }

    // チームプレイヤーのリスト
    public Dictionary<Guid, LobyUserData> TeamPlayerList { get; private set; } = new Dictionary<Guid, LobyUserData>();

    // チームのプレイヤー最大人数
    private int teamMaxPlayerAmount = 5;

    // チームのプレイヤー人数
    private int teamPlayerAmount;

    // 自分のキャラクターオブジェクト
    private GameObject myCharacter;
    // 自分のステータステキスト
    private Text myStatusText;
    private Vector3 StatusTextOffset = new Vector3(0, 6f, 0);

    private void Awake() {
        teamPlayerAmount = 0;
    }

    private async void Start() {
        mySelf = new JoinedUser();
        mySelf.UserData = new User();
        playerObjectParent = GameObject.Find("Players").GetComponent<Transform>();

        // ユーザーが入退室したときにメソッドを実行するよう、モデルに登録しておく
        RoomModel.Instance.OnJoinedLobyUser += OnJoinedLobyUser;
        RoomModel.Instance.OnLeavedLobyUser += OnLeavedLobyUser;

        // チームを入退室したときにメソッドを実行するよう、モデルに登録しておく
        RoomModel.Instance.OnJoinedTeamUser += OnJoinedTeamUser;
        RoomModel.Instance.OnLeavedTeamUser += OnLeavedTeamUser;

        // マッチング通知
        RoomModel.Instance.OnMatchingedRoomUser += OnMatchingedRoomUser;

        // プレイヤーを生成する
        myCharacter = Instantiate(playerPrefab, playerStandList[0].position, playerPrefab.transform.rotation, playerObjectParent);

        // ステータステキストを生成
        myStatusText = Instantiate(lobyUIManager.playerStatusTextPrefab, parent: lobyUIManager.playerStatusParent).GetComponent<Text>();
        myStatusText.rectTransform.position = RectTransformUtility.WorldToScreenPoint(Camera.main, myCharacter.transform.position + StatusTextOffset);

        await JoinLobyRoom();

        await CreateTeamAndJoin();
    }

    private void Update() {
        // デバッグ用
        if(Input.GetKeyDown(KeyCode.F)) {
            string teamPlayerInfo = "{\n";
            foreach(var item in TeamPlayerList) {
                teamPlayerInfo += "    [\n";
                teamPlayerInfo += "        ID : " + item.Value.joinedData.UserData.Id + "\n";
                teamPlayerInfo += "        名前 : " + item.Value.joinedData.UserData.Display_Name +"\n";
                teamPlayerInfo += "        接続ID : " + item.Value.joinedData.ConnectionId +"\n";
                teamPlayerInfo += "    ]\n";
            }
            teamPlayerInfo += "}\n";

            Debug.Log(
                "チームID : " + MyTeamId + "\n" +
                "チームの人数 : " + teamPlayerAmount + "\n" +
                "チームのプレイヤーのリスト \n" + teamPlayerInfo
                );
        }
    }

    /// <summary>
    /// オンラインかどうか
    /// </summary>
    public bool IsOnline(int userId) {
        return lobyUserList.Any(userData => userData.Value.joinedData.UserData.Id == userId);
    }

    /// <summary>
    /// ユーザーのコネクションIDを取得
    /// </summary>
    public Guid GetConnectionId(int userId) {
        var connectionId = lobyUserList.FirstOrDefault( user => user.Value.joinedData.UserData.Id == userId ).Value.joinedData;
        if (connectionId == null) {
            return Guid.Empty;
        }

        return connectionId.ConnectionId;
    }

    /// <summary>
    /// ロビールームに参加
    /// </summary>
    private async UniTask JoinLobyRoom() {
        // 自分のユーザー情報とコネクションIDを保存
        mySelf.UserData = await UserModel.Instance.GetUserByIdAsync(UserModel.Instance.UserId);
        mySelf.ConnectionId = await RoomModel.Instance.GetConnectionIdAsync();

        // ステータステキスト設定
        myStatusText.text = $"<b>{mySelf.UserData.Display_Name}</b>\n<color=red>準備中</color>";

        // 参加
        await RoomModel.Instance.JoinLobyAsync();
    }

    /// <summary>
    /// ロビールームから退室
    /// </summary>
    private async void LeaveLobyRoom() {
        await RoomModel.Instance.LeaveLobyAsync();

        Application.Quit();
    }


    /// <summary>
    /// [サーバー通知]
    /// ロビーの入室通知
    /// </summary>
    private void OnJoinedLobyUser(JoinedUser user) {
        // すでに追加済みのユーザーは追加しない
        if (lobyUserList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分は追加しない
        if (user.ConnectionId == mySelf.ConnectionId) {
            // ユーザーデータを保存
            mySelf.UserData = user.UserData;
            // 参加順番を保存
            mySelf.JoinOrder = user.JoinOrder;

            // ロビーユーザーデータとパーティーユーザーデータに自分を追加
            LobyUserData myUserData = new LobyUserData() { joinedData = mySelf, playerObject = myCharacter};
            lobyUserList[mySelf.ConnectionId] = myUserData;

            return;
        }

        // フィールドで保持
        LobyUserData lobyUserData = new LobyUserData() { joinedData = user };
        lobyUserList[user.ConnectionId] = lobyUserData;
    }

    /// <summary>
    /// [サーバー通知]
    /// ロビーの退室通知
    /// </summary>
    private void OnLeavedLobyUser(Guid connectionId, int joinOrder) {
        if(mySelf.ConnectionId == connectionId) {
            return;
        }

        // 参加順番を繰り下げ
        if (mySelf.JoinOrder > joinOrder) {
            mySelf.JoinOrder -= 1;
        }

        lobyUserList.Remove(connectionId);
    }

    /// <summary>
    /// チームにそのユーザーがいるか
    /// </summary>
    public bool InTeamUser(int userId) {
        return TeamPlayerList.Any(userData => userData.Value.joinedData.UserData.Id == userId);
    } 

    /// <summary>
    /// チームを作成
    /// </summary>
    private async UniTask CreateTeamAndJoin() {
        MyTeamId = await RoomModel.Instance.CreateTeamAndJoinAsync();
        // チームリストに自分を追加
        TeamPlayerList[mySelf.ConnectionId] = lobyUserList[mySelf.ConnectionId];
        teamPlayerAmount++;

        // ステータステキストリストに追加
        lobyUIManager.playerStatusTextList[mySelf.ConnectionId] = myStatusText;

        // 退出ボタンの切替
        if (teamPlayerAmount > 1) {
            lobyUIManager.SwitchActiveLeaveBtn(true);
        }
        else {
            lobyUIManager.SwitchActiveLeaveBtn(false);
        }

        // マッチングボタン設定
        lobyUIManager.matchingBtn.SetActive(true);
    }

    /// <summary>
    /// チームに参加
    /// </summary>
    public async void JoinTeam(Guid targetTeamId) {
        await LeaveTeam(false);

        // チームプレイヤーリストに自分を追加
        TeamPlayerList[mySelf.ConnectionId] = lobyUserList[mySelf.ConnectionId];
        teamPlayerAmount++;

        // ステータステキストリストに追加
        lobyUIManager.playerStatusTextList[mySelf.ConnectionId] = myStatusText;

        // 退出ボタンの切替
        if (teamPlayerAmount > 1) {
            lobyUIManager.SwitchActiveLeaveBtn(true);
        }
        else {
            lobyUIManager.SwitchActiveLeaveBtn(false);
        }

        // 参加
        await RoomModel.Instance.JoinTeamAsync(targetTeamId);
        MyTeamId = targetTeamId;

        // マッチングボタン設定
        lobyUIManager.readyBtn.SetActive(true);

        // UIを更新
        lobyUIManager.UpdateUI();
    }

    /// <summary>
    /// チームから退室ボタン
    /// </summary>
    public async void LeaveTeamBtn() {
        await LeaveTeam(true);
    }

    /// <summary>
    /// チームから退室
    /// </summary>
    private async UniTask LeaveTeam(bool IsNewCreateTeam) {
        await RoomModel.Instance.LeaveTeamAsync();

        // プレイヤーを削除
        foreach (LobyUserData userData in TeamPlayerList.Values) {
            if (userData.joinedData.ConnectionId != mySelf.ConnectionId) {
                Destroy(userData.playerObject);
            }
        }

        // チームプレイヤーリストを初期化
        TeamPlayerList = new Dictionary<Guid, LobyUserData>();

        teamPlayerAmount = 0;

        // マッチングボタン設定
        lobyUIManager.matchingBtn.SetActive(false);
        lobyUIManager.readyBtn.SetActive(false);

        // チーム作成
        if (IsNewCreateTeam) {
            await CreateTeamAndJoin();
        }

        // ステータステキストを削除
        foreach (var textList in lobyUIManager.playerStatusTextList) {
            if(textList.Key != mySelf.ConnectionId) {
                Destroy(textList.Value.gameObject);
            }
        }

        // ステータステキストを初期化
        lobyUIManager.playerStatusTextList = new Dictionary<Guid, Text>();
    }

    /// <summary>
    /// [サーバー通知]
    /// チームの参加通知
    /// </summary>
    private void OnJoinedTeamUser(JoinedUser user) {
        // すでに表示済みのユーザーは表示しない
        if (TeamPlayerList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分自身は追加しない
        if(mySelf.ConnectionId == user.ConnectionId) {
            return;
        }

        // フィールドで保持
        GameObject createdPlayer = Instantiate(playerPrefab, playerStandList[teamPlayerAmount].position, playerPrefab.transform.rotation, playerObjectParent);
        LobyUserData userData = new LobyUserData() { joinedData = user, playerObject = createdPlayer };
        TeamPlayerList[user.ConnectionId] = userData;
        teamPlayerAmount++;

        // ステータステキストを生成
        GameObject createdStatusObj = Instantiate(lobyUIManager.playerStatusTextPrefab, parent: lobyUIManager.playerStatusParent);
        //　テキストを設定する
        Text statusText = createdStatusObj.GetComponent<Text>();
        statusText.text = $"<b>{user.UserData.Display_Name}</b>\n<color=red>準備中</color>";

        // 位置を設定
        statusText.rectTransform.position = RectTransformUtility.WorldToScreenPoint(Camera.main, createdPlayer.transform.position + StatusTextOffset);

        // フィールドで保持
        lobyUIManager.playerStatusTextList[user.ConnectionId] = statusText;

        // 退出ボタンの切替
        if (teamPlayerAmount > 1) {
            lobyUIManager.SwitchActiveLeaveBtn(true);
        }
        else {
            lobyUIManager.SwitchActiveLeaveBtn(false);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// チームの退出通知
    /// </summary>
    private void OnLeavedTeamUser(Guid connectionId) {
        // チームに存在しなかったら何もしない
        if (!TeamPlayerList.ContainsKey(connectionId)) {
            return;
        }

        // 自分だったらなにもしない
        if(mySelf.ConnectionId == connectionId) {
            return;
        }

        // プレイヤー情報削除
        Destroy(TeamPlayerList[connectionId].playerObject);
        TeamPlayerList.Remove(connectionId);
        teamPlayerAmount--;

        // ステータステキストリストから削除
        Destroy(lobyUIManager.playerStatusTextList[connectionId].gameObject);
        lobyUIManager.playerStatusTextList.Remove(connectionId);

        // 退出ボタンの切替
        if (teamPlayerAmount > 1) {
            lobyUIManager.SwitchActiveLeaveBtn(true);
        }
        else {
            lobyUIManager.SwitchActiveLeaveBtn(false);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// マッチング通知
    /// </summary>
    public void OnMatchingedRoomUser(string roomName) {
        Debug.Log("マッチング通知 : " + roomName);
        SceneManager.LoadScene("GameScene");
    }
}
