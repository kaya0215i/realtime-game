using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using realtime_game.Shared.Models.Entities;
using UnityEngine;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// 接続済みユーザー
    /// </summary>
    [MessagePackObject]
    public class JoinedUser {
        [Key(0)]
        public Guid ConnectionId { get; set; } // 接続id
        [Key(1)]
        public User UserData { get; set; } //ユーザー情報
        [Key(2)]
        public int JoinOrder { get; set; } // 参加順番
    }
}
