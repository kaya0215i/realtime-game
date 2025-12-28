using System;
using MessagePack;
using realtime_game.Shared.Models.Entities;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// 接続済みユーザー
    /// </summary>
    [MessagePackObject]
    public class JoinedUser {
        [Key(0)]
        public Guid ConnectionId { get; set; } // 接続id
        [Key(1)]
        public User UserData { get; set; } = new User(); //ユーザー情報
        [Key(2)]
        public int JoinOrder { get; set; } // 参加順番
        [Key(3)]
        public TeamUser TeamUser { get; set; } = new TeamUser();// チーム情報
        [Key(4)]
        public LoadoutData LoadoutData { get; set; } = new LoadoutData(); // ロードアウトデータ
    }
}
