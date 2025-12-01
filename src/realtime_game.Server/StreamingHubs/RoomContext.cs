using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Runtime.Multicast;
using realtime_game.Shared.Interfaces.StreamingHubs;

namespace realtime_game.Server.StreamingHubs {
    public class RoomContext : IDisposable {
        public Guid Id { get; } // ルームid
        public string Name { get; } // ルーム名
        public IMulticastSyncGroup<Guid, IRoomHubReceiver> Group { get; } // グループ
        public Dictionary<Guid, RoomUserData> RoomUserDataList { get; } =
            new Dictionary<Guid, RoomUserData>(); // ユーザーデータ一覧
        public Dictionary<Guid, RoomObjectData> RoomObjectDataList { get; } =
            new Dictionary<Guid, RoomObjectData>(); // オブジェクトデータ一覧

        // その他、ルームのデータとして保存したいものをフィールドに追加していく
        // コンストラクタ
        public RoomContext(IMulticastGroupProvider groupProvider, string roomName) {
            Id = Guid.NewGuid(); // ルーム毎のデータにIDを付けておく
            Name = roomName; // ルーム名をフィールドに保存
            Group = groupProvider.GetOrAddSynchronousGroup<Guid, IRoomHubReceiver>(roomName); // グループを作成
        }
        
        public void Dispose() {
            Group.Dispose();
        }
    }
}
