using System.Collections.Generic;
using UnityEngine;
using static CharacterSettings;

[CreateAssetMenu(menuName = "MyScripts/SubWeaponDataSO")]
public class SubWeaponDataSO : ScriptableObject {
    public List<SubWeaponData> subWeaponDataList = new List<SubWeaponData>();
}

[System.Serializable]
public class SubWeaponData {
    public PLAYER_SUB_WEAPON subWeapon;
}
