using realtime_game.Shared.Interfaces.StreamingHubs;
using UnityEngine;

namespace realtime_game.Server.StreamingHubs {
    public class RoomObjectData {
        public Vector3 pos = new Vector3();
        public Quaternion rotate = new Quaternion();

        public Guid InteractingUser = Guid.Empty;
        public bool IsInteracting = false;
    }
}
