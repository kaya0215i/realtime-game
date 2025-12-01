using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class RoomModel : BaseModel, IRoomHubReceiver {
    private GrpcChannelx channel;
    private IRoomHub roomHub;

    private GameManager gameManager;

    // 接続ID
    public Guid ConnectionId { get; set; }

    // ユーザー接続通知
    public Action<JoinedUser> OnJoinedUser { get; set; }

    // ユーザー退出通知
    public Action<Guid> OnLeavedUser { get; set; }

    // ユーザーTransform更新通知
    public Action<Guid, Vector3, Quaternion> OnUpdatedTransformUser { get; set; }

    // オブジェクトの作成通知
    public Action<Guid, Guid, Vector3, Quaternion> OnCreatedObject { get; set; }

    // オブジェクトの破棄通知
    public Action<Guid, Guid> OnDestroyedObject { get; set; }

    // オブジェクトのTransform更新通知
    public Action<Guid, Guid, Vector3, Quaternion> OnUpdatedObjectTransform { get; set; }

    private void Start() {
        gameManager = this.GetComponent<GameManager>();
    }

    // MagicOnion接続処理
    public async UniTask ConnectAsync() {
        channel = GrpcChannelx.ForAddress(ServerURL);
        roomHub = await StreamingHubClient.ConnectAsync<IRoomHub, IRoomHubReceiver>(channel, this);
        this.ConnectionId = await roomHub.GetConnectionId();
    }

    // MagicOnion切断処理
    public async UniTask DisconnectAsync() {
        if(roomHub != null) await roomHub.DisposeAsync();
        if(channel != null) await channel.ShutdownAsync();
        roomHub = null;
        channel = null;
    }

    // 破棄処理
    private async void OnDestroy() {
        DisconnectAsync();
    }

    // 入室
    public async UniTask JoinAsync(string roomName, int userId) {
        JoinedUser[] users = await roomHub.JoinAsync(roomName, userId);
        foreach(var user in users) {
            if(OnJoinedUser != null) {
                OnJoinedUser(user);
            }
        }
    }

    //　入室通知 (IRoomHubReceiverインタフェースの実装)
    public void OnJoin(JoinedUser user) {
        if (OnJoinedUser != null) {
            OnJoinedUser(user);
        }
    }

    // 退出
    public async UniTask LeaveAsync() {
        await roomHub.LeaveAsync();
        if(OnLeavedUser != null) {
            OnLeavedUser(this.ConnectionId);
        }
    }

    // 退出通知 (IRoomHubReceiverインタフェースの実装)
    public void OnLeave(Guid connectionId) {
        if (OnLeavedUser != null) {
            OnLeavedUser(connectionId);
        }
    }

    // Transformの更新
    public async UniTask UpdateTransformAsync(Vector3 pos, Quaternion rotate) {
        if (roomHub != null) {
            await roomHub.UpdateTransformAsync(pos, rotate);
        }
    }

    // Transformの更新通知
    public void OnUpdateTransform(Guid connectionId, Vector3 pos, Quaternion rotate) {
        if (OnUpdatedTransformUser != null) {
            OnUpdatedTransformUser(connectionId, pos, rotate);
        }
    }

    // オブジェクトの作成
    public async UniTask<Guid> CreateObjectAsync(Vector3 pos, Quaternion rotate, GameObject obj) {
        Guid objectId;

        if (roomHub != null) {
            objectId = await roomHub.CreateObjectAsync(pos, rotate);
            gameManager.AddObjectList(objectId, obj);
        }
        else {
            objectId = Guid.Empty;
        }

        return objectId;
    }

    // オブジェクトの破棄
    public async UniTask DestroyObjectAsync(Guid objectId) {
        if (roomHub != null) {
            await roomHub.DestroyObjectAsync(objectId);
        }
    }

    // オブジェクトのTransformの更新
    public async UniTask UpdateObjectTransformAsync(Guid objectId, Vector3 pos, Quaternion rotate) {
        if (roomHub != null) {
            await roomHub.UpdateObjectTransformAsync(objectId, pos, rotate);
        }
    }

    // オブジェクトの作成通知
    public void OnCreateObject(Guid connectionId, Guid objectId, Vector3 pos, Quaternion rotate) {
        if (OnCreatedObject != null) {
            OnCreatedObject(connectionId, objectId, pos, rotate);
        }
    }

    // オブジェクトの破棄通知
    public void OnDestroyObject(Guid connectionId, Guid objectId) {
        if(OnDestroyedObject  != null) {
            OnDestroyedObject(connectionId, objectId);
        }
    }

    // オブジェクトのTransformの更新通知
    public void OnUpdateObjectTransform(Guid connectionId, Guid objectId, Vector3 pos, Quaternion rotate) {
        if (OnUpdatedObjectTransform != null) {
            OnUpdatedObjectTransform(connectionId, objectId, pos, rotate);
        }
    }
}
