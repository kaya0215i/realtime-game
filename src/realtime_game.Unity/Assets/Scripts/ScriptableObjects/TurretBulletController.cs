using System;
using System.Linq;
using UnityEngine;
using static CharacterSettings;

public class TurretBulletController : MonoBehaviour {
    [NonSerialized] public TurretController turretController;
    private NetworkObject networkObject;
    private Rigidbody myRb;

    // VFX
    [SerializeField] private GameObject fieldHitVFX;
    // VFXの親Transform
    private Transform VFXParent;

    public float MaxLife { private set; get; }
    public float Life { private set; get; }
    public float AttackPower { private set; get; }
    public float SmashPower { private set; get; }
    public float ShotPower { private set; get; }

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();

        VFXParent = GameObject.Find("VFX").transform;

        // アサルトライフルをもとに設定
        BulletData bulletData = CharacterSettings.Instance.BDSO.bulletDataList.First(_ => _.CharacterType == PLAYER_CHARACTER_TYPE.AssaultRifle);
        MaxLife = bulletData.Life;
        Life = bulletData.Life;
        AttackPower = bulletData.AttackPower;
        SmashPower = bulletData.SmashPower;
        ShotPower = bulletData.ShotPower;

        // 弾をを飛ばす処理
        myRb.AddForce(this.transform.forward * ShotPower, ForceMode.Impulse);
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
