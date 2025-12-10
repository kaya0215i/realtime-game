using realtime_game.Shared.Interfaces.StreamingHubs;
using UnityEngine;

namespace realtime_game.Server.StreamingHubs {
    public class RoomObjectData {
        public Vector3 pos;
        public Quaternion rotate;

        public Guid InteractingUser;
        public bool IsInteracting;
    }
}
