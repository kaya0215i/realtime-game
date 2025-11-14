using MessagePack;

namespace realtime_game.Shared.Interfaces.Services {
    [MessagePackObject]
    public class Number {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;
    }
}
