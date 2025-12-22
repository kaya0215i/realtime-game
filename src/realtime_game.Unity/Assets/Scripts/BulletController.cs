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
    public float SmashPower { private set; get; }
    public float ShotPower { private set; get; }

    // ShotGun用定数
    private const float MAX_RANDOM_VALUE = 3.5f;
    private const float MIN_RANDOM_VALUE = -3.5f;

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();

        // 撃ったプレイヤーのPlayerManagerを取得
        var playerManager = networkObject.GameManager.FindCharacterObject(networkObject.createrConnectionId).GetComponent<PlayerManager>();
        if (playerManager == null) {
            return;
        }

        PLAYER_CHARACTER_TYPE createrCharacterType = playerManager.characterType;

        // キャラクターのタイプをもとに設定
        switch (createrCharacterType) {
            case PLAYER_CHARACTER_TYPE.AssaultRifle:
                Life = 3f;
                AttackPower = 6.5f;
                SmashPower = 5f;
                ShotPower = 30f;
                break;

            case PLAYER_CHARACTER_TYPE.ShotGun:
                Life = 0.3f;
                AttackPower = 5f;
                SmashPower = 1.15f;
                ShotPower = 50f;
                break;

            case PLAYER_CHARACTER_TYPE.SniperRifle:
                Life = 7f;
                AttackPower = 35f;
                SmashPower = 8.5f;
                ShotPower = 50f;
                break;
        }

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

    /// <summary>
    /// このスクリプトがアタッチされているゲームオブジェクトを削除する
    /// </summary>
    public void DestroyThisObject() {
        Destroy(this.gameObject);
    }
}
