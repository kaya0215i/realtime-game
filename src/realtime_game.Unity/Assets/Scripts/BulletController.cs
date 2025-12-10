using System;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
using Unity.VisualScripting;
using UnityEngine;
using static PlayerManager;

public class BulletController : MonoBehaviour {
    private NetworkObject networkObject;
    private Rigidbody myRb;

    public float Life { private set; get; }
    public float AttackPower { private set; get; }
    public float ShotPower { private set; get; }

    // ShotGun用定数
    private const float MAX_RANDOM_VALUE = 2.5f;
    private const float MIN_RANDOM_VALUE = -2.5f;

    private void Awake() {

    }

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();

        PlayerManager playerManager = networkObject.GameManager.FindCharacterObject(networkObject.createrConnectionId).GetComponent<PlayerManager>();
        PLAYER_CHARACTER_TYPE createrCharacterType = playerManager.characterType;

        // キャラクターのタイプをもとに設定
        switch (createrCharacterType) {
            case PLAYER_CHARACTER_TYPE.AssaultRifle:
                Life = 3;
                AttackPower = 10;
                ShotPower = 30;
                break;

            case PLAYER_CHARACTER_TYPE.ShotGun:
                Life = 0.3f;
                AttackPower = 8;
                ShotPower = 50;
                break;

            case PLAYER_CHARACTER_TYPE.SniperRifle:
                Life = 7;
                AttackPower = 40;
                ShotPower = 50;
                break;
        }

        if (networkObject.isCreater) {
            if (createrCharacterType == PLAYER_CHARACTER_TYPE.ShotGun) {
                Vector3 rndSolt = new Vector3(UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE));

                myRb.AddForce(Camera.main.transform.forward * ShotPower + rndSolt, ForceMode.Impulse);
            }
            else {
                myRb.AddForce(Camera.main.transform.forward * ShotPower, ForceMode.Impulse);
            }
        }
        else {
            PlayerController playerController = playerManager.GetComponent<PlayerController>();

            if (createrCharacterType == PLAYER_CHARACTER_TYPE.ShotGun) {
                Vector3 rndSolt = new Vector3(UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE),
                    UnityEngine.Random.Range(MIN_RANDOM_VALUE, MAX_RANDOM_VALUE));

                myRb.AddForce(playerController._head.forward * ShotPower + rndSolt, ForceMode.Impulse);
            }
            else {
                myRb.AddForce(playerController._head.forward * ShotPower, ForceMode.Impulse);
            }
        }
    }

    private void Update() {
        // 作成者でなければreturn
        if (!networkObject.IsCreater()) {
            return;
        }

        Life -= Time.deltaTime;

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
            Destroy(this.gameObject);
        }
    }
}
