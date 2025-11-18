using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameDirector : MonoBehaviour {
    [SerializeField] GameObject characterPrefab;
    Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>();
    RoomModel roomModel;
    UserModel userModel;

    int myUserId = 1; // 自分のユーザーID
    Guid myConnectionId; // 自分の接続ID
    User myself; // 自分のユーザー情報を保存

    [SerializeField] Text roomNameInputFieldText;
    [SerializeField] Text userIdInputFieldText;

    private async void Start() {
        roomModel = GetComponent<RoomModel>();
        userModel = GetComponent<UserModel>();

        // ユーザーが入退室したときにメソッドを実行するよう、モデルに登録しておく
        roomModel.OnJoinedUser += this.OnJoinedUser;
        roomModel.OnLeavedUser += this.OnLeavedUser;
        // 接続
        await roomModel.ConnectAsync();
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
        await roomModel.LeaveAsync();

        foreach (GameObject character in characterList.Values) {
            Destroy(character);
        }

        characterList = new Dictionary<Guid, GameObject>();
    }

    // ユーザーが入室したときの処理
    private void OnJoinedUser(JoinedUser user) {
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
}
