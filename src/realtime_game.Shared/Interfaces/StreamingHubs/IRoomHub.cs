using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MagicOnion;

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
    }
}
