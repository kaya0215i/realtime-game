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
    public interface IRoomHub :IStreamingHub<IRoomHub, IRoomHubReceiver> {
        // [サーバーに実装]
        // [クライアントから呼び出す]

        // ユーザー入室
        Task<JoinedUser[]> JoinAsync(string roomName, int userId);

        // ユーザー退室
        Task LeaveAsync();

        // 接続ID取得
        Task<Guid> GetConnectionId();

        // Transform更新
        Task UpdateTransformAsync(Vector3 pos, Quaternion rotate);

        // オブジェクトの作成
        Task<Guid> CreateObjectAsync(Vector3 pos, Quaternion rotate);

        // オブジェクトの破棄
        Task DestroyObjectAsync(Guid objectId);

        // オブジェクトのTransform更新
        Task UpdateObjectTransformAsync(Guid objectId, Vector3 pos, Quaternion rotate);
    }
}
