using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MyScripts/ObjectDataSO")]
public class ObjectDataSO : ScriptableObject {
    public List<ObjectData> objectDataList = new List<ObjectData>();
}

[System.Serializable]
public class ObjectData {
    public int id;
    public string name;
    public GameObject objectData;
}
