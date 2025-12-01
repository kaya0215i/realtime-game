using DG.Tweening;
using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GameDirector : MonoBehaviour {
    [SerializeField] GameObject characterPrefab;
    Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>();
    [SerializeField] GameObject objectPrefab;
    Dictionary<Guid, GameObject> objectList = new Dictionary<Guid, GameObject>();
    RoomModel roomModel;
    UserModel userModel;
    PlayerDirector playerDirector;

    int myUserId = 1; // 自分のユーザーID
    Guid myConnectionId; // 自分の接続ID
    User myself; // 自分のユーザー情報を保存
    public bool isJoined;

    [SerializeField] Text roomNameInputFieldText;
    [SerializeField] Text userIdInputFieldText;

    float updateTransformTime;

    private async void Start() {
        roomModel = GetComponent<RoomModel>();
        userModel = GetComponent<UserModel>();

        // ユーザーが入退室したときにメソッドを実行するよう、モデルに登録しておく
        roomModel.OnJoinedUser += this.OnJoinedUser;
        roomModel.OnLeavedUser += this.OnLeavedUser;

        // ユーザーのTransfromを反映
        roomModel.OnUpdatedTransformUser += this.OnUpdatedTransformUser;

        // オブジェクトが作成されたら
        roomModel.OnCreatedObject += this.OnCreatedObject;

        // オブジェクトが破棄されたら
        roomModel.OnDestroyedObject += this.OnDestroyedObject;

        // オブジェクトのTransformを反映
        roomModel.OnUpdatedObjectTransform += this.OnUpdateObjectTransform;

        GameObject characterObject = Instantiate(characterPrefab); // プレイヤーインスタンス生成
        characterObject.transform.position = Vector3.one;
        playerDirector = characterObject.GetComponent<PlayerDirector>();

        isJoined = false;

        // 接続
        await roomModel.ConnectAsync();
    }

    private async void Update() {
        updateTransformTime += Time.deltaTime;
        if (updateTransformTime >= 0.1f) {
            updateTransformTime = 0;

            if (isJoined) {
                await roomModel.UpdateTransformAsync(playerDirector.transform.position, playerDirector.transform.rotation);
            }
        }
    }

    public async void JoinRoom() {
        // 入室
        string roomName = roomNameInputFieldText.text;

        if (roomName.Length < 1) {
            Debug.Log("ルーム名を入力してください");
            return;
        }

        int userId = int.Parse(userIdInputFieldText.text);

        myUserId = userId;

        try {
            // ユーザー情報を取得
            myself = await userModel.GetUserAsync(myUserId);
        }
        catch (Exception e) {
            Debug.LogError("GetUser failed");
            Debug.LogException(e);
        }

        await roomModel.JoinAsync(roomName, userId);
    }

    public async void LeaveRoom() {
        // 退出
        isJoined = false;

        await roomModel.LeaveAsync();

        foreach (GameObject character in characterList.Values) {
            Destroy(character);
        }

        characterList = new Dictionary<Guid, GameObject>();
    }

    // ユーザーが入室したときの処理
    private void OnJoinedUser(JoinedUser user) {
        isJoined = true;

        // すでに表示済みのユーザーは追加しない
        if (characterList.ContainsKey(user.ConnectionId)) {
            return;
        }

        // 自分は追加しない
        if(user.UserData.Id == myUserId) {
            myConnectionId = user.ConnectionId;
            return;
        }

        GameObject characterObject = Instantiate(characterPrefab); // インスタンス生成
        characterObject.transform.position = Vector3.zero;
        characterObject.GetComponent<PlayerDirector>().enabled = false;
        characterList[user.ConnectionId] = characterObject; // フィールドで保持

        Debug.Log("接続ID : " + user.ConnectionId + ", ユーザーID : " + user.UserData.Id + ", ユーザー名 : " + user.UserData.Name);
    }

    // ユーザーが退出したときの処理
    private void OnLeavedUser(Guid connectionId) {
        if(myConnectionId == connectionId) {
            return;
        }

        Destroy(characterList[connectionId]);
        characterList.Remove(connectionId);
    }

    // ユーザーのTransfromを反映
    private void OnUpdatedTransformUser(Guid connectionId, Vector3 pos, Quaternion rotate) {
        if(myConnectionId == connectionId) {
            return;
        }

        if (characterList.ContainsKey(connectionId)) {
            characterList[connectionId].transform.DOMove(pos, 0.1f).SetEase(Ease.InOutQuad);
            characterList[connectionId].transform.DORotateQuaternion(rotate, 0.1f).SetEase(Ease.InOutQuad);
        }
    }

    // オブジェクトをリストに追加
    public void AddObjectList(Guid objectId, GameObject obj) {
        objectList[objectId] = obj;
    }

    // オブジェクトを作成
    private void OnCreatedObject(Guid connectionId, Guid objectId, Vector3 pos, Quaternion rotate) {
        if (myConnectionId == connectionId) {
            return;
        }

        GameObject obj = Instantiate(objectPrefab, pos, rotate); // インスタンス生成
        obj.GetComponent<NetworkObject>().myObjectId = objectId;
        objectList[objectId] = obj; // フィールドで保持
    }

    // オブジェクトをリストから削除
    public void RemoveObjectList(Guid objectId) {
        objectList.Remove(objectId);
    }

    // オブジェクトを破棄
    private void OnDestroyedObject(Guid connectionId, Guid objectId) {
        if (myConnectionId == connectionId) {
            return;
        }

        objectList[objectId].gameObject.GetComponent<NetworkObject>().sendDestroyMessage = false;
        Destroy(objectList[objectId].gameObject);
        objectList.Remove(objectId);
    }

    // オブジェクトのTransformを反映
    private void OnUpdateObjectTransform(Guid connectionId, Guid objectId, Vector3 pos, Quaternion rotate) {
        if (myConnectionId == connectionId) {
            return;
        }

        if (objectList.ContainsKey(objectId)) {
            objectList[objectId].transform.DOMove(pos, 0.1f).SetEase(Ease.InOutQuad);
            objectList[objectId].transform.DORotateQuaternion(rotate, 0.1f).SetEase(Ease.InOutQuad);
        }
    }
}
