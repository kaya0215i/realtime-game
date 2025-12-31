using System.Linq;
using UnityEngine;
using static CharacterSettings;

public class BulletController : MonoBehaviour {
    private NetworkObject networkObject;
    private Rigidbody myRb;

    // VFX
    [SerializeField] private GameObject fieldHitVFX;
    // VFXの親Transform
    private Transform VFXParent;

    // 弾のデータ
    [SerializeField] private BulletDataSO bulletDataSO;

    public float MaxLife { private set; get; }
    public float Life { private set; get; }
    public float AttackPower { private set; get; }
    public float SmashPower { private set; get; }
    public float ShotPower { private set; get; }

    // ShotGun用定数
    private const float MAX_RANDOM_VALUE = 3.5f;
    private const float MIN_RANDOM_VALUE = -3.5f;

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();

        VFXParent = GameObject.Find("VFX").transform;

        // 撃ったプレイヤーのPlayerManagerを取得
        var playerManager = networkObject.GameManager.FindCharacterObject(networkObject.createrConnectionId).GetComponent<PlayerManager>();
        if (playerManager == null) {
            return;
        }

        PLAYER_CHARACTER_TYPE createrCharacterType = playerManager.characterType;

        // キャラクターのタイプをもとに設定
        BulletData bulletData = bulletDataSO.bulletDataList.First(_=>_.characterType == createrCharacterType);
        MaxLife = bulletData.Life;
        Life = bulletData.Life;
        AttackPower = bulletData.AttackPower;
        SmashPower = bulletData.SmashPower;
        ShotPower = bulletData.ShotPower;

        // 自分の弾だったら
        if (networkObject.isCreater) {
            if (createrCharacterType == PLAYER_CHARACTER_TYPE.ShotGun) {
                Vector3 rndSolt = new Vector3(UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE));

                myRb.AddForce(this.transform.forward * ShotPower + rndSolt, ForceMode.Impulse);
            }
            else {
                myRb.AddForce(this.transform.forward * ShotPower, ForceMode.Impulse);
            }
        }
        // 他のプレイヤーの撃った弾だったら
        else {
            PlayerController playerController = playerManager.GetComponent<PlayerController>();

            if (createrCharacterType == PLAYER_CHARACTER_TYPE.ShotGun) {
                Vector3 rndSolt = new Vector3(UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE));

                myRb.AddForce(this.transform.forward * ShotPower + rndSolt, ForceMode.Impulse);
            }
            else {
                myRb.AddForce(this.transform.forward * ShotPower, ForceMode.Impulse);
            }
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

    private void OnCollisionEnter(Collision collision) {
        // 作成者でなければreturn
        if (!networkObject.IsCreater()) {
            return;
        }

        // プレイヤー以外に当たったら
        if (!collision.gameObject.CompareTag("Player")) {
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
