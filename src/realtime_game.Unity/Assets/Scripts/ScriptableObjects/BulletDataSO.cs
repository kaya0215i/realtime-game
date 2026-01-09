using System.Collections.Generic;
using UnityEngine;
using static CharacterSettings;

[CreateAssetMenu(menuName = "MyScripts/BulletDataSO")]
public class BulletDataSO : ScriptableObject {
    public List<BulletData> bulletDataList = new List<BulletData>();
}

[System.Serializable]
public class BulletData {
    public PLAYER_CHARACTER_TYPE CharacterType;
    public Sprite LoadoutSprite;
    public GameObject ObjectPrefab;
    public float Life;
    public float AttackPower;
    public float SmashPower;
    public float ShotPower;
}
