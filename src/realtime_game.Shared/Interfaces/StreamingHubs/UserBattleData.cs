using MessagePack;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// バトル中のユーザーデータ
    /// </summary>
    [MessagePackObject]
    public class UserBattleData {
        [Key(0)]
        public float HitPercent { get; set; } = 0;
        [Key(1)]
        public int Score { get; set; } = 0;
    }
}
