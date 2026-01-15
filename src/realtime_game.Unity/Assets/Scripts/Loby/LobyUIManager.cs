using Cysharp.Threading.Tasks;
using DG.Tweening;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static CharacterSettings;
using static GameManager;

public class LobyUIManager : MonoBehaviour {
    [SerializeField] private LobyManager lobyManager;
    // 退出ボタンの
    [SerializeField] private GameObject leaveTeamBtn;

    // ヘッダーメニュー用

    // フレンドUI用
    [SerializeField] private GameObject friendUI;
    [SerializeField] private Text friendUIHeaderText;

    // フレンドリスト用
    [SerializeField] private GameObject friendListPanel;
    [SerializeField] private GameObject friendPrefab;
    [SerializeField] private Transform friendListScrollViewContent;

    // ユーザー検索用
    [SerializeField] private GameObject findUserPanel;
    [SerializeField] private Text findUserInputField;
    [SerializeField] private GameObject findUserPrefab;
    [SerializeField] private Transform findUserListScrollViewContent;

    // フレンドリクエスト用
    [SerializeField] private GameObject friendRequestPrefab;
    [SerializeField] private Transform friendRequestListScrollViewContent;
    private Dictionary<int, FriendList> friendList = new Dictionary<int, FriendList>();

    // 設定用
    [SerializeField] private GameObject settingUI;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Slider sensSlider;
    [SerializeField] private InputField sensInputField;
    [SerializeField] private Slider audioValumeSlider;
    [SerializeField] private InputField audioValumeInputField;

    // ロードアウト用
    [SerializeField] private GameObject loadoutUI;
    [SerializeField] private GameObject itemSelects;
    [SerializeField] private GameObject selecter;
    [SerializeField] private Image selectiongImage;
    [SerializeField] private GameObject itemSelectPrefab;
    [SerializeField] private Transform itemSelectParent;
    [SerializeField] private SerializableDictionary<string, Image> weaponSelectImages;
    [SerializeField] private SerializableDictionary<string, Image> skinPartSelectImages;

    // チーム招待用
    [SerializeField] private GameObject inviteNotificationPrefab;
    [SerializeField] private Transform notificationParent;

    // マッチング、準備用
    [SerializeField] public GameObject matchingUI;
    [SerializeField] public GameObject matchingBtn;
    [SerializeField] public GameObject readyBtn;

    // ユーザーステータス用
    [NonSerialized] public Dictionary<Guid, TextMeshProUGUI> playerStatusTextList = new Dictionary<Guid, TextMeshProUGUI>();

    // マッチングできるか
    private bool isStartMatching = false;

    // 準備状態
    private bool isReady = false;

    // フレンドリスト更新タイマー
    private float updateFriendListTimer = 0;

    private CancellationToken CT;

    private void Awake() {
        CT = this.GetCancellationTokenOnDestroy();
    }

    private void Start() {
        // チームに招待通知登録
        RoomModel.Instance.OnInvitedTeamUser += OnInvitedTeamUser;
        // 準備状態通知登録
        RoomModel.Instance.OnIsReadyedStatusUser += OnIsReadyedStatusUser;

        // 武器選択ボタンの画像設定
        weaponSelectImages["MainWeapon"].sprite = CharacterSettings.Instance.GetCharacterCharacterTypeSprite();
        weaponSelectImages["SubWeapon"].sprite = CharacterSettings.Instance.GetCharacterSubWeaponSprite();
        weaponSelectImages["Ultimate"].sprite = CharacterSettings.Instance.GetCharacterUltimateSprite();

        // スキン選択ボタンの画像を設定
        foreach (var item in skinPartSelectImages) {
            item.Value.sprite = CharacterSettings.Instance.GetCharacterEquipmentSprite(item.Key);
        }

        // 感度設定
        sensInputField.text = CharacterSettings.Instance.RawSens.ToString();
        OnChangedSensInputField();
        // 音量設定
        audioValumeInputField.text = AudioManager.Instance.AudioVolume.ToString();
        OnChangedAudioVolumeInputField();
    }

    private async void Update() {
        if (lobyManager == null ||
            lobyManager.InGame) {
            return;
        }

        if(Input.GetKeyDown(KeyCode.F)) {
            string textListLog = "{\n";
            foreach (var textList in playerStatusTextList) {
                textListLog += "    [\n";
                textListLog += "        " + textList.Key + "\n";
                textListLog += "        " + textList.Value.text + "\n";
                textListLog += "    ]\n";
            }
            textListLog += "}\n";

            Debug.Log(textListLog);
        }

        updateFriendListTimer += Time.deltaTime;
        if (updateFriendListTimer >= 0.2f) {
            updateFriendListTimer = 0;
            // UIを更新
            try {
                await GetFriendToList().AttachExternalCancellation(CT);
            }
            catch { }
        }
    }

    private void OnDisable() {
        if (RoomModel.Instance != null) {
            // 通知関連の登録解除
            RoomModel.Instance.OnInvitedTeamUser -= OnInvitedTeamUser;
            RoomModel.Instance.OnIsReadyedStatusUser -= OnIsReadyedStatusUser;
        }
    }

    private void OnDestroy() {
        OnDisable();
    }

    /// <summary>
    /// プレイボタン
    /// </summary>
    public void OnClickPlayBtn() {
        Camera.main.transform.DOKill();

        loadoutUI.SetActive(false);

        Camera.main.transform.DOMoveX(0, Camera.main.transform.position.x / 25f * 1f).SetEase(Ease.InOutQuad).OnComplete(() => {
            matchingUI.SetActive(true);
        });
    }

    /// <summary>
    /// ロードアウトボタン
    /// </summary>
    public void OnClickLoadoutBtn() {
        Camera.main.transform.DOKill();

        matchingUI.SetActive(false);

        Camera.main.transform.DOMoveX(25, (25f - Camera.main.transform.position.x) / 25f * 1f).SetEase(Ease.InOutQuad).OnComplete(() => {
            loadoutUI.SetActive(true);
        });
    }

    /// <summary>
    /// マッチングボタンを押したら
    /// </summary>
    public async void OnClickMatchingBtn() {
        if (isStartMatching) {
            // ステータステキスト変数
            playerStatusTextList[lobyManager.mySelf.ConnectionId].text = $"<b>{lobyManager.mySelf.UserData.Display_Name}</b>\n<color=green>準備完了</color>";
            await RoomModel.Instance.SendIsReadyStatusAsync(true);
            // マッチングスタート
            await RoomModel.Instance.StartMatchingAsync();
        }
        else {
            Debug.Log("チームメンバーの準備が完了していません");
        }
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
            playerStatusTextList[lobyManager.mySelf.ConnectionId].text = $"<b>{lobyManager.mySelf.UserData.Display_Name}</b>\n<color=green>準備完了</color>";
        }
        // 準備BAD
        else {
            btnImg.color = new Color(1, 0.8f, 0);
            btnText.text = "準備";
            playerStatusTextList[lobyManager.mySelf.ConnectionId].text = $"<b>{lobyManager.mySelf.UserData.Display_Name}</b>\n<color=red>準備中</color>";
        }

        // TeamUserのIsReadyを変更
        lobyManager.ChangeTeamUserDataIsReady(lobyManager.mySelf.ConnectionId, isReady);

        await RoomModel.Instance.SendIsReadyStatusAsync(isReady);
    }

    /// <summary>
    /// マッチングボタンを押せるか
    /// </summary>
    public void IsOnClickMatchingBtn() {
        // リーダーじゃなかったら何もしない
        if (!lobyManager.mySelf.TeamUser.IsLeader) {
            return;
        }

        int maxReadyUserAmount = lobyManager.TeamPlayerList.Count - 1;
        int readyUserAmount = 0;

        foreach (var user in lobyManager.TeamPlayerList) {
            // リーダーは無視
            if (user.Value.joinedData.TeamUser.IsLeader) {
                continue;
            }

            // 準備完了状態だったら++
            if (user.Value.joinedData.TeamUser.IsReady) {
                readyUserAmount++;
            }
        }

        Image btnImg = matchingBtn.GetComponent<Image>();
        Text btnText = matchingBtn.GetComponentInChildren<Text>(true);

        // リーダー以外が準備完了状態ならリーダーはマッチングボタンを押せる
        if (maxReadyUserAmount == readyUserAmount) {
            isStartMatching = true;

            btnImg.color = new Color(1, 0.8f, 0);
            btnText.text = "マッチング";
        }
        else {
            isStartMatching = false;

            btnImg.color = new Color(0.7f, 0.6f, 0.3f);
            btnText.text = "チームメンバーの準備が完了していません";
        }
    }

    /// <summary>
    /// フレンドリストを表示非表示
    /// </summary>
    public void OnClickFriendListScrollView() {
        friendUIHeaderText.text = "フレンドリスト";

        friendListPanel.SetActive(true);
        findUserPanel.SetActive(false);
    }

    /// <summary>
    /// フレンド検索表示非表示
    /// </summary>
    public void OnClickFindUserPanel() {
        friendUIHeaderText.text = "検索";

        findUserPanel.SetActive(true);
        friendListPanel.SetActive(false);
        // フレンドリクエストリストを表示
        GetFriendRequestToList();
    }

    /// <summary>
    /// 退出ボタン切替
    /// </summary>
    public void SwitchActiveLeaveBtn(bool value) {
        leaveTeamBtn.SetActive(value);
    }

    /// <summary>
    /// ハンバーガーメニューボタン
    /// </summary>
    public void OnClickHamBergerMenuBtn() {
        if (!friendUI.activeSelf &&
            !settingUI.activeSelf) {
            friendUIHeaderText.text = "フレンドリスト";
            friendUI.SetActive(true);

            friendListPanel.SetActive(true);
            findUserPanel.SetActive(false);

            settingUI.SetActive(true);
        }
        else {
            friendUI.SetActive(false);

            friendListPanel.SetActive(false);
            findUserPanel.SetActive(false);

            settingUI.SetActive(false);
        }
    }

    /// <summary>
    /// フレンドを取得してリスト表示
    /// </summary>
    private async UniTask GetFriendToList() {
        // フレンドを取得
        List<User> users = await UserModel.Instance.GetFriendInfoAsync();

        // フレンドリストを生成
        foreach (User user in users) {
            if (!friendList.ContainsKey(user.Id)) {
                GameObject objUI = Instantiate(friendPrefab, Vector3.zero, Quaternion.identity, friendListScrollViewContent);
                // フレンドリストに追加
                friendList.Add(user.Id, new FriendList() { uiObject = objUI });
            }
            else {
                if (friendList[user.Id].isInvite) {
                    friendList[user.Id].inviteCooltimer += Time.deltaTime;

                    if (friendList[user.Id].inviteCooltimer >= 0.1f) {
                        friendList[user.Id].inviteCooltimer = 0;
                        friendList[user.Id].isInvite = false;
                    }
                }
            }

            // ユーザー名を設定
            Text userName = friendList[user.Id].uiObject.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "UserName");
            userName.text = user.Display_Name;

            // ステータス表示
            Text userStatus = friendList[user.Id].uiObject.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "StatusText");
            if (lobyManager.IsOnline(user.Id)) {
                userStatus.text = "●オンライン";
                userStatus.color = Color.green;

                // ユーザーのコネクションIDを取得
                Guid connectionId = lobyManager.GetConnectionId(user.Id);

                // ボタンのイベントを設定
                Button inviteBtn = friendList[user.Id].uiObject.GetComponentInChildren<Button>(true);

                // フレンド
                var friend = lobyManager.LobyUserList.FirstOrDefault(userList => userList.Value.joinedData.UserData.Id == user.Id).Value;

                // チームにそのプレイヤーがいたら
                if (lobyManager.InTeamUser(user.Id)) {
                    // ボタンを非表示
                    inviteBtn = friendList[user.Id].uiObject.GetComponentInChildren<Button>(true);
                    inviteBtn.gameObject.SetActive(false);
                    // テキストに表示
                    Text teamStatus = friendList[user.Id].uiObject.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "TeamStatusText");
                    teamStatus.text = "チーム内";
                    teamStatus.color = Color.green;

                    friendList[user.Id].inviteCooltimer = 0;
                    friendList[user.Id].isInvite = false;
                }
                // プレイ中だったら
                else if (friend != null &&
                        friend.joinedData.TeamUser.IsPlaying) {
                    // ボタンを非表示
                    inviteBtn = friendList[user.Id].uiObject.GetComponentInChildren<Button>(true);
                    inviteBtn.gameObject.SetActive(false);
                    // テキストに表示
                    Text teamStatus = friendList[user.Id].uiObject.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "TeamStatusText");
                    teamStatus.text = "プレイ中";
                    teamStatus.color = Color.blue;
                }
                // 招待中だったら
                else if (friendList[user.Id].isInvite) {
                    // ボタンを非表示
                    inviteBtn = friendList[user.Id].uiObject.GetComponentInChildren<Button>(true);
                    inviteBtn.gameObject.SetActive(false);
                    // テキストに表示
                    Text teamStatus = friendList[user.Id].uiObject.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "TeamStatusText");
                    teamStatus.text = "招待中";
                    teamStatus.color = Color.cyan;
                }
                else {
                    // ボタンを表示
                    inviteBtn.gameObject.SetActive(true);

                    // テキストに表示
                    Text teamStatus = friendList[user.Id].uiObject.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "TeamStatusText");
                    teamStatus.text = "";

                    // 招待ボタンのイベントを設定
                    inviteBtn.onClick.RemoveAllListeners();
                    inviteBtn.onClick.AddListener(async () => {
                        // チームに招待
                        await RoomModel.Instance.InviteTeamFriendAsync(connectionId);
                        inviteBtn.gameObject.SetActive(false);

                        // 招待中に
                        friendList[user.Id].isInvite = true;

                        // テキストに表示
                        Text teamStatus = friendList[user.Id].uiObject.GetComponentsInChildren<Text>(true).First(text => text.gameObject.name == "TeamStatusText");
                        teamStatus.text = "招待中";
                        teamStatus.color = Color.cyan;
                    });
                }
            }
            else {
                userStatus.text = "×オフライン";
                userStatus.color = Color.red;

                // ボタンを非表示
                Button inviteBtn = friendList[user.Id].uiObject.GetComponentInChildren<Button>(true);
                inviteBtn.gameObject.SetActive(false);
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
    private async void GetFriendRequestToList() {
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
    /// 設定ボタンを押したら
    /// </summary>
    public void OnClickSettingBtn() {
        if (settingPanel.activeSelf) {
            settingPanel.SetActive(false);
        }
        else {
            settingPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 感度変更スライダー
    /// </summary>
    public void OnChangedSensSlider() {
        float sens = sensSlider.value;
        if (sens < 0.01f) {
            sens = 0.01f;
            sensSlider.value = sens;
        } 

        sensInputField.text = sens.ToString();

        CharacterSettings.Instance.ChangeSens(sens);
    }

    /// <summary>
    /// 感度変更InputField
    /// </summary>
    public void OnChangedSensInputField() {
        float sens;
        bool parseResult = float.TryParse(sensInputField.text, out sens);
        if (parseResult) {
            if (sens < 0.01f) {
                sens = 0.01f;
                sensSlider.value = sens;
            }
            else if (sens > 1) {
                sens = 1;
            }

            sensSlider.value = sens;
            sensInputField.text = sens.ToString();
            CharacterSettings.Instance.ChangeSens(sens);
        }
        else {
            OnChangedSensSlider();
        }
    }

    /// <summary>
    /// 音量変更スライダー
    /// </summary>
    public void OnChangedAudioVolumeSlider() {
        float volume = audioValumeSlider.value;

        audioValumeInputField.text = volume.ToString();
        AudioManager.Instance.ChangeAllAudioSourceVolume(volume);
    }

    /// <summary>
    /// 音量変更InputField
    /// </summary>
    public void OnChangedAudioVolumeInputField() {
        float volume;
        bool parseResult = float.TryParse(audioValumeInputField.text, out volume);
        if (parseResult) {
            if (volume < 0) {
                volume = 0;
            }
            else if (volume > 1) {
                volume = 1;
            }

            audioValumeSlider.value = volume;
            audioValumeInputField.text = volume.ToString();
            AudioManager.Instance.ChangeAllAudioSourceVolume(volume);
        }
        else {
            OnChangedAudioVolumeSlider();
        }
    }


    /// <summary>
    /// ロードアウトアイテムセレクトボタン共有
    /// </summary>
    public void OnClickItemSelectBtn(string select) {
        // 子オブジェクトを削除
        foreach (Transform child in itemSelectParent) {
            Destroy(child.gameObject);
        }

        itemSelects.SetActive(false);
        selecter.SetActive(true);

        // 弾のデータ
        BulletDataSO BDSO = CharacterSettings.Instance.BDSO;
        // サブ武器のデータ
        SubWeaponDataSO SWDSO = CharacterSettings.Instance.SWDSO;
        // アルティメットのデータ
        UltimateDataSO UDSO = CharacterSettings.Instance.UDSO;
        // キャラクター装備メッシュ
        CharacterEquipmentSO CESO = CharacterSettings.Instance.CESO;

        switch (select) {
            case "MainWeapon":
                // 選択中のアイテム画像設定
                selectiongImage.sprite = CharacterSettings.Instance.GetCharacterCharacterTypeSprite();

                foreach (var item in BDSO.bulletDataList) {
                    GameObject createdUI = Instantiate(itemSelectPrefab, parent: itemSelectParent);
                    // イメージ画像を設定
                    createdUI.GetComponentsInChildren<Image>().First(_ => _.gameObject.name == "ItemPanel").sprite = item.LoadoutSprite;
                    createdUI.GetComponentInChildren<Text>().text = item.CharacterType.ToString();
                    createdUI.GetComponent<Button>().onClick.AddListener(() => {
                        // 武器変更
                        CharacterSettings.Instance.CharacterType = item.CharacterType;
                        // 選択中のアイテム画像設定
                        selectiongImage.sprite = CharacterSettings.Instance.GetCharacterCharacterTypeSprite();
                        // 武器選択ボタンの画像設定
                        weaponSelectImages.First(_ => _.Key == select).Value.sprite = item.LoadoutSprite;
                    });
                }

                break;

            case "SubWeapon":
                // 選択中のアイテム画像設定
                selectiongImage.sprite = CharacterSettings.Instance.GetCharacterSubWeaponSprite();

                foreach (var item in SWDSO.subWeaponDataList) {
                    GameObject createdUI = Instantiate(itemSelectPrefab, parent: itemSelectParent);
                    // イメージ画像を設定
                    createdUI.GetComponentsInChildren<Image>().First(_ => _.gameObject.name == "ItemPanel").sprite = item.LoadoutSprite;
                    createdUI.GetComponentInChildren<Text>().text = item.SubWeapon.ToString();
                    createdUI.GetComponent<Button>().onClick.AddListener(() => {
                        // 武器変更
                        CharacterSettings.Instance.SubWeapon = item.SubWeapon;
                        // 選択中のアイテム画像設定
                        selectiongImage.sprite = CharacterSettings.Instance.GetCharacterSubWeaponSprite();
                        // 武器選択ボタンの画像設定
                        weaponSelectImages.First(_ => _.Key == select).Value.sprite = item.LoadoutSprite;
                    });
                }

                break;

            case "Ultimate":
                // 選択中のアイテム画像設定
                selectiongImage.sprite = CharacterSettings.Instance.GetCharacterUltimateSprite();

                foreach (var item in UDSO.ultimateDataList) {
                    GameObject createdUI = Instantiate(itemSelectPrefab, parent: itemSelectParent);
                    // イメージ画像を設定
                    createdUI.GetComponentsInChildren<Image>().First(_ => _.gameObject.name == "ItemPanel").sprite = item.LoadoutSprite;
                    createdUI.GetComponentInChildren<Text>().text = item.Ultimate.ToString();
                    createdUI.GetComponent<Button>().onClick.AddListener(() => {
                        // 武器変更
                        CharacterSettings.Instance.Ultimate = item.Ultimate;
                        // 選択中のアイテム画像設定
                        selectiongImage.sprite = CharacterSettings.Instance.GetCharacterUltimateSprite();
                        // 武器選択ボタンの画像設定
                        weaponSelectImages.First(_ => _.Key == select).Value.sprite = item.LoadoutSprite;
                    });
                }

                break;

            case "Hat":
            case "Accessories":
            case "Pants":
            case "Hairstyle":
            case "Outerwear":
            case "Shoes":
                // 選択中のアイテム画像設定
                selectiongImage.sprite = CharacterSettings.Instance.GetCharacterEquipmentSprite(select);

                foreach (var item in CESO.characterEquipment.First(_ => _.name == select).equipment.ToList()) {
                    GameObject createdUI = Instantiate(itemSelectPrefab, parent: itemSelectParent);
                    // イメージ画像を設定
                    createdUI.GetComponentsInChildren<Image>().First(_ => _.gameObject.name == "ItemPanel").sprite = item.sprite;
                    createdUI.GetComponentInChildren<Text>().text = item.name;

                    // ボタンイベントを設定
                    Character_Skin_Part part = (Character_Skin_Part)Enum.Parse(typeof(Character_Skin_Part), select);
                    createdUI.GetComponent<Button>().onClick.AddListener(async () => {
                        // 装備変更
                        CharacterSettings.Instance.ChangeCharacterEquipment(part, item.name);
                        // 選択中のアイテム画像設定
                        selectiongImage.sprite = CharacterSettings.Instance.GetCharacterEquipmentSprite(part.ToString());
                        // 部位選択ボタンの画像設定
                        skinPartSelectImages.First(_ => _.Key == select).Value.sprite = item.sprite;
                    });
                }

                break;
        }
    }

    /// <summary>
    /// ロードアウトアイテムセレクトから戻る
    /// </summary>
    public void OnClickBackItemSelectBtn() {
        itemSelects.SetActive(true);
        selecter.SetActive(false);
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

        // TeamUserのIsReadyを変更
        lobyManager.ChangeTeamUserDataIsReady(connectionId, IsReady);

        // マッチングボタンを押せるか判定
        IsOnClickMatchingBtn();

        // ステータステキストを設定
        string statusText;
        if (IsReady) {
            statusText = $"<b>{lobyManager.TeamPlayerList[connectionId].joinedData.UserData.Display_Name}</b>\n<color=green>準備完了</color>";
        }
        else {
            statusText = $"<b>{lobyManager.TeamPlayerList[connectionId].joinedData.UserData.Display_Name}</b>\n<color=red>準備中</color>";
        }
        playerStatusTextList[connectionId].text = statusText ;
    }
}
