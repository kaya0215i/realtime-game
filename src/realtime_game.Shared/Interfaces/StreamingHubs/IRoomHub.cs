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

        // ユーザー入室
        Task<JoinedUser[]> JoinAsync(string roomName, int userId);

        // ユーザー退室
        Task LeaveAsync();

        // ロビールームの入室
        Task<JoinedUser[]> JoinLobyAsync(int userId);

        // ロビールームの退室
        Task LeaveLobyAsync();

        // チームを作成
        Task<Guid> CreateTeamAndJoinAsync();

        // チームに参加
        Task<JoinedUser[]> JoinTeamAsync(Guid targetTeamId);

        // チームを抜ける
        Task LeaveTeamAsync();

        // 接続ID取得
        Task<Guid> GetConnectionId();

        // ユーザーTransform更新
        Task UpdateUserTransformAsync(Vector3 pos, Quaternion rotate, Quaternion cameraRotate);

        // オブジェクトの作成
        Task<Guid> CreateObjectAsync(int objectDataId, Vector3 pos, Quaternion rotate, int updateTypeNum);

        // オブジェクトの破棄
        Task DestroyObjectAsync(Guid objectId);

        // オブジェクトのTransform更新
        Task UpdateObjectTransformAsync(Guid objectId, Vector3 pos, Quaternion rotate);

        // オブジェクトがインタラクト可能であればする
        Task<bool> InteractObjectAsync(Guid objectId);

        // オブジェクトを手放す
        Task DisInteractObjectAsync(Guid objectId);
    }
}
