using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MyScripts/DeathCauseSO")]
public class DeathCauseSO : ScriptableObject {
    public List<DeathCauseData> deathCauseDataList = new List<DeathCauseData>();
}

[System.Serializable]
public class DeathCauseData {
    public int id;
    public string name;
    public Sprite image;
}
