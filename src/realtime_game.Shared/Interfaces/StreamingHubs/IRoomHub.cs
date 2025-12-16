using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MagicOnion;
using UnityEngine;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// クライアントから呼び出す処理を実装するクラス用インターフェース
    /// </summary>
    public interface IRoomHub : IStreamingHub<IRoomHub, IRoomHubReceiver> {
        // [サーバーに実装]
        // [クライアントから呼び出す]

        /// <summary>
        /// ユーザー入室
        /// </summary>
        Task<JoinedUser[]> JoinRoomAsync(string roomName, int userId);

        /// <summary>
        /// ユーザー退室
        /// </summary>
        Task LeaveRoomAsync(string roomName);

        /// <summary>
        /// ロビールームの入室
        /// </summary>
        Task<JoinedUser[]> JoinLobyAsync(int userId);

        /// <summary>
        /// ロビールームの退室
        /// </summary>
        Task LeaveLobyAsync();

        /// <summary>
        /// チームを作成
        /// </summary>
        Task<Guid> CreateTeamAndJoinAsync();

        /// <summary>
        /// チームに参加
        /// </summary>
        Task<JoinedUser[]> JoinTeamAsync(Guid targetTeamId);

        /// <summary>
        /// チームを抜ける
        /// </summary>
        Task LeaveTeamAsync();

        /// <summary>
        /// チームにフレンドを招待
        /// </summary>
        Task InviteTeamFriendAsync(Guid targetConnectionId, Guid teamId);

        /// <summary>
        /// 準備状態を通知
        /// </summary>
        Task SendIsReadyStatusAsync(bool isReady);

        /// <summary>
        /// マッチングする　ルームを探してなかったら作る
        /// </summary>
        Task StartMatchingAsync();

        /// <summary>
        /// 接続ID取得
        /// </summary>
        Task<Guid> GetConnectionId();

        /// <summary>
        /// ユーザーTransform更新
        /// </summary>
        Task UpdateUserTransformAsync(Vector3 pos, Quaternion rotate, Quaternion cameraRotate);

        /// <summary>
        /// オブジェクトの作成
        /// </summary>
        Task<Guid> CreateObjectAsync(int objectDataId, Vector3 pos, Quaternion rotate, int updateTypeNum);

        /// <summary>
        /// オブジェクトの破棄
        /// </summary>
        Task DestroyObjectAsync(Guid objectId);

        /// <summary>
        /// オブジェクトのTransform更新
        /// </summary>
        Task UpdateObjectTransformAsync(Guid objectId, Vector3 pos, Quaternion rotate);

        /// <summary>
        /// オブジェクトがインタラクト可能であればする
        /// </summary>
        Task<bool> InteractObjectAsync(Guid objectId);

        /// <summary>
        /// オブジェクトを手放す
        /// </summary>
        Task DisInteractObjectAsync(Guid objectId);
    }
}
