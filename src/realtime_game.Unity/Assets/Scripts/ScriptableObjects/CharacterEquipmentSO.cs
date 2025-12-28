using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MyScripts/CharacterEquipmentSO")]
public class CharacterEquipmentSO : ScriptableObject {
    public List<CharacterEquipment> characterEquipment = new List<CharacterEquipment>();
}

[System.Serializable]
public class CharacterEquipment {
    public string name;
    public List<Equipment> equipment = new List<Equipment>();
}

[System.Serializable]
public class Equipment {
    public int id;
    public string name;
    public Mesh mesh;
}