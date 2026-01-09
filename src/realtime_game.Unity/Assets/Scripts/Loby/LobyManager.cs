using Cysharp.Threading.Tasks;
using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static CharacterSettings;

public class LobyManager : MonoBehaviour {
    [SerializeField] private LobyUIManager lobyUIManager;

    private LobyPlayerManager lobyPlayerManager;
    [NonSerialized] public JoinedUser mySelf = new JoinedUser(); // 自分のユーザー情報を保存

    // ロビー内のユーザーのリスト
    public Dictionary<Guid, UserDataAndObject> LobyUserList { get; private set; } = new Dictionary<Guid, UserDataAndObject>();

    // プレイヤーを生成する親Transform
    private Transform playerObjectParent;
    // ロビーに生成するプレイヤープレハブ
    [SerializeField] private GameObject playerPrefab;
    // プレイヤーを生成する場所の土台
    [SerializeField] private List<Transform> playerStandList;

    // チームプレイヤーのリスト
    public Dictionary<Guid, UserDataAndObject> TeamPlayerList { get; private set; } = new Dictionary<Guid, UserDataAndObject>();

    // チームに入ってるか
    public bool InTeam { get; private set; } = false;

    // チームのプレイヤー最大人数
    private int teamMaxPlayerAmount = 5;

    // チームのプレイヤー人数
    private int teamPlayerAmount = 0;

    // 自分のキャラクターオブジェクト
    private GameObject myCharacter;
    // 自分のステータステキスト
    private TextMeshProUGUI myStatusText;

    // ゲーム中かどうか
    [NonSerialized] public bool InGame = false;

    private async void Start() {
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

        // プレイヤーシーン移動通知
        RoomModel.Instance.OnWentGameRoomUser += OnWentGameRoomUser;
        RoomModel.Instance.OnReturnedLobyRoomUser += OnReturnedLobyRoomUser;

        // ロードアウト変更通知
        RoomModel.Instance.OnChangedLoadoutUser += OnChangedLoadoutUser;

        // プレイヤーを生成する
        myCharacter = Instantiate(playerPrefab, playerStandList[0].position + new Vector3(0, 0.1f, 0), playerPrefab.transform.rotation, playerObjectParent);
        lobyPlayerManager = myCharacter.GetComponent<LobyPlayerManager>();
        // タイプと武器を設定
        lobyPlayerManager.characterType = CharacterSettings.Instance.CharacterType;
        lobyPlayerManager.subWeapon = CharacterSettings.Instance.SubWeapon;
        lobyPlayerManager.ultimate = CharacterSettings.Instance.Ultimate;

        // ステータステキストを生成
        myStatusText = myCharacter.GetComponentInChildren<TextMeshProUGUI>();

        // 元々チームに入っていたら
        if (await RoomModel.Instance.IsAlreadyInTeamAsync()) {
            Debug.Log("あなたはチームに入っています");

            await ReturnLobyScene();
        }
        else {
            Debug.Log("あなたはチームに入っていません");

            bool joinResult = await JoinLobyRoom();

            // ロビーに入室出来たら
            if (joinResult) {
                await CreateTeamAndJoin();
            }
            else {
                SceneManager.LoadScene("TitleScene");
            }
        }
    }

    private void Update() {
        // デバッグ用
        if(Input.GetKeyDown(KeyCode.F)) {
            string teamPlayerInfo = "{\n";
            foreach(var item in TeamPlayerList) {
                teamPlayerInfo += "    [\n";
                teamPlayerInfo += "        ID : " + item.Value.joinedData.UserData.Id + "\n";
                teamPlayerInfo += "        名前 : " + item.Value.joinedData.UserData.Display_Name +"\n";
                teamPlayerInfo += "        接続ID : " + item.Value.joinedData.ConnectionId + "\n";
                teamPlayerInfo += "        リーダー : " + item.Value.joinedData.TeamUser.IsLeader + "\n";
                teamPlayerInfo += "        準備状態 : " + item.Value.joinedData.TeamUser.IsReady + "\n";
                teamPlayerInfo += "    ]\n";
            }
            teamPlayerInfo += "}\n";

            Debug.Log(
                "チームID : " + RoomModel.Instance.TeamId + "\n" +
                "チームの人数 : " + teamPlayerAmount + "\n" +
                "チームのプレイヤーのリスト \n" + teamPlayerInfo
                );
        }
    }

    /// <summary>
    /// ロビーシーンに戻ってきた
    /// </summary>
    public async UniTask ReturnLobyScene() {
        // ロビーに戻ったことを知らせる
        await RoomModel.Instance.ReturnLobyRoomAsync();

        // 自分のユーザー情報とコネクションIDを保存
        mySelf.UserData = await UserModel.Instance.GetUserByIdAsync(UserModel.Instance.UserId);
        mySelf.ConnectionId = await RoomModel.Instance.GetConnectionIdAsync();
        lobyPlayerManager.thisCharacterConnectionId = mySelf.ConnectionId;

        // ステータステキスト設定
        myStatusText.text = $"<b>{mySelf.UserData.Display_Name}</b>\n<color=red>準備中</color>";

        // ロビーユーザー情報を取得
        await RoomModel.Instance.GetLobyUsersAsync();

        InTeam = true;

        // チームリストに自分を追加
        TeamPlayerList[mySelf.ConnectionId] = LobyUserList[mySelf.ConnectionId];
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
        lobyUIManager.matchingBtn.SetActive(RoomModel.Instance.IsLeader);
        lobyUIManager.readyBtn.SetActive(!RoomModel.Instance.IsLeader);

        // チームにいるユーザーを取得
        await RoomModel.Instance.GetTeamUsersAsync();

        // マッチングボタンを押せるか
        lobyUIManager.IsOnClickMatchingBtn();
    }


    private void OnDisable() {
        if (RoomModel.Instance != null) {
            // 通知関連の登録解除
            RoomModel.Instance.OnJoinedLobyUser -= OnJoinedLobyUser;
            RoomModel.Instance.OnLeavedLobyUser -= OnLeavedLobyUser;
            RoomModel.Instance.OnJoinedTeamUser -= OnJoinedTeamUser;
            RoomModel.Instance.OnLeavedTeamUser -= OnLeavedTeamUser;
            RoomModel.Instance.OnMatchingedRoomUser -= OnMatchingedRoomUser;
            RoomModel.Instance.OnWentGameRoomUser -= OnWentGameRoomUser;
            RoomModel.Instance.OnReturnedLobyRoomUser -= OnReturnedLobyRoomUser;
            RoomModel.Instance.OnChangedLoadoutUser -= OnChangedLoadoutUser;
        }
    }

    private void OnDestroy() {
        OnDisable();
    }


    /// <summary>
    /// オンラインかどうか
    /// </summary>
    public bool IsOnline(int userId) {
        return LobyUserList.Any(userData => userData.Value.joinedData.UserData.Id == userId);
    }

    /// <summary>
    /// ユーザーのコネクションIDを取得
    /// </summary>
    public Guid GetConnectionId(int userId) {
        var connectionId = LobyUserList.FirstOrDefault( user => user.Value.joinedData.UserData.Id == userId ).Value.joinedData;
        if (connectionId == null) {
            return Guid.Empty;
        }

        return connectionId.ConnectionId;
    }

    /// <summary>
    /// ロビールームに参加
    /// </summary>
    private async UniTask<bool> JoinLobyRoom() {
        // 自分のユーザー情報とコネクションIDを保存
        mySelf.UserData = await UserModel.Instance.GetUserByIdAsync(UserModel.Instance.UserId);
        mySelf.ConnectionId = await RoomModel.Instance.GetConnectionIdAsync();
        lobyPlayerManager.thisCharacterConnectionId = mySelf.ConnectionId;

        // ステータステキスト設定
        myStatusText.text = $"<b>{mySelf.UserData.Display_Name}</b>\n<color=red>準備中</color>";

        // 参加
        return await RoomModel.Instance.JoinLobyAsync();
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
        if (LobyUserList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分は追加しない
        if (user.ConnectionId == mySelf.ConnectionId) {
            // ユーザーデータを保存
            mySelf.UserData = user.UserData;
            // 参加順番を保存
            mySelf.JoinOrder = user.JoinOrder;

            // ロビーユーザーデータとチームユーザーデータに自分を追加
            UserDataAndObject myUserData = new UserDataAndObject() { joinedData = mySelf, playerObject = myCharacter};
            myUserData.joinedData.TeamUser = new TeamUser() { IsLeader = user.TeamUser.IsLeader, IsReady = user.TeamUser.IsReady, IsPlaying = user.TeamUser.IsPlaying };
            LobyUserList[mySelf.ConnectionId] = myUserData;

            return;
        }

        // フィールドで保持
        UserDataAndObject lobyUserData = new UserDataAndObject() { joinedData = user };
        lobyUserData.joinedData.TeamUser = new TeamUser() { IsLeader = user.TeamUser.IsLeader, IsReady = user.TeamUser.IsReady, IsPlaying = user.TeamUser.IsPlaying };
        LobyUserList[user.ConnectionId] = lobyUserData;
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

        LobyUserList.Remove(connectionId);
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
        await RoomModel.Instance.CreateTeamAndJoinAsync();

        InTeam = true;

        // チームリストに自分を追加
        TeamPlayerList[mySelf.ConnectionId] = LobyUserList[mySelf.ConnectionId];
        TeamPlayerList[mySelf.ConnectionId].joinedData.TeamUser.IsLeader = true;
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

        // マッチングボタンを押せるか
        lobyUIManager.IsOnClickMatchingBtn();
    }

    /// <summary>
    /// チームに参加
    /// </summary>
    public async void JoinTeam(Guid targetTeamId) {
        await LeaveTeam(false);

        // チームプレイヤーリストに自分を追加
        TeamPlayerList[mySelf.ConnectionId] = LobyUserList[mySelf.ConnectionId];
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

        InTeam = true;

        // マッチングボタン設定
        lobyUIManager.readyBtn.SetActive(true);
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

        InTeam = false;

        // プレイヤーを削除
        foreach (UserDataAndObject userData in TeamPlayerList.Values) {
            if (userData.joinedData.ConnectionId != mySelf.ConnectionId) {
                Destroy(userData.playerObject);
            }
        }

        // チームを抜けるのでリーダーじゃない
        LobyUserList[mySelf.ConnectionId].joinedData.TeamUser.IsLeader = false;
        // おなじく準備完了ではない
        LobyUserList[mySelf.ConnectionId].joinedData.TeamUser.IsReady = false;

        // ステータステキスト変更
        lobyUIManager.playerStatusTextList[mySelf.ConnectionId].text = $"<b>{mySelf.UserData.Display_Name}</b>\n<color=red>準備中</color>";

        // チームプレイヤーリストを初期化
        TeamPlayerList = new Dictionary<Guid, UserDataAndObject>();

        teamPlayerAmount = 0;

        // マッチングボタン設定
        lobyUIManager.matchingBtn.SetActive(false);
        lobyUIManager.readyBtn.SetActive(false);

        // ステータステキストを削除
        foreach (var textList in lobyUIManager.playerStatusTextList) {
            if(textList.Key != mySelf.ConnectionId) {
                Destroy(textList.Value.gameObject);
            }
        }

        // ステータステキストを初期化
        lobyUIManager.playerStatusTextList = new Dictionary<Guid, TextMeshProUGUI>();

        // チーム作成
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
        if (TeamPlayerList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分自身は追加しない
        if(mySelf.ConnectionId == user.ConnectionId) {
            return;
        }

        // フィールドで保持
        GameObject createdPlayer = Instantiate(playerPrefab, playerStandList[teamPlayerAmount].position + new Vector3(0, 0.1f, 0), playerPrefab.transform.rotation, playerObjectParent);

        LobyPlayerManager createdLobyPlayerManager = createdPlayer.GetComponent<LobyPlayerManager>();
        // コネクションID適応
        createdLobyPlayerManager.thisCharacterConnectionId = user.ConnectionId;

        // キャラクター装備メッシュ
        CharacterEquipmentSO CESO = CharacterSettings.Instance.CESO;

        // キャラクタータイプと武器適応
        createdLobyPlayerManager.characterType = (PLAYER_CHARACTER_TYPE)Enum.ToObject(typeof(PLAYER_CHARACTER_TYPE), user.LoadoutData.CharacterTypeNum);
        createdLobyPlayerManager.subWeapon = (PLAYER_SUB_WEAPON)Enum.ToObject(typeof(PLAYER_SUB_WEAPON), user.LoadoutData.SubWeaponNum);
        createdLobyPlayerManager.ultimate = (PLAYER_ULTIMATE)Enum.ToObject(typeof(PLAYER_ULTIMATE), user.LoadoutData.UltimateNum);

        // メッシュ適応
        createdLobyPlayerManager.Hat.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Hat").equipment.Find(_ => _.name == user.LoadoutData.HatName).mesh;
        createdLobyPlayerManager.Accessories.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Accessories").equipment.Find(_ => _.name == user.LoadoutData.AccessoriesName).mesh;
        createdLobyPlayerManager.Pants.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Pants").equipment.Find(_ => _.name == user.LoadoutData.PantsName).mesh;
        createdLobyPlayerManager.Hairstyle.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Hairstyle").equipment.Find(_ => _.name == user.LoadoutData.HairstyleName).mesh;
        createdLobyPlayerManager.Outerwear.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Outerwear").equipment.Find(_ => _.name == user.LoadoutData.OuterwearName).mesh;
        createdLobyPlayerManager.Shoes.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Shoes").equipment.Find(_ => _.name == user.LoadoutData.ShoesName).mesh;

        UserDataAndObject userData = new UserDataAndObject() { joinedData = user, playerObject = createdPlayer };
        TeamPlayerList[user.ConnectionId] = userData;
        teamPlayerAmount++;

        //　ステータステキストを取得しテキストを設定する
        TextMeshProUGUI statusText = createdPlayer.GetComponentInChildren<TextMeshProUGUI>();
        if (user.TeamUser.IsPlaying) {
            statusText.text = $"<b>{user.UserData.Display_Name}</b>\n<color=blue>プレイ中</color>";
        }
        else if (user.TeamUser.IsReady) {
            statusText.text = $"<b>{user.UserData.Display_Name}</b>\n<color=green>準備完了</color>";
        }
        else {
            statusText.text = $"<b>{user.UserData.Display_Name}</b>\n<color=red>準備中</color>";
        }

        // フィールドで保持
        lobyUIManager.playerStatusTextList[user.ConnectionId] = statusText;

        // 退出ボタンの切替
        if (teamPlayerAmount > 1) {
            lobyUIManager.SwitchActiveLeaveBtn(true);
        }
        else {
            lobyUIManager.SwitchActiveLeaveBtn(false);
        }

        // マッチングボタンを押せるか
        lobyUIManager.IsOnClickMatchingBtn();
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

        // マッチングボタンを押せるか
        lobyUIManager.IsOnClickMatchingBtn();
    }

    /// <summary>
    /// [サーバー通知]
    /// マッチング通知
    /// </summary>
    public void OnMatchingedRoomUser(string roomName) {
        Debug.Log("マッチング通知 : " + roomName);

        // ステータステキスト変更
        foreach (var textList in lobyUIManager.playerStatusTextList) {
            textList.Value.text = $"<b>{TeamPlayerList[textList.Key].joinedData.UserData.Display_Name}</b>\n<color=blue>プレイ中</color>";
        }

        InGame = true;

        // ゲームシーンに移動
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// [サーバー通知]
    /// ゲームシーンに移動した通知
    /// </summary>
    public void OnWentGameRoomUser(Guid connectionId) {
        LobyUserList[connectionId].joinedData.TeamUser.IsPlaying = true;
    }

    /// <summary>
    /// [サーバー通知]
    /// ロビーに帰ってきたとき通知
    /// </summary>
    public void OnReturnedLobyRoomUser(Guid connectionId) {
        if (!LobyUserList.ContainsKey(connectionId)) {
            return;
        }

        LobyUserList[connectionId].joinedData.TeamUser.IsPlaying = false;

        // チームにいたときは
        if(TeamPlayerList.ContainsKey(connectionId)) {
            lobyUIManager.playerStatusTextList[connectionId].text = $"<b>{LobyUserList[connectionId].joinedData.UserData.Display_Name}</b>\n<color=red>準備中</color>";
        }
    }

    /// <summary>
    /// TeamUserのIsReadyを変更
    /// </summary>
    public void ChangeTeamUserDataIsReady(Guid connectionId, bool isReady) {
        TeamPlayerList[connectionId].joinedData.TeamUser.IsReady = isReady;
    }

    /// <summary>
    /// [サーバー通知]
    /// チームメンバーにロードアウト変更通知
    /// </summary>
    public void OnChangedLoadoutUser(Guid connectionId, LoadoutData loadoutData) {
        if (connectionId == mySelf.ConnectionId) {
            return;
        }
        // キャラクター装備メッシュ
        CharacterEquipmentSO CESO = CharacterSettings.Instance.CESO;

        // タイプと武器変更
        LobyPlayerManager createdLobyPlayerManager = TeamPlayerList[connectionId].playerObject.GetComponent<LobyPlayerManager>();

        // キャラクタータイプと武器適応
        createdLobyPlayerManager.characterType = (PLAYER_CHARACTER_TYPE)Enum.ToObject(typeof(PLAYER_CHARACTER_TYPE), loadoutData.CharacterTypeNum);
        createdLobyPlayerManager.subWeapon = (PLAYER_SUB_WEAPON)Enum.ToObject(typeof(PLAYER_SUB_WEAPON), loadoutData.SubWeaponNum);
        createdLobyPlayerManager.ultimate = (PLAYER_ULTIMATE)Enum.ToObject(typeof(PLAYER_ULTIMATE), loadoutData.UltimateNum);

        // メッシュ適応
        LobyPlayerManager lPM = TeamPlayerList[connectionId].playerObject.GetComponent<LobyPlayerManager>();
        lPM.Hat.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Hat").equipment.Find(_ => _.name == loadoutData.HatName).mesh;
        lPM.Accessories.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Accessories").equipment.Find(_ => _.name == loadoutData.AccessoriesName).mesh;
        lPM.Pants.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Pants").equipment.Find(_ => _.name == loadoutData.PantsName).mesh;
        lPM.Hairstyle.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Hairstyle").equipment.Find(_ => _.name == loadoutData.HairstyleName).mesh;
        lPM.Outerwear.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Outerwear").equipment.Find(_ => _.name == loadoutData.OuterwearName).mesh;
        lPM.Shoes.sharedMesh = CESO.characterEquipment.Find(_ => _.name == "Shoes").equipment.Find(_ => _.name == loadoutData.ShoesName).mesh;

        createdLobyPlayerManager.ChangeCharacterModel();
    }
}
