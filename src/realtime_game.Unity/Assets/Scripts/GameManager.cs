using DG.Tweening;
using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static NetworkObject;

public class GameManager : MonoBehaviour {
    // プレイヤーキャラのプレハブ
    [SerializeField] private GameObject characterPrefab;
    // プレイヤーキャラのリスト
    private Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>();
    // オブジェクトのプレハブ
    [SerializeField] public ObjectDataSO objectDataSO;
    // オブジェクトのリスト
    private Dictionary<Guid, GameObject> objectList = new Dictionary<Guid, GameObject>();

    // 自分のプレイヤーキャラ
    private GameObject myCharacter;

    private PlayerManager playerManager;
    private PlayerController playerController;

    [NonSerialized] public JoinedUser mySelf; // 自分のユーザー情報を保存
    [NonSerialized] public bool isJoined;

    [SerializeField] private Text roomNameInputFieldText;
    [SerializeField] private Text userIdInputFieldText;

    private bool isShowMouseCursor;

    private void Awake() {
        HideMouseCursor();
        isShowMouseCursor = false;
        isJoined = false;

        mySelf = new JoinedUser();
    }

    private async void Start() {
        // ユーザーが入退室したときにメソッドを実行するよう、モデルに登録しておく
        RoomModel.Instance.OnJoinedUser += this.OnJoinedUser;
        RoomModel.Instance.OnLeavedUser += this.OnLeavedUser;

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

        myCharacter = Instantiate(characterPrefab); // プレイヤーインスタンス生成
        myCharacter.transform.position = Vector3.one;
        playerManager = myCharacter.GetComponent<PlayerManager>();
        playerController = myCharacter.GetComponent<PlayerController>();
        playerController.cinemachineCamera.Priority = 10;

        // 接続
        await RoomModel.Instance.ConnectAsync();
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
        if(Input.GetKeyDown(KeyCode.Escape)) {
            isShowMouseCursor = isShowMouseCursor ? false : true;
            if(isShowMouseCursor) {
                ShowMouseCursor();
            }
            else {
                HideMouseCursor();
            }
        }
    }

    /// <summary>
    /// キャラクターオブジェクトをコネクションIDで検索して返す
    /// </summary>
    /// <param name="connectionId"></param>
    /// <returns></returns>
    public GameObject FindCharacterObject(Guid connectionId) {
         return characterList.FirstOrDefault( _ => _.Key == connectionId ).Value;
    }

    /// <summary>
    /// 入室処理
    /// </summary>
    public async void JoinRoom() {
        string roomName = roomNameInputFieldText.text;

        if (roomName.Length < 1) {
            Debug.Log("ルーム名を入力してください");
            return;
        }

        int userId = int.Parse(userIdInputFieldText.text);

        try {
            // ユーザー情報を取得
            mySelf.UserData = await UserModel.Instance.GetUserByIdAsync(userId);
        }
        catch (Exception e) {
            Debug.LogError("GetUser failed");
            Debug.LogException(e);
        }

        // 自分のコネクションIDを保存
        mySelf.ConnectionId = await RoomModel.Instance.GetConnectionIdAsync();

        playerManager.thisCharacterConnectionId = mySelf.ConnectionId;

        // 参加
        await RoomModel.Instance.JoinAsync(roomName, userId);
    }

    /// <summary>
    /// 退室処理
    /// </summary>
    public async void LeaveRoom() {
        if (!isJoined) {
            return;
        }

        isJoined = false;

        await RoomModel.Instance.LeaveAsync();

        foreach (KeyValuePair<Guid, GameObject> character in characterList) {
            if (character.Key != mySelf.ConnectionId) {
                Destroy(character.Value);
            }
        }

        mySelf = new JoinedUser();
        characterList = new Dictionary<Guid, GameObject>();
    }


    /// <summary>
    /// [サーバー通知]
    /// ユーザーが入室したときの処理
    /// </summary>
    /// <param name="user"></param>
    private void OnJoinedUser(JoinedUser user) {
        // すでに表示済みのユーザーは追加しない
        if (characterList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分は生成しない
        if (user.UserData.Id == mySelf.UserData.Id) {
            isJoined = true;

            // フィールドで保持
            myCharacter.name = "Player_" + user.JoinOrder;
            characterList[mySelf.ConnectionId] = myCharacter;

            // 参加順番を保存
            mySelf.JoinOrder = user.JoinOrder;
            return;
        }

        GameObject characterObject = Instantiate(characterPrefab); // インスタンス生成
        characterObject.name = "Player_" + user.JoinOrder;
        characterObject.GetComponent<PlayerManager>().thisCharacterConnectionId = user.ConnectionId;
        //characterObject.GetComponent<PlayerManager>().enabled = false;
        //characterObject.GetComponent<PlayerController>().enabled = false;
        characterObject.transform.position = Vector3.zero;
        characterList[user.ConnectionId] = characterObject; // フィールドで保持

        Debug.Log("接続ID : " + user.ConnectionId + ", ユーザーID : " + user.UserData.Id + ", ユーザー名 : " + user.UserData.Display_Name + ", 参加順番 : " + user.JoinOrder);
    }

    /// <summary>
    /// [サーバー通知]
    /// ユーザーが退出したときの処理
    /// </summary>
    /// <param name="connectionId"></param>
    private void OnLeavedUser(Guid connectionId, int joinOrder) {
        if (mySelf.ConnectionId == connectionId) {
            return;
        }

        // 参加順番を繰り下げ
        if (mySelf.JoinOrder > joinOrder) {
            mySelf.JoinOrder -= 1;
            myCharacter.name = "Player_" + joinOrder;
            characterList[mySelf.ConnectionId].name = "Player_" + joinOrder;
        }

        Destroy(characterList[connectionId]);
        DOTween.Kill(characterList[connectionId]);
        characterList.Remove(connectionId);
    }


    /// <summary>
    /// [サーバー通知]
    /// ユーザーのTransfromを反映
    /// </summary>
    /// <param name="connectionId"></param>
    /// <param name="pos"></param>
    /// <param name="rotate"></param>
    private void OnUpdatedTransformUser(Guid connectionId, Vector3 pos, Quaternion rotate, Quaternion cameraRotate) {
        if (mySelf.ConnectionId == connectionId) {
            return;
        }

        if (characterList.ContainsKey(connectionId) &&
            characterList[connectionId] != null) {
            characterList[connectionId].transform.DOMove(pos, 0.2f).SetEase(Ease.InOutQuad);
            characterList[connectionId].transform.DORotateQuaternion(rotate, 0.2f).SetEase(Ease.InOutQuad);

            characterList[connectionId].GetComponent<PlayerManager>().cameraRotate = cameraRotate;
        }
    }


    /// <summary>
    /// オブジェクトをリストに追加
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="obj"></param>
    public void AddObjectList(Guid objectId, GameObject obj) {
        objectList[objectId] = obj;
    }

    /// <summary>
    /// [サーバー通知]
    /// オブジェクトを作成
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectDataId"></param>
    /// <param name="pos"></param>
    /// <param name="rotate"></param>
    /// <param name="updateType"></param>
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
    /// <param name="objectId"></param>
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
    /// <param name="objectId"></param>
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
    /// <param name="objectId"></param>
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
    /// <param name="objectId"></param>
    /// <param name="pos"></param>
    /// <param name="rotate"></param>
    private void OnUpdatedObjectTransform(Guid objectId, Vector3 pos, Quaternion rotate) {
        if (objectList.ContainsKey(objectId) &&
            objectList[objectId] != null) {
            objectList[objectId].transform.DOMove(pos, 0.2f).SetEase(Ease.InOutQuad);
            objectList[objectId].transform.DORotateQuaternion(rotate, 0.2f).SetEase(Ease.InOutQuad);
        }
    }
}
