using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using static CharacterSettings;

public class BulletController : WeaponManager {
    // ShotGun用定数
    private const float MAX_RANDOM_VALUE = 3.5f;
    private const float MIN_RANDOM_VALUE = -3.5f;

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();
        myAudioSource = this.GetComponent<AudioSource>();

        // AudioSource設定
        AudioManager.Instance.SetAudioSouceVolume(myAudioSource);

        VFXParent = GameObject.Find("VFX").transform;

        // 撃ったプレイヤーのPlayerManagerを取得
        var playerManager = networkObject.GameManager.FindCharacterObject(networkObject.createrConnectionId).GetComponent<PlayerManager>();
        if (playerManager == null) {
            return;
        }

        PLAYER_CHARACTER_TYPE createrCharacterType = playerManager.characterType;

        // キャラクターのタイプをもとに設定
        BulletData bulletData = CharacterSettings.Instance.BDSO.bulletDataList.First(_=>_.CharacterType == createrCharacterType);
        MaxLife = bulletData.Life;
        Life = bulletData.Life;
        AttackPower = bulletData.AttackPower;
        SmashPower = bulletData.SmashPower;
        ShotPower = bulletData.ShotPower;

        // 弾をを飛ばす処理
        if (createrCharacterType == PLAYER_CHARACTER_TYPE.ShotGun) {
            Vector3 rndSolt = new Vector3(Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE));

            myRb.AddForce(this.transform.forward * ShotPower + rndSolt, ForceMode.Impulse);
        }
        else {
            myRb.AddForce(this.transform.forward * ShotPower, ForceMode.Impulse);
        }
    }

    private void Update() {
        Life -= Time.deltaTime;

        // 作成者でなければreturn
        if (!networkObject.IsCreater()) {
            return;
        }

        // lifeの時間経過したら削除
        if (Life <= 0) {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other) {
        // 作成者でなければreturn
        if (!networkObject.IsCreater()) {
            return;
        }

        // プレイヤー以外に当たったら
        if (!other.gameObject.CompareTag("Player")) {
            // VFX生成
            Instantiate(fieldHitVFX, this.transform.position, Quaternion.identity, VFXParent);

            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// このスクリプトがアタッチされているゲームオブジェクトを削除する
    /// </summary>
    public void DestroyThisObject() {
        Destroy(this.gameObject);
    }
}
