using Cysharp.Threading.Tasks;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using System.Linq;
using UnityEngine;

public class CharacterSettings : MonoBehaviour {
    // シングルトンにする
    private static CharacterSettings instance;

    private static bool isQuitting;
    private static bool isShuttingDown;

    public static CharacterSettings Instance {
        get {

            // アプリ終了/破棄中は新規生成しない
            if (isQuitting || isShuttingDown) {
                return null;
            }

            if (instance == null) {
                GameObject obj = new GameObject("CharacterData");
                instance = obj.AddComponent<CharacterSettings>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private void OnDestroy() {
        if (instance == this) {
            isShuttingDown = true;
            instance = null;
        }
    }

    private void OnApplicationQuit() {
        isQuitting = true;
    }


    // カメラ感度
    public float SensX = 30f; 
    public float SensY = 30f;

    /// <summary>
    /// データ読み込み
    /// </summary>
    public async void LoadData() {
        CDSO = (CharacterDataSO)await Resources.LoadAsync<CharacterDataSO>("CharacterDataSO");
        BDSO = (BulletDataSO)await Resources.LoadAsync<BulletDataSO>("BulletDataSO");
        SWDSO = (SubWeaponDataSO)await Resources.LoadAsync<SubWeaponDataSO>("SubWeaponDataSO");
        UDSO = (UltimateDataSO)await Resources.LoadAsync<UltimateDataSO>("UltimateDataSO");
        await MeshLoadDataAsync();
    }

    /*
     * 
     * キャラクターのタイプ
     * 
     */

    // キャラクタータイプの列挙型
    public enum PLAYER_CHARACTER_TYPE {
        AssaultRifle,
        ShotGun,
        SniperRifle,
    }

    // キャラクターのデータ
    public CharacterDataSO CDSO;
    // 弾のデータ
    public BulletDataSO BDSO;

    // キャラクターのタイプ
    public PLAYER_CHARACTER_TYPE CharacterType = PLAYER_CHARACTER_TYPE.AssaultRifle;

    /// <summary>
    /// キャラクタータイプのスプライトを返す
    /// </summary>
    public Sprite GetCharacterCharacterTypeSprite() {
        return BDSO.bulletDataList.First(_=>_.CharacterType == CharacterType).LoadoutSprite;
    }

    /*
     * 
     * キャラクターのサブ武器
     * 
     */

    // サブ武器の列挙型
    public enum PLAYER_SUB_WEAPON {
        Bomb,
        ImpactBomb,
        Turret,
    }

    // サブ武器のデータ
    public SubWeaponDataSO SWDSO;

    // サブ武器
    public PLAYER_SUB_WEAPON SubWeapon = PLAYER_SUB_WEAPON.Bomb;

    /// <summary>
    /// サブ武器のスプライトを返す
    /// </summary>
    public Sprite GetCharacterSubWeaponSprite() {
        return SWDSO.subWeaponDataList.First(_ => _.SubWeapon == SubWeapon).LoadoutSprite;
    }

    /*
     * 
     * キャラクターのアルティメット
     * 
     */

    // アルティメットの列挙型
    public enum PLAYER_ULTIMATE {
        Meteor,
        HomingMissile,
    }

    // アルティメットのデータ
    public UltimateDataSO UDSO;

    // アルティメット
    public PLAYER_ULTIMATE Ultimate = PLAYER_ULTIMATE.Meteor;

    /// <summary>
    /// アルティメットのスプライトを返す
    /// </summary>
    public Sprite GetCharacterUltimateSprite() {
        return UDSO.ultimateDataList.First(_ => _.Ultimate == Ultimate).LoadoutSprite;
    }

    /*
     * 
     * キャラクターカスタマイズ用
     * 
     */

    // キャラクタースキン部位
    public enum Character_Skin_Part {
        Hat,
        Accessories,
        Pants,
        Hairstyle,
        Outerwear,
        Shoes,
    }

    // 帽子
    public Mesh Hat;
    // アクセサリー
    public Mesh Accessories;
    // パンツ
    public Mesh Pants;
    // 髪型
    public Mesh Hairstyle;
    // アウター
    public Mesh Outerwear;
    // 靴
    public Mesh Shoes;

    // 装備メッシュSO
    public CharacterEquipmentSO CESO;

    /// <summary>
    /// キャラクタースキン用のメッシュデータを非同期で読み込む
    /// </summary>
    public UniTask MeshLoadDataAsync() {
        CESO = Resources.Load<CharacterEquipmentSO>("CharacterEquipmentSO");

        Hat = CESO.characterEquipment.FirstOrDefault(_=>_.name == "Hat").equipment.FirstOrDefault(_=> _.name == "Default").mesh;
        Accessories = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Accessories").equipment.FirstOrDefault(_ => _.name == "Default").mesh;
        Pants = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Pants").equipment.FirstOrDefault(_ => _.name == "Default").mesh;
        Hairstyle = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Hairstyle").equipment.FirstOrDefault(_ => _.name == "Default").mesh;
        Outerwear = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Outerwear").equipment.FirstOrDefault(_ => _.name == "Default").mesh;
        Shoes = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Shoes").equipment.FirstOrDefault(_ => _.name == "Default").mesh;

        return UniTask.CompletedTask;
    }

    /// <summary>
    /// ロードアウトデータを取得
    /// </summary>
    public LoadoutData GetLoadoutData() {
        LoadoutData loadoutData = new LoadoutData() {
            CharacterTypeNum = (int)CharacterType,
            SubWeaponNum = (int)SubWeapon,
            UltimateNum = (int)Ultimate,
            HatName = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Hat").equipment.FirstOrDefault(_ => _.mesh == Hat).name,
            AccessoriesName = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Accessories").equipment.FirstOrDefault(_ => _.mesh == Accessories).name,
            PantsName = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Pants").equipment.FirstOrDefault(_ => _.mesh == Pants).name,
            HairstyleName = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Hairstyle").equipment.FirstOrDefault(_ => _.mesh == Hairstyle).name,
            OuterwearName = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Outerwear").equipment.FirstOrDefault(_ => _.mesh == Outerwear).name,
            ShoesName = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Shoes").equipment.FirstOrDefault(_ => _.mesh == Shoes).name,
        };

        return loadoutData;
    }

    /// <summary>
    /// キャラクターのスキン装備を変更
    /// </summary>
    public void ChangeCharacterEquipment(Character_Skin_Part part, string name) {
        switch (part) {
            case Character_Skin_Part.Hat:
                Hat = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Hat").equipment.FirstOrDefault(_ => _.name == name).mesh;
                break;
            case Character_Skin_Part.Accessories:
                Accessories = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Accessories").equipment.FirstOrDefault(_ => _.name == name).mesh;
                break;
            case Character_Skin_Part.Pants:
                Pants = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Pants").equipment.FirstOrDefault(_ => _.name == name).mesh;
                break;
            case Character_Skin_Part.Hairstyle:
                Hairstyle = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Hairstyle").equipment.FirstOrDefault(_ => _.name == name).mesh;
                break;
            case Character_Skin_Part.Outerwear:
                Outerwear = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Outerwear").equipment.FirstOrDefault(_ => _.name == name).mesh;
                break;
            case Character_Skin_Part.Shoes:
                Shoes = CESO.characterEquipment.FirstOrDefault(_ => _.name == "Shoes").equipment.FirstOrDefault(_ => _.name == name).mesh;
                break;
        }
    }

    /// <summary>
    /// 装備しているスキンのスプライトを返す
    /// </summary>
    public Sprite GetCharacterEquipmentSprite(string part) {
        Sprite sprite = null;

        switch (part) {
            case "Hat":
                sprite = CESO.characterEquipment.FirstOrDefault(_ => _.name == part).equipment.FirstOrDefault(_ => _.mesh == Hat).sprite;
                break;

            case "Accessories":
                sprite = CESO.characterEquipment.FirstOrDefault(_ => _.name == part).equipment.FirstOrDefault(_ => _.mesh == Accessories).sprite;
                break;

            case "Pants":
                sprite = CESO.characterEquipment.FirstOrDefault(_ => _.name == part).equipment.FirstOrDefault(_ => _.mesh == Pants).sprite;
                break;

            case "Hairstyle":
                sprite = CESO.characterEquipment.FirstOrDefault(_ => _.name == part).equipment.FirstOrDefault(_ => _.mesh == Hairstyle).sprite;
                break;

            case "Outerwear":
                sprite = CESO.characterEquipment.FirstOrDefault(_ => _.name == part).equipment.FirstOrDefault(_ => _.mesh == Outerwear).sprite;
                break;

            case "Shoes":
                sprite = CESO.characterEquipment.FirstOrDefault(_ => _.name == part).equipment.FirstOrDefault(_ => _.mesh == Shoes).sprite;
                break;
        }

        return sprite;
    }
}
