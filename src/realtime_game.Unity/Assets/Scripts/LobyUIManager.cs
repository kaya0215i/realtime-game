using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobyUIManager : MonoBehaviour {
    [SerializeField] private LobyManager lobyManager;

    [SerializeField] private GameObject leaveTeamBtn;

    [SerializeField] private GameObject friendListScrollView;
    // フレンドリスト用
    [SerializeField] private GameObject friendPrefab;
    [SerializeField] private Transform friendListScrollViewContent;

    [SerializeField] private GameObject findUserPanel;
    // ユーザー検索用
    [SerializeField] private Text findUserInputField;
    [SerializeField] private GameObject findUserPrefab;
    [SerializeField] private Transform findUserListScrollViewContent;

    // フレンドリクエスト用
    [SerializeField] private GameObject friendRequestPrefab;
    [SerializeField] private Transform friendRequestListScrollViewContent;

    // チーム招待用
    [SerializeField] private GameObject inviteNotificationPrefab;
    [SerializeField] private Transform notificationParent;

    // マッチング、準備用
    [SerializeField] public GameObject matchingBtn;
    [SerializeField] public GameObject readyBtn;

    // ユーザーステータス用
    [SerializeField] public GameObject playerStatusTextPrefab;
    [SerializeField] public Transform playerStatusParent;
    [NonSerialized] public Dictionary<Guid, Text> playerStatusTextList = new Dictionary<Guid, Text>();

    private bool isReady = false;

    private void Start() {
        // チームに招待通知登録
        RoomModel.Instance.OnInvitedTeamUser += OnInvitedTeamUser;
        // 準備状態通知登録
        RoomModel.Instance.OnIsReadyedStatusUser += OnIsReadyedStatusUser;
    }

    /// <summary>
    /// マッチングボタンを押したら
    /// </summary>
    public async void OnClickMatchingBtn() {

        // ステータステキスト変数
        playerStatusTextList[lobyManager.mySelf.ConnectionId].text = $"<b>{lobyManager.mySelf.UserData.Display_Name}</b>\n<color=lime>準備完了</color>";
        await RoomModel.Instance.SendIsReadyStatusAsync(true);
        // マッチングスタート
        await RoomModel.Instance.StartMatchingAsync();
    }

    /// <summary>
    /// 準備ボタン
    /// </summary>
    public async void OnClickReadyBtn() {
        isReady = !isReady;
        Image btnImg = readyBtn.GetComponent<Image>();
        Text btnText = readyBtn.GetComponentInChildren<Text>(true);

        // 準備OK
        if (isReady) {
            btnImg.color = new Color(0.7f, 0.6f, 0.3f);
            btnText.text = "準備完了";
            playerStatusTextList[lobyManager.mySelf.ConnectionId].text = $"<b>{lobyManager.mySelf.UserData.Display_Name}</b>\n<color=lime>準備完了</color>";
        }
        // 準備BAD
        else {
            btnImg.color = new Color(1, 0.8f, 0);
            btnText.text = "準備";
            playerStatusTextList[lobyManager.mySelf.ConnectionId].text = $"<b>{lobyManager.mySelf.UserData.Display_Name}</b>\n<color=red>準備中</color>";
        }

        await RoomModel.Instance.SendIsReadyStatusAsync(isReady);
    }

    /// <summary>
    /// フレンドリストを表示非表示
    /// </summary>
    public void OnClickFriendListScrollView() {
        if (friendListScrollView.activeSelf) {
            friendListScrollView.SetActive(false);
        }
        else {
            friendListScrollView.SetActive(true);
            findUserPanel.SetActive(false);
            // フレンドリストを表示
            GetFriendToList();
        }
    }

    /// <summary>
    /// フレンド検索表示非表示
    /// </summary>
    public void OnClickFindUserPanel() {
        if(findUserPanel.activeSelf) {
            findUserPanel.SetActive(false);
        }
        else {
            findUserPanel.SetActive(true);
            friendListScrollView.SetActive(false);
            // フレンドリクエストリストを表示
            GetFriendRequestToList();
        }
    }

    /// <summary>
    /// UIを更新
    /// </summary>
    public void UpdateUI() {
        GetFriendToList();
        GetFriendRequestToList();
    }

    /// <summary>
    /// 退出ボタン切替
    /// </summary>
    public void SwitchActiveLeaveBtn(bool value) {
        leaveTeamBtn.SetActive(value);
    }

    /// <summary>
    /// フレンドを取得してリスト表示
    /// </summary>
    public async void GetFriendToList() {
        // 要素削除
        foreach (Transform child in friendListScrollViewContent) {
            Destroy(child.gameObject);
        }

        // フレンドを取得
        List<User> users = await UserModel.Instance.GetFriendInfoAsync();

        // フレンドリストを生成
        foreach (User user in users) {
            GameObject objUI = Instantiate(friendPrefab, Vector3.zero, Quaternion.identity, friendListScrollViewContent);
            // ユーザー名を設定
            Text userName = objUI.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "UserName");
            userName.text = user.Display_Name;

            // ステータス表示
            Text userStatus = objUI.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "StatusText");
            if (lobyManager.IsOnline(user.Id)) {
                userStatus.text = "●オンライン";
                userStatus.color = Color.green;

                // 送り先のコネクションIDを取得
                Guid connectionId = lobyManager.GetConnectionId(user.Id);

                // ボタンのイベントを設定
                Button inviteBtn = objUI.GetComponentInChildren<Button>(true);

                // チームにそのプレイヤーがいたら
                if (lobyManager.InTeamUser(user.Id)) {
                    // ボタンを削除
                    inviteBtn = objUI.GetComponentInChildren<Button>(true);
                    Destroy(inviteBtn.gameObject);
                    // テキストに表示
                    Text teamStatus = objUI.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "TeamStatusText");
                    teamStatus.text = "チーム内";
                    teamStatus.color = Color.green;
                }
                else {
                    inviteBtn.onClick.AddListener(async () => {
                        // チームに招待
                        await RoomModel.Instance.InviteTeamFriendAsync(connectionId, lobyManager.MyTeamId);
                        Destroy(inviteBtn.gameObject);

                        // テキストに表示
                        Text teamStatus = objUI.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "TeamStatusText");
                        teamStatus.text = "招待中";
                        teamStatus.color = Color.blue;
                    });
                }
            }
            else {
                userStatus.text = "×オフライン";
                userStatus.color = Color.red;

                // ボタンを削除
                Button inviteBtn = objUI.GetComponentInChildren<Button>(true);
                Destroy(inviteBtn.gameObject);
            }
        }
    }

    /// <summary>
    /// ユーザーを検索してリスト表示
    /// </summary>
    public async void FindUserToList() {
        // 要素削除
        foreach(Transform child in findUserListScrollViewContent) {
            Destroy(child.gameObject);
        }

        // ユーザー検索
        List<User> users = await UserModel.Instance.GetUserByDisplayNameAsync(findUserInputField.text);

        // フレンドを取得
        List<User> friends = await UserModel.Instance.GetFriendInfoAsync();

        // 送信済みリクエストを取得
        List<User> sentRequestUser = await UserModel.Instance.GetSendFriendRequestInfoAsync();

        // ユーザーリストを生成
        foreach (User user in users) {
            // 自分だったらパス
            if(user.Id == UserModel.Instance.UserId) {
                continue;
            }

            GameObject objUI = Instantiate(findUserPrefab, Vector3.zero, Quaternion.identity, findUserListScrollViewContent);
            // ユーザー名を設定
            Text userName = objUI.GetComponentInChildren<Text>(true);
            userName.text = user.Display_Name;

            // フレンド追加済みだったら
            if (friends.Any(friend => friend.Id == user.Id)) {
                Text addText = objUI.GetComponentsInChildren<Text>(true).First(txt => txt.gameObject.name == "StatusText");
                addText.text = "フレンド";
                addText.color = Color.green;

                // ボタン削除
                Button requestBtn = objUI.GetComponentInChildren<Button>(true);
                Destroy(requestBtn.gameObject);
            }
            // リクエストを送信済みだったら
            else if (sentRequestUser.Any(requestUser => requestUser.Id == user.Id)) {
                Text addText = objUI.GetComponentsInChildren<Text>(true).First(txt => txt.gameObject.name == "StatusText");
                addText.text = "申請中";
                addText.color = Color.blue;

                // ボタン削除
                Button requestBtn = objUI.GetComponentInChildren<Button>(true);
                Destroy(requestBtn.gameObject);
            }
            else {
                // ボタンのイベントを設定
                Button requestBtn = objUI.GetComponentInChildren<Button>(true);
                requestBtn.onClick.AddListener(async () => {
                    // フレンドリクエストを送信
                    await UserModel.Instance.SendFriendRequestAsync(user.Id);
                    Destroy(requestBtn.gameObject);

                    Text addText = objUI.GetComponentsInChildren<Text>(true).First(txt => txt.gameObject.name == "StatusText");
                    addText.text = "申請中";
                    addText.color = Color.blue;
                });
            }
        }
    }

    /// <summary>
    /// フレンドリクエストを取得してリスト表示
    /// </summary>
    public async void GetFriendRequestToList() {
        // 要素削除
        foreach (Transform child in friendRequestListScrollViewContent) {
            Destroy(child.gameObject);
        }

        // フレンドリクエスト取得
        List<User> users = await UserModel.Instance.GetFriendRequestInfoAsync();

        // フレンドリクエストリストを生成
        foreach (User user in users) {
            GameObject objUI = Instantiate(friendRequestPrefab, Vector3.zero, Quaternion.identity, friendRequestListScrollViewContent);
            // ユーザー名を設定
            Text userName = objUI.GetComponentInChildren<Text>(true);
            userName.text = user.Display_Name;

            // ボタンのイベントを設定
            var buttons = objUI.GetComponentsInChildren<Button>(true);
            foreach(Button button in buttons) {
                if(button.gameObject.name == "AcceptBtn") {
                    button.onClick.AddListener(async () => {
                        // フレンドリクエストを許可
                        await UserModel.Instance.AcceptFriendRequestAsync(user.Id);
                        Destroy(objUI);
                    });
                }
                else if(button.gameObject.name == "RefusalBtn") {
                    button.onClick.AddListener(async () => {
                        // フレンドリクエストを拒否
                        await UserModel.Instance.RefusalFriendRequestAsync(user.Id);
                        Destroy(objUI);
                    });
                }
            }
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// チームに招待通知
    /// </summary>
    private void OnInvitedTeamUser(Guid teamId, User senderUser) {
        GameObject objUI = Instantiate(inviteNotificationPrefab, parent: notificationParent);
        // 通知メッセージを設定
        Text notificationText = objUI.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "InviteNotificationText");
        notificationText.text = $"{senderUser.Display_Name}から招待が届きました";

        Destroy(objUI, 5f);

        // ボタンのイベントを設定
        var buttons = objUI.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons) {
            if (button.gameObject.name == "AcceptBtn") {
                button.onClick.AddListener(() => {
                    lobyManager.JoinTeam(teamId);
                    Destroy(objUI);
                });
            }
            else if (button.gameObject.name == "RefusalBtn") {
                button.onClick.AddListener(() => {
                    Destroy(objUI);
                });
            }
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// 準備状態通知
    /// </summary>
    private void OnIsReadyedStatusUser(Guid connectionId, bool IsReady) {
        // 自分のだったら何もしない
        if (lobyManager.mySelf.ConnectionId == connectionId) {
            return;
        }

        // ステータステキストを設定
        string statusText;
        if (IsReady) {
            statusText = $"<b>{lobyManager.TeamPlayerList[connectionId].joinedData.UserData.Display_Name}</b>\n<color=lime>準備完了</color>";
        }
        else {
            statusText = $"<b>{lobyManager.TeamPlayerList[connectionId].joinedData.UserData.Display_Name}</b>\n<color=red>準備中</color>";
        }
        playerStatusTextList[connectionId].text = statusText ;
    }
}
