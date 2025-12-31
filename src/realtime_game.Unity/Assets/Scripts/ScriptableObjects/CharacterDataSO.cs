using System.Collections.Generic;
using UnityEngine;
using static CharacterSettings;

[CreateAssetMenu(menuName = "MyScripts/CharacterDataSO")]
public class CharacterDataSO : ScriptableObject {
    public List<CharacterData> characterDataList = new List<CharacterData>();
}

[System.Serializable]
public class CharacterData {
    public PLAYER_CHARACTER_TYPE characterType;
    public int maxBulletAmount;
    public float shotCoolTime;
    public float reloadTime;
}
