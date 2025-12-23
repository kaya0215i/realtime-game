using UnityEngine;

public class CharacterData : MonoBehaviour {
    // シングルトンにする
    private static CharacterData instance;
    public static CharacterData Instance {
        get {
            if (instance == null) {
                GameObject obj = new GameObject("CharacterData");
                instance = obj.AddComponent<CharacterData>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    // キャラクタータイプの列挙型
    public enum PLAYER_CHARACTER_TYPE {
        None,
        AssaultRifle,
        ShotGun,
        SniperRifle,
    }

    // キャラクターのタイプ
    public PLAYER_CHARACTER_TYPE characterType = PLAYER_CHARACTER_TYPE.AssaultRifle;


}
