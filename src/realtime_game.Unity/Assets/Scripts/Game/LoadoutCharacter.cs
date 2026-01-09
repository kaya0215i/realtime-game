using UnityEngine;
using static CharacterSettings;

public class LoadoutCharacter : MonoBehaviour {
    // 右肩
    [SerializeField] private Transform rightShoulder;
    // 右手
    [SerializeField] private Transform rightHand;

    private Vector3 GodShoulder { get; } = new Vector3(3, 2, 2);
    private Vector3 GodHand { get; } = new Vector3(5, 5, 5);

    // 帽子
    [SerializeField] private SkinnedMeshRenderer Hat;
    // アクセサリー
    [SerializeField] private SkinnedMeshRenderer Accessories;
    // パンツ
    [SerializeField] private SkinnedMeshRenderer Pants;
    // 髪型
    [SerializeField] private SkinnedMeshRenderer Hairstyle;
    // アウター
    [SerializeField] private SkinnedMeshRenderer Outerwear;
    // 靴
    [SerializeField] private SkinnedMeshRenderer Shoes;

    // キャラクターのタイプ
    public PLAYER_CHARACTER_TYPE characterType = PLAYER_CHARACTER_TYPE.AssaultRifle;
    // キャラクターのサブ武器
    public PLAYER_SUB_WEAPON subWeapon = PLAYER_SUB_WEAPON.Bomb;
    // キャラクターのアルティメット
    public PLAYER_ULTIMATE ultimate = PLAYER_ULTIMATE.Meteor;

    private void Update() {
        // スキンが変わったら
        if (Hat.sharedMesh != CharacterSettings.Instance.Hat) {
            Hat.sharedMesh = CharacterSettings.Instance.Hat;
        }
        if (Accessories.sharedMesh != CharacterSettings.Instance.Accessories) {
            Accessories.sharedMesh = CharacterSettings.Instance.Accessories;
        }
        if (Pants.sharedMesh != CharacterSettings.Instance.Pants) {
            Pants.sharedMesh = CharacterSettings.Instance.Pants;
        }
        if (Hairstyle.sharedMesh != CharacterSettings.Instance.Hairstyle) {
            Hairstyle.sharedMesh = CharacterSettings.Instance.Hairstyle;
        }
        if (Outerwear.sharedMesh != CharacterSettings.Instance.Outerwear) {
            Outerwear.sharedMesh = CharacterSettings.Instance.Outerwear;
        }
        if (Shoes.sharedMesh != CharacterSettings.Instance.Shoes) {
            Shoes.sharedMesh = CharacterSettings.Instance.Shoes;
        }

        // プレイヤーのキャラクタータイプか武器が変わったら
        if (characterType != CharacterSettings.Instance.CharacterType ||
            subWeapon != CharacterSettings.Instance.SubWeapon ||
            ultimate != CharacterSettings.Instance.Ultimate) {
            // 変更を適応
            characterType = CharacterSettings.Instance.CharacterType;
            subWeapon = CharacterSettings.Instance.SubWeapon;
            ultimate = CharacterSettings.Instance.Ultimate;

            ChangeCharacterModel();
        }
    }

    /// <summary>
    /// キャラクタータイプに合わせてモデルのTransform変更
    /// </summary>
    public void ChangeCharacterModel() {
        switch (characterType) {
            case PLAYER_CHARACTER_TYPE.AssaultRifle:
                rightShoulder.localScale = Vector3.one;
                rightHand.localScale = Vector3.one;
                break;

            case PLAYER_CHARACTER_TYPE.ShotGun:
                rightShoulder.localScale = Vector3.one;
                rightHand.localScale = GodHand;
                break;

            case PLAYER_CHARACTER_TYPE.SniperRifle:
                rightShoulder.localScale = GodShoulder;
                rightHand.localScale = Vector3.one;
                break;
        }
    }
}
