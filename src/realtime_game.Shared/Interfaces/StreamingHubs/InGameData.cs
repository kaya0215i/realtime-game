using MessagePack;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// インゲームの情報
    /// </summary>
    [MessagePackObject]
    public class InGameData {
        [Key(0)]
        public bool IsGameStart { get; set; } = false;

        [Key(1)]
        public bool IsGameSet { get; set; } = false;

        [Key(2)]
        public float GameTime { get; set; } = 0;
        [Key(3)]
        public float GameTimer { get; set; } = 0;
    }
}
