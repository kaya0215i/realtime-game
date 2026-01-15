using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class AudioManager : MonoBehaviour {
    // シングルトンにする
    private static AudioManager instance;

    private static bool isQuitting;
    private static bool isShuttingDown;

    public static AudioManager Instance {
        get {

            // アプリ終了/破棄中は新規生成しない
            if (isQuitting || isShuttingDown) {
                return null;
            }

            if (instance == null) {
                GameObject obj = new GameObject("AudioManager");
                instance = obj.AddComponent<AudioManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private void OnDestroy() {
        if (instance == this) {
            isShuttingDown = true;
            instance = null;
        }
    }

    private void OnApplicationQuit() {
        isQuitting = true;
    }

    // オーディオデータ
    private AudioDataSO ADSO;

    /// <summary>
    /// データ読み込み
    /// </summary>
    public async UniTask LoadData() {
        ADSO = (AudioDataSO)await Resources.LoadAsync<AudioDataSO>("AudioDataSO");
    }

    public float AudioVolume { get; private set; } = 1.0f;

    /// <summary>
    /// ボリューム変更
    /// </summary>
    public void ChangeAllAudioSourceVolume(float volume) {
        AudioVolume = volume;

        SetAllAudioSouceVolume();
    }

    /// <summary>
    /// 全てのAudioSouceのボリューム設定
    /// </summary>
    public void SetAllAudioSouceVolume() {
        var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        foreach (AudioSource audioSource in audioSources) {
            audioSource.volume = AudioVolume;
        }
    }

    /// <summary>
    /// 1つのAudioSouceのボリューム設定
    /// </summary>
    public void SetAudioSouceVolume(AudioSource audioSource) {
        audioSource.volume = AudioVolume;
    }


    /// <summary>
    /// オーディオクリップ再生
    /// </summary>
    public void PlayOneShotAudioClip(AudioSource audioSource, string audioName) {
        AudioClip clip = FindAudioClip(audioName);
        audioSource.PlayOneShot(clip);
    }


    /// <summary>
    /// オーディオクリップを名前で検索して返す
    /// </summary>
    private AudioClip FindAudioClip(string name) {
        return ADSO.audioDatas.FirstOrDefault(_ => _.Name == name).Clip;
    }
}
