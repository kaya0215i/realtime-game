using realtime_game.Shared.Interfaces.StreamingHubs;
using UnityEngine;

namespace realtime_game.Server.StreamingHubs {
    public class RoomUserData {
        public Vector3 pos;
        public Quaternion rotate;

        public JoinedUser JoinedUser;
    }
}
