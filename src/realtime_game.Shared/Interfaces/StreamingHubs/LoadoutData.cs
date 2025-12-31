using MessagePack;

namespace realtime_game.Shared.Interfaces.StreamingHubs {
    /// <summary>
    /// キャラクターのスキン装備
    /// </summary>
    [MessagePackObject]
    public class LoadoutData {
        [Key(0)]
        public int CharacterTypeNum { get; set; } = 0; // キャラクタータイプ
        [Key(1)]
        public int SubWeaponNum { get; set; } = 0; // サブ武器
        [Key(2)]
        public int UltimateNum { get; set; } = 0; // アルティメット
        [Key(3)]
        public string HatName { get; set; } = "Default"; // 帽子
        [Key(4)]
        public string AccessoriesName { get; set; } = "Default"; // アクセサリー
        [Key(5)]
        public string PantsName { get; set; } = "Default"; // パンツ
        [Key(6)]
        public string HairstyleName { get; set; } = "Default"; // 髪型
        [Key(7)]
        public string OuterwearName { get; set; } = "Default"; // アウター
        [Key(8)]
        public string ShoesName { get; set; } = "Default"; // 靴
    }
}
