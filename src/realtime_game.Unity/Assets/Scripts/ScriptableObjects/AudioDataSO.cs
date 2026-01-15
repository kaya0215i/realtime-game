using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MyScripts/AudioDataSO")]
public class AudioDataSO : ScriptableObject {
    public List<AudioData> audioDatas = new List<AudioData>();
}

[System.Serializable]
public class AudioData {
    public string Name;
    public AudioClip Clip;
}