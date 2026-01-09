using System.Collections.Generic;
using UnityEngine;
using static CharacterSettings;

[CreateAssetMenu(menuName = "MyScripts/SubWeaponDataSO")]
public class SubWeaponDataSO : ScriptableObject {
    public List<SubWeaponData> subWeaponDataList = new List<SubWeaponData>();
}

[System.Serializable]
public class SubWeaponData {
    public PLAYER_SUB_WEAPON SubWeapon;
    public Sprite LoadoutSprite;
    public GameObject ObjectPrefab;
    public float Life;
    public float AttackPower;
    public float SmashPower;
    public float ShotPower;
    public float Radius;
    public float CoolTime;
}
