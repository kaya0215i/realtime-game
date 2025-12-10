using Cysharp.Threading.Tasks;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class LobyManager : MonoBehaviour {
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
    private Guid myTeamId;

    // チームプレイヤーのリスト
    private Dictionary<Guid, LobyUserData> teamPlayerList = new Dictionary<Guid, LobyUserData>();

    // チームのプレイヤー最大人数
    private int teamMaxPlayerAmount = 5;

    // チームのプレイヤー人数
    private int teamPlayerAmount;

    [SerializeField] private Text teamIdText;
    [SerializeField] private Text userIdText;

    // 自分のキャラクターオブジェクト
    private GameObject myCharacter;

    private void Awake() {
        teamPlayerAmount = 0;
    }

    private async void Start() {
        mySelf = new JoinedUser();
        playerObjectParent = GameObject.Find("Players").GetComponent<Transform>();

        // ユーザーが入退室したときにメソッドを実行するよう、モデルに登録しておく
        RoomModel.Instance.OnJoinedLobyUser += OnJoinedLobyUser;
        RoomModel.Instance.OnLeavedLobyUser += OnLeavedLobyUser;

        // チームを入退室したときにメソッドを実行するよう、モデルに登録しておく
        RoomModel.Instance.OnJoinedTeamUser += OnJoinedTeamUser;
        RoomModel.Instance.OnLeavedTeamUser += OnLeavedTeamUser;

        // プレイヤーを生成する
        myCharacter = Instantiate(playerPrefab, playerStandList[0].position, playerPrefab.transform.rotation, playerObjectParent);

        await JoinLobyRoom();

        //await CreateTeamAndJoin();
    }

    private void Update() {
        // デバッグ用
        if(Input.GetKeyDown(KeyCode.F)) {
            string teamPlayerInfo = "{\n";
            foreach(var item in teamPlayerList) {
                teamPlayerInfo += "    [\n";
                teamPlayerInfo += "        ID : " + item.Value.joinedData.UserData.Id + "\n";
                teamPlayerInfo += "        名前 : " + item.Value.joinedData.UserData.Name +"\n";
                teamPlayerInfo += "        接続ID : " + item.Value.joinedData.ConnectionId +"\n";
                teamPlayerInfo += "    ]\n";
            }
            teamPlayerInfo += "}\n";

            Debug.Log(
                "チームID : " + myTeamId + "\n" +
                "チームの人数 : " + teamPlayerAmount + "\n" +
                "チームのプレイヤーのリスト \n" + teamPlayerInfo
                );
        }
    }

    /// <summary>
    /// ロビールームに参加
    /// </summary>
    private async UniTask JoinLobyRoom() {

        // あとで消す
        await RoomModel.Instance.ConnectAsync();


        // 自分のコネクションIDを保存
        mySelf.ConnectionId = await RoomModel.Instance.GetConnectionIdAsync();

        // 参加
        //await RoomModel.Instance.JoinLobyAsync(1);
    }

    public async void JoinLobyRoomBtn() {
        int userId = int.Parse(userIdText.text);

        // 参加
        await RoomModel.Instance.JoinLobyAsync(userId);

        await CreateTeamAndJoin();
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
    /// チームを作成
    /// </summary>
    private async UniTask CreateTeamAndJoin() {
        myTeamId = await RoomModel.Instance.CreateTeamAndJoinAsync();
        // チームリストに自分を追加
        teamPlayerList[mySelf.ConnectionId] = lobyUserList[mySelf.ConnectionId];
        teamPlayerAmount++;
        Debug.Log(myTeamId);
    }

    /// <summary>
    /// チームに参加
    /// </summary>
    public async void JoinTeam() {
        if(teamIdText.text == "") {
            return;
        }

        await LeaveTeam(false);

        Guid targetTeamId = Guid.Parse(teamIdText.text);

        // チームプレイヤーリストに自分を追加
        teamPlayerList[mySelf.ConnectionId] = lobyUserList[mySelf.ConnectionId];

        teamPlayerAmount++;

        // 参加
        await RoomModel.Instance.JoinTeamAsync(targetTeamId);
        myTeamId = targetTeamId;
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

        foreach (LobyUserData userData in teamPlayerList.Values) {
            if (userData.joinedData.ConnectionId != mySelf.ConnectionId) {
                Destroy(userData.playerObject);
            }
        }

        // チームプレイヤーリストを初期化
        teamPlayerList = new Dictionary<Guid, LobyUserData>();

        teamPlayerAmount = 0;

        if (IsNewCreateTeam) {
            await CreateTeamAndJoin();
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// チームの参加通知
    /// </summary>
    private void OnJoinedTeamUser(JoinedUser user) {
        // すでに表示済みのユーザーは表示しない
        if (teamPlayerList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分自身は追加しない
        if(mySelf.ConnectionId == user.ConnectionId) {
            return;
        }

        // フィールドで保持
        GameObject createdPlayer = Instantiate(playerPrefab, playerStandList[teamPlayerAmount].position, playerPrefab.transform.rotation, playerObjectParent);
        LobyUserData userData = new LobyUserData() { joinedData = user, playerObject = createdPlayer };
        teamPlayerList[user.ConnectionId] = userData;

        teamPlayerAmount++;
    }

    /// <summary>
    /// [サーバー通知]
    /// チームの退出通知
    /// </summary>
    private void OnLeavedTeamUser(Guid connectionId) {
        // チームに存在しなかったら何もしない
        if (!teamPlayerList.ContainsKey(connectionId)) {
            return;
        }

        // 自分だったらなにもしない
        if(mySelf.ConnectionId == connectionId) {
            return;
        }

        // プレイヤー情報削除
        Destroy(teamPlayerList[connectionId].playerObject);
        teamPlayerList.Remove(connectionId);

        teamPlayerAmount--;
    }
}
