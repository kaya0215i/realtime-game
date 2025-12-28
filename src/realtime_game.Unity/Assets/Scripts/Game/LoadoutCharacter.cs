using UnityEngine;

public class LoadoutCharacter : MonoBehaviour {
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
    }
}
