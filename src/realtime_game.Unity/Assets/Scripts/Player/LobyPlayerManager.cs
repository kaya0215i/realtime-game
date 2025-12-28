using DG.Tweening.Core.Easing;
using System;
using UnityEngine;

public class LobyPlayerManager : MonoBehaviour {
    private LobyManager lobyManager;

    // このキャラクターのコネクションID
    [NonSerialized] public Guid thisCharacterConnectionId;

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

    private void Start() {
        lobyManager = GameObject.Find("LobyManager").GetComponent<LobyManager>();
    }

    private void Update() {
        if (!IsOwner()) {
            return;
        }

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
}
