using MessagePack;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// インゲームの情報
    /// </summary>
    [MessagePackObject]
    public class InGameData {
        [Key(0)]
        public bool isGameStart { get; set; } = false;

        [Key(1)]
        public float gameTime { get; set; } = 0;
        [Key(2)]
        public float gameTimer { get; set; } = 0;
    }
}
