using System.Collections.Generic;
using UnityEngine;
using static CharacterSettings;

[CreateAssetMenu(menuName = "MyScripts/UltimateDataSO")]
public class UltimateDataSO : ScriptableObject {
    public List<UltimateData> ultimateDataList = new List<UltimateData>();
}

[System.Serializable]
public class UltimateData {
    public PLAYER_ULTIMATE Ultimate;
    public Sprite LoadoutSprite;
    public GameObject ObjectPrefab;
    public float Life;
    public float AttackPower;
    public float SmashPower;
    public float ShotPower;
    public float Radius;
    public float ChargeAmount;
}
