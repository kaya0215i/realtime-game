using System;
using UnityEngine;
using static CharacterSettings;

public class LobyPlayerManager : MonoBehaviour {
    private LobyManager lobyManager;

    // このキャラクターのコネクションID
    [NonSerialized] public Guid thisCharacterConnectionId;

    // 右肩
    [SerializeField] private Transform rightShoulder;
    // 右手
    [SerializeField] private Transform rightHand;

    private Vector3 GodShoulder { get; } = new Vector3(3, 2, 2);
    private Vector3 GodHand { get; } = new Vector3(5, 5, 5);

    // 帽子
    [SerializeField] public SkinnedMeshRenderer Hat;
    // アクセサリー
    [SerializeField] public SkinnedMeshRenderer Accessories;
    // パンツ
    [SerializeField] public SkinnedMeshRenderer Pants;
    // 髪型
    [SerializeField] public SkinnedMeshRenderer Hairstyle;
    // アウター
    [SerializeField] public SkinnedMeshRenderer Outerwear;
    // 靴
    [SerializeField] public SkinnedMeshRenderer Shoes;

    // キャラクターのタイプ
    public PLAYER_CHARACTER_TYPE characterType = PLAYER_CHARACTER_TYPE.AssaultRifle;
    // キャラクターのサブ武器
    public PLAYER_SUB_WEAPON subWeapon = PLAYER_SUB_WEAPON.Bomb;
    // キャラクターのアルティメット
    public PLAYER_ULTIMATE ultimate = PLAYER_ULTIMATE.Meteor;

    private void Start() {
        lobyManager = GameObject.Find("LobyManager").GetComponent<LobyManager>();

        ChangeCharacterModel();
    }

    private async void Update() {
        if (!IsOwner()) {
            return;
        }

        bool isChanged = false;

        // スキンが変わったら
        if (Hat.sharedMesh != CharacterSettings.Instance.Hat) {
            isChanged = true;
            Hat.sharedMesh = CharacterSettings.Instance.Hat;
        }
        if (Accessories.sharedMesh != CharacterSettings.Instance.Accessories) {
            isChanged = true;
            Accessories.sharedMesh = CharacterSettings.Instance.Accessories;
        }
        if (Pants.sharedMesh != CharacterSettings.Instance.Pants) {
            isChanged = true;
            Pants.sharedMesh = CharacterSettings.Instance.Pants;
        }
        if (Hairstyle.sharedMesh != CharacterSettings.Instance.Hairstyle) {
            isChanged = true;
            Hairstyle.sharedMesh = CharacterSettings.Instance.Hairstyle;
        }
        if (Outerwear.sharedMesh != CharacterSettings.Instance.Outerwear) {
            isChanged = true;
            Outerwear.sharedMesh = CharacterSettings.Instance.Outerwear;
        }
        if (Shoes.sharedMesh != CharacterSettings.Instance.Shoes) {
            isChanged = true;
            Shoes.sharedMesh = CharacterSettings.Instance.Shoes;
        }

        if (isChanged) {
            // 他のプレイヤーに通知
            await RoomModel.Instance.ChangeLoadoutLobyAsync(CharacterSettings.Instance.GetLoadoutData());
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

            // 他のプレイヤーに通知
            await RoomModel.Instance.ChangeLoadoutLobyAsync(CharacterSettings.Instance.GetLoadoutData());
        }
    }

    /// <summary>
    /// 自分自身かどうか
    /// </summary>
    public bool IsOwner() {
        if (!lobyManager.InTeam ||
            thisCharacterConnectionId == Guid.Empty ||
            thisCharacterConnectionId != lobyManager.mySelf.ConnectionId) {
            return false;
        }

        return true;
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
