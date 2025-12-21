using Cysharp.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared.Interfaces.StreamingHubs;
using realtime_game.Shared.Models.Entities;
using System;
using System.Threading.Tasks;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NetworkObject;
using static PlayerManager;

public class RoomModel : BaseModel, IRoomHubReceiver {
    // シングルトンにする
    private static RoomModel instance;
    public static RoomModel Instance {
        get {
            if(instance == null) {
                GameObject obj = new GameObject("RoomModel");
                instance = obj.AddComponent<RoomModel>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private GrpcChannelx channel;
    private IRoomHub roomHub;

    // チームID
    public Guid TeamId { get; private set; }

    // リーダーかどうか
    public bool IsLeader {  get; private set; }

    // ルーム名
    private string roomName;

    // 接続ID
    public Guid ConnectionId { get; set; }

    // ロビー用ユーザー接続通知
    public Action<JoinedUser> OnJoinedLobyUser { get; set; }

    // ロビー用ユーザー退出通知
    public Action<Guid, int> OnLeavedLobyUser { get; set; }

    // ユーザー接続通知
    public Action<JoinedUser> OnJoinedRoomUser { get; set; }

    // ユーザー退出通知
    public Action<Guid, int> OnLeavedRoomUser { get; set; }

    // チーム参加通知
    public Action<JoinedUser> OnJoinedTeamUser { get; set; }

    // チーム退出通知
    public Action<Guid> OnLeavedTeamUser { get; set; }

    // チームに招待通知
    public Action<Guid, User> OnInvitedTeamUser { get; set; }

    // 準備状態通知
    public Action<Guid, bool> OnIsReadyedStatusUser { get; set; }

    // マッチング通知
    public Action<string> OnMatchingedRoomUser { get; set; }

    // ゲームシーンに移動した通知
    public Action<Guid> OnWentGameRoomUser { get; set; }

    // ロビーに帰ってきたとき通知
    public Action<Guid> OnReturnedLobyRoomUser { get; set; }

    // ユーザーTransform更新通知
    public Action<Guid, Vector3, Quaternion, Quaternion> OnUpdatedTransformUser { get; set; }

    // オブジェクトの作成通知
    public Action<Guid, Guid, int, Vector3, Quaternion, UpdateObjectTypes> OnCreatedObject { get; set; }

    // オブジェクトの破棄通知
    public Action<Guid> OnDestroyedObject { get; set; }

    // オブジェクトのTransform更新通知
    public Action<Guid, Vector3, Quaternion> OnUpdatedObjectTransform { get; set; }

    // オブジェクトのインタラクトをFalseにする通知
    public Action<Guid> OnFalsedObjectInteracting { get; set; }

    // ゲームスタート通知
    public Action OnGameStarted { get; set; }

    // ゲーム終了通知
    public Action OnGameEnded { get; set; }

    // ゲームタイマー更新通知
    public Action<float> OnUpdatedGameTimer { get; set; }

    // キャラクタータイプ変更通知
    public Action<Guid, PLAYER_CHARACTER_TYPE> OnChangedCharacterTypeUser { get; set; }

    // プレイヤーのリスポーン通知
    public Action<Guid> OnReSpownedPlayer { get; set; }

    // プレイヤーの死亡通知
    public Action<Guid, Guid> OnDeadPlayer { get; set; }

    // プレイヤーのヒットパーセント通知
    public Action<Guid, float> OnHitedPercentUser { get; set; }

    /// <summary>
    /// MagicOnion接続処理
    /// </summary>
    public async UniTask ConnectAsync() {
        channel = GrpcChannelx.ForAddress(ServerURL);
        roomHub = await StreamingHubClient.ConnectAsync<IRoomHub, IRoomHubReceiver>(channel, this);
        this.ConnectionId = await roomHub.GetConnectionId();
    }

    /// <summary>
    /// MagicOnion切断処理
    /// </summary>
    public async UniTask DisconnectAsync() {
        if(roomHub != null) await roomHub.DisposeAsync();
        if(channel != null) await channel.ShutdownAsync();
        roomHub = null;
        channel = null;
    }

    /// <summary>
    /// 破棄処理
    /// </summary>
    private void OnDestroy() {
        DisconnectAsync().Forget();
    }

    /// <summary>
    /// ゲーム終了時
    /// </summary>
    private void OnApplicationQuit() {
        DisconnectAsync().Forget();
    }


    /*
     * 
     * ロビー内
     * 
     */


    /// <summary>
    /// コネクションID取得
    /// </summary>
    public async UniTask<Guid> GetConnectionIdAsync() {
        return await roomHub.GetConnectionId();
    }

    /// <summary>
    /// ロビールームに入室
    /// </summary>
    public async UniTask<bool> JoinLobyAsync() {
        if (roomHub != null) {
            try {
                JoinedUser[] users = await roomHub.JoinLobyAsync(UserModel.Instance.UserId);
                foreach (var user in users) {
                    if (OnJoinedLobyUser != null) {
                        OnJoinedLobyUser(user);
                    }
                }

                return true;
            }
            catch (RpcException e) {
                Debug.LogException(e);
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// [サーバー通知]
    /// ロビーの入室通知
    /// </summary>
    public void OnJoinLoby(JoinedUser user) {
        if (OnJoinedLobyUser != null) {
            OnJoinedLobyUser(user);
        }
    }

    /// <summary>
    /// ロビールームから退室
    /// </summary>
    public async UniTask LeaveLobyAsync() {
        if (roomHub != null) {
            await roomHub.LeaveLobyAsync();
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// ロビーの退室通知
    /// </summary>
    public void OnLeaveLoby(Guid connectionId, int joinOrder) {
        if (OnLeavedLobyUser != null) {
            OnLeavedLobyUser(connectionId, joinOrder);
        }
    }


    /// <summary>
    /// ロビーユーザー情報を取得
    /// </summary>
    public async UniTask GetLobyUsersAsync() {
        if (roomHub != null) {
            JoinedUser[] users = await roomHub.GetLobyUsersAsync();
            // 情報を取得出来てたら
            if (users != null) {
                foreach (var user in users) {
                    if (OnJoinedLobyUser != null) {
                        OnJoinedLobyUser(user);
                    }
                }
            }
        }
    }


    /// <summary>
    /// チームを作成
    /// </summary>
    public async UniTask CreateTeamAndJoinAsync() {
        TeamId = await roomHub.CreateTeamAndJoinAsync();
        IsLeader = true;
    }

    /// <summary>
    /// チームに参加
    /// </summary>
    public async UniTask JoinTeamAsync(Guid targetTeamId) {
        if (roomHub != null) {
            JoinedUser[] users = await roomHub.JoinTeamAsync(targetTeamId);

            TeamId = targetTeamId;

            // 配列の長さが0の(もうチームに入っている)場合何もしない
            if (users.Length == 0) {
                return;
            }
            foreach (var user in users) {
                if (OnJoinedTeamUser != null) {
                    OnJoinedTeamUser(user);
                }
            }
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// チームの参加通知
    /// </summary>
    public void OnJoinTeam(JoinedUser user) {
        if (OnJoinedTeamUser != null) {
            OnJoinedTeamUser(user);
        }
    }

    /// <summary>
    /// チームから退出
    /// </summary>
    public async UniTask LeaveTeamAsync() {
        if (roomHub != null) {
            await roomHub.LeaveTeamAsync();
            IsLeader = false;
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// チームの退出通知
    /// </summary>
    public void OnLeaveTeam(Guid connectionId) {
        if (OnLeavedTeamUser != null) {
            OnLeavedTeamUser(connectionId);
        }
    }


    /// <summary>
    /// チームにフレンドを招待
    /// </summary>
    public async UniTask InviteTeamFriendAsync(Guid targetConnectionId) {
        if (roomHub != null) {
            await roomHub.InviteTeamFriendAsync(targetConnectionId, TeamId);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// チームに招待通知
    /// </summary>
    public void OnInviteTeam(Guid teamId, User senderUser) {
        if (OnInvitedTeamUser != null) {
            OnInvitedTeamUser(teamId, senderUser);
        }
    }


    /// <summary>
    /// チームに入っているか
    /// </summary>
    public async UniTask<bool> IsAlreadyInTeamAsync() {
        if (roomHub != null) {
            try {
                return await roomHub.IsAlreadyInTeamAsync();
            }
            catch {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// チームメンバー情報を取得
    /// </summary>
    public async UniTask GetTeamUsersAsync() {
        if (roomHub != null) {
            JoinedUser[] users = await roomHub.GetTeamUsersAsync();
            // 情報を取得出来てたら
            if (users != null) {
                foreach (var user in users) {
                    if (OnJoinedTeamUser != null) {
                        OnJoinedTeamUser(user);
                    }
                }
            }
        }
    }


    /// <summary>
    /// 準備状態を送る
    /// </summary>
    public async UniTask SendIsReadyStatusAsync(bool isReady) {
        if (roomHub != null) {
            await roomHub.SendIsReadyStatusAsync(isReady);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// 準備状態通知
    /// </summary>
    public void OnIsReadyStatus(Guid connectionId, bool IsReady) {
        if (OnIsReadyedStatusUser != null) {
            OnIsReadyedStatusUser(connectionId, IsReady);
        }
    }


    /// <summary>
    /// ロビーに帰ってきたとき
    /// </summary>
    public async UniTask ReturnLobyRoomAsync() {
        if (roomHub != null) {
            await roomHub.ReturnLobyRoomAsync();
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// ロビーに帰ってきたとき通知
    /// </summary>
    public void OnReturnedLobyRoom(Guid connectionId) {
        if (OnReturnedLobyRoomUser != null) {
            OnReturnedLobyRoomUser(connectionId);
        }
    }


    /// <summary>
    /// マッチングする　ルームを探してなかったら作る
    /// </summary>
    public async UniTask StartMatchingAsync() {
        if (roomHub != null) {
            await roomHub.StartMatchingAsync();
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// マッチング通知
    /// </summary>
    public void OnMatchingRoom(string roomName) {
        if (OnMatchingedRoomUser != null) {
            this.roomName = roomName;
            OnMatchingedRoomUser(roomName);
        }
    }


    /// <summary>
    /// [サーバー通知]
    /// ゲームシーンに移動した通知
    /// </summary>
    public void OnGoGameRoom(Guid connectionId) {
        if (OnWentGameRoomUser != null) {
            OnWentGameRoomUser(connectionId);
        }
    }


    /*
     * 
     * インゲーム内
     * 
     */


    /// <summary>
    /// 入室処理
    /// </summary>
    public async UniTask<bool> JoinRoomAsync() {
        if (roomHub != null) {
            try {
                JoinedUser[] users = await roomHub.JoinRoomAsync(this.roomName, UserModel.Instance.UserId);

                foreach (var user in users) {
                    if (OnJoinedRoomUser != null) {
                        OnJoinedRoomUser(user);
                    }
                }
                return true;
            }
            catch (RpcException e) {
                Debug.LogException(e);
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// [サーバー通知]
    /// 入室通知 (IRoomHubReceiverインタフェースの実装)
    /// </summary>
    public void OnJoinRoom(JoinedUser user) {
        if (OnJoinedRoomUser != null) {
            OnJoinedRoomUser(user);
        }
    }

    /// <summary>
    /// 退出処理
    /// </summary>
    public async UniTask LeaveRoomAsync() {
        await roomHub.LeaveRoomAsync(roomName);
    }

    /// <summary>
    /// [サーバー通知]
    /// 退出通知 (IRoomHubReceiverインタフェースの実装)
    /// </summary>
    public void OnLeaveRoom(Guid connectionId, int joinOrder) {
        if (OnLeavedRoomUser != null) {
            OnLeavedRoomUser(connectionId, joinOrder);
        }
    }



    /// <summary>
    /// 現在のインゲームの情報を取得
    /// </summary>
    public async UniTask<InGameData> GetInGameDataAsync() {
        if (roomHub != null) {
            return await roomHub.GetInGameDataAsync();
        }

        return null;
    }



    /// <summary>
    /// ゲームスタート
    /// </summary>
    public async UniTask GameStartAsync() {
        if(roomHub != null) {
            await roomHub.GameStartAsync();
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// ゲームスタート通知
    /// </summary>
    public void OnGameStart() {
        if (OnGameStarted != null) {
            OnGameStarted();
        }
    }

    /// <summary>
    /// ゲーム終了
    /// </summary>
    public async UniTask GameEndAsync() {
        if (roomHub != null) {
            await roomHub.GameEndAsync();
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// ゲーム終了通知
    /// </summary>
    public void OnGameEnd() {
        if (OnGameEnded != null) {
            OnGameEnded();
        }
    }


    /// <summary>
    /// ゲームタイマー更新
    /// </summary>
    public async UniTask UpdateGameTimerAsync(float deltaTime) {
        if(roomHub != null) {
            await roomHub.UpdateGameTimerAsync(deltaTime);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// ゲームタイマーの更新通知
    /// </summary>
    public void OnUpdateGameTimer(float timer) {
        if (OnUpdatedGameTimer != null) {
            OnUpdatedGameTimer(timer);
        }
    }


    /// <summary>
    /// キャラクタータイプ変更
    /// </summary>
    public async UniTask ChangeCharacterTypeAsync(PLAYER_CHARACTER_TYPE type) {
        if (roomHub  != null) {
            await roomHub.ChangeCharacterTypeAsync((int)type);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// キャラクタータイプ変更通知
    /// </summary>
    public void OnChangeCharacterType(Guid connectionId, int typeNum) {
        if (OnChangedCharacterTypeUser  != null) {
            OnChangedCharacterTypeUser(connectionId, (PLAYER_CHARACTER_TYPE)Enum.ToObject(typeof(PLAYER_CHARACTER_TYPE), typeNum));
        }
    }


    /// <summary>
    /// プレイヤーのリスポーン
    /// </summary>
    public async UniTask ReSpownPlayerAsync() {
        if (roomHub != null) {
            await roomHub.ReSpownPlayerAsync();
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// プレイヤーのリスポーン通知
    /// </summary>
    public void OnReSpownPlayer(Guid connectionId) {
        if (OnReSpownedPlayer  != null) {
            OnReSpownedPlayer(connectionId);
        }
    }
    
    /// <summary>
    /// プレイヤー死亡
    /// </summary>
    public async UniTask DeathPlayerAsync(Guid killedPlayerConnectionId) {
        if (roomHub != null) {
            await roomHub.DeathPlayerAsync(killedPlayerConnectionId);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// プレイヤー死亡通知
    /// </summary>
    public void OnDeathPlayer(Guid connectionId, Guid killedPlayerConnectionId) {
        if (OnDeadPlayer != null) {
            OnDeadPlayer(connectionId, killedPlayerConnectionId);
        }
    }


    /// <summary>
    /// プレイヤーのヒットパーセント
    /// </summary>
    public async UniTask HitPercentAsync(float value) {
        if (roomHub != null) {
            await roomHub.HitPercentAsync(value);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// プレイヤーのヒットパーセント通知
    /// </summary>
    public void OnHitPercent(Guid connectionId, float value) {
        if (OnHitedPercentUser  != null) {
            OnHitedPercentUser(connectionId, value);
        }
    }


    /// <summary>
    /// ユーザーのTransformの更新
    /// </summary>
    public async UniTask UpdateUserTransformAsync(Vector3 pos, Quaternion rotate, Quaternion cameraRotate) {
        if (roomHub != null) {
            await roomHub.UpdateUserTransformAsync(pos, rotate, cameraRotate);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// ユーザーのTransformの更新通知
    /// </summary>
    public void OnUpdateUserTransform(Guid connectionId, Vector3 pos, Quaternion rotate, Quaternion cameraRotate) {
        if (OnUpdatedTransformUser != null) {
            OnUpdatedTransformUser(connectionId, pos, rotate, cameraRotate);
        }
    }


    /// <summary>
    /// オブジェクトの作成
    /// </summary>
    public async UniTask<Guid> CreateObjectAsync(int objectDataId, Vector3 pos, Quaternion rotate, UpdateObjectTypes updateType) {
        Guid objectId;

        if (roomHub != null) {
            objectId = await roomHub.CreateObjectAsync(objectDataId, pos, rotate, (int)updateType);
        }
        else {
            objectId = Guid.Empty;
        }

        return objectId;
    }

    /// <summary>
    /// [サーバー通知]
    /// オブジェクトの作成通知
    /// </summary>
    public void OnCreateObject(Guid connectionId, Guid objectId, int objectDataId, Vector3 pos, Quaternion rotate, int updateTypeNum) {
        if (OnCreatedObject != null) {
            OnCreatedObject(connectionId, objectId, objectDataId, pos, rotate, (UpdateObjectTypes)Enum.ToObject(typeof(UpdateObjectTypes), updateTypeNum));
        }
    }

    /// <summary>
    /// オブジェクトがインタラクト可能であればする
    /// </summary>
    public async UniTask<bool> InteractObjectAsync(Guid objectId) {
        if (roomHub != null) {
            return await roomHub.InteractObjectAsync(objectId);
        }

        return false;
    }

    /// <summary>
    /// オブジェクトを手放す
    /// </summary>
    public async UniTask DisInteractObjectAsync(Guid objectId) {
        if (roomHub != null) {
            await roomHub.DisInteractObjectAsync(objectId);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// オブジェクトのinteractingをfalseにする
    /// </summary>
    public void OnFalseObjectInteracting(Guid objectId) {
        if (OnFalsedObjectInteracting != null) {
            OnFalsedObjectInteracting(objectId);
        }
    }

    /// <summary>
    /// オブジェクトの破棄
    /// </summary>
    public async UniTask DestroyObjectAsync(Guid objectId) {
        if (roomHub != null) {
            await roomHub.DestroyObjectAsync(objectId);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// オブジェクトの破棄通知
    /// </summary>
    public void OnDestroyObject(Guid objectId) {
        if (OnDestroyedObject != null) {
            OnDestroyedObject(objectId);
        }
    }

    /// <summary>
    /// オブジェクトのTransformの更新
    /// </summary>
    public async UniTask UpdateObjectTransformAsync(Guid objectId, Vector3 pos, Quaternion rotate) {
        if (roomHub != null) {
            await roomHub.UpdateObjectTransformAsync(objectId, pos, rotate);
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// オブジェクトのTransformの更新通知
    /// </summary>
    public void OnUpdateObjectTransform(Guid objectId, Vector3 pos, Quaternion rotate) {
        if (OnUpdatedObjectTransform != null) {
            OnUpdatedObjectTransform(objectId, pos, rotate);
        }
    }    
}
