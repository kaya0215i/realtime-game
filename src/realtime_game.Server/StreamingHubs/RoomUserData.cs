using realtime_game.Shared.Interfaces.StreamingHubs;
using UnityEngine;

namespace realtime_game.Server.StreamingHubs {
    public class RoomUserData {
        public Vector3 pos = new Vector3();
        public Quaternion rotate = new Quaternion();

        public UserBattleData UserBattleData = new UserBattleData();

        public JoinedUser JoinedUser = new JoinedUser();
    }
}
