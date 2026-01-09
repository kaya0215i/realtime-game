using System.Collections.Generic;
using UnityEngine;
using static GameManager;

[CreateAssetMenu(menuName = "MyScripts/DeathCauseSO")]
public class DeathCauseSO : ScriptableObject {
    public List<DeathCauseData> deathCauseDataList = new List<DeathCauseData>();
}

[System.Serializable]
public class DeathCauseData {
    public Death_Cause death_Cause;
    public Sprite image;
}
