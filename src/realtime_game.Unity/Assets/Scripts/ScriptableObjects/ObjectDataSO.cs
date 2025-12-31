using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MyScripts/ObjectDataSO")]
public class ObjectDataSO : ScriptableObject {
    public List<GameObject> objectDataList = new List<GameObject>();
}
