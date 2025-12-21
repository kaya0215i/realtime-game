using System;
using MessagePack;
using realtime_game.Shared.Models.Entities;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// チーム内のユーザー情報
    /// </summary>
    [MessagePackObject]
    public class TeamUser {
        [Key(0)]
        public bool IsLeader { get; set; } // リーダーかどうか
        [Key(1)]
        public bool IsReady { get; set; } // 準備状態
        [Key(3)]
        public bool IsPlaying { get; set; } // 遊んでいるか
    }
}
