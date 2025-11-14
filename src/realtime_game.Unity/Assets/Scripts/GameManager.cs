using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    [SerializeField] GameObject characterPrefab;
    [SerializeField] RoomModel roomModel;
    [SerializeField] Text roomNameInputFieldText;
    Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>();

    private async void Start() {
        // ユーザーが入室したときにOnJoinedUserメソッドを実行するよう、モデルに登録しておく
        roomModel.OnJoinedUser += this.OnJoinedUser;
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

        await roomModel.JoinAsync(roomName, 1);
    }

    // ユーザーが入室したときの処理
    private void OnJoinedUser(JoinedUser user) {
        //GameObject characterObject = Instantiate(characterPrefab); // インスタンス生成
        //characterObject.transform.position = Vector3.zero;
        characterList[user.ConnectionId] = characterPrefab; // フィールドで保持

        Debug.Log("接続ID : " + user.ConnectionId + ", ユーザーID : " + user.UserData.Id + ", ユーザー名 : " + user.UserData.Name);
    }
}
