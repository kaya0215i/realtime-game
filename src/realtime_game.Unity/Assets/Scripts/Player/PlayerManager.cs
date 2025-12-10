using DG.Tweening;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayerManager : MonoBehaviour {
    private GameManager gameManager;
    private PlayerController playerController;

    private Rigidbody myRb;

    [NonSerialized] public UIManager uiManager;

    // このキャラクターのコネクションID
    [NonSerialized] public Guid thisCharacterConnectionId;

    // Transform情報を送るタイマー
    private float updateTransformTime;
    // Transform情報を送る間隔
    private const float SEND_TRANSFORM_INTERVAL = 0.1f;

    // プレイヤー視点カメラのRotate
    [NonSerialized] public Quaternion cameraRotate;

    // キャラクタータイプの列挙型
    public enum PLAYER_CHARACTER_TYPE {
        None,
        AssaultRifle,
        ShotGun,
        SniperRifle,
    }

    // キャラクターのタイプ
    public PLAYER_CHARACTER_TYPE characterType = PLAYER_CHARACTER_TYPE.None;

    private void Awake() {
        updateTransformTime = 0;
    }

    private void Start() {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        playerController = this.GetComponent<PlayerController>();

        myRb = this.GetComponent <Rigidbody>();

        uiManager = GameObject.Find("UICanvas").GetComponent<UIManager>();
    }

    private async void Update() {
        if (!IsOwner()) {
            // カメラ情報を反映
            playerController._head.transform.DORotateQuaternion(cameraRotate, 0.1f).SetEase(Ease.InOutQuad);
            return;
        }

        cameraRotate = playerController._head.transform.rotation;

        // キャラクターのTransformをサーバーに送信
        updateTransformTime += Time.deltaTime;
        if (updateTransformTime >= SEND_TRANSFORM_INTERVAL) {
            updateTransformTime = 0;

            // サーバーにキャラクターのTransformを送信
            await RoomModel.Instance.UpdateUserTransformAsync(this.transform.position, this.transform.rotation, cameraRotate);
        }
    }

    /// <summary>
    /// 自分自身かどうか
    /// </summary>
    /// <returns></returns>
    public bool IsOwner() {
        if (!gameManager.isJoined ||
            gameManager.mySelf.ConnectionId != thisCharacterConnectionId) {
            return false;
        }

        return true;
    }

    private void OnCollisionEnter(Collision collision) {
        if (!IsOwner()) {
            return;
        }

        // 弾に当たったら
        if (collision.gameObject.CompareTag("Bullet")) {
            NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();
            // 自分の撃った弾だったら何もしない
            if (netObj.createrConnectionId == thisCharacterConnectionId) {
                return;
            }

            BulletController bulletController = collision.gameObject.GetComponent<BulletController>();
            float addPower = bulletController.Life / 10;
            if (addPower < 1) {
                addPower = 1;
            }

            myRb.linearVelocity = Vector3.zero;
            myRb.AddForce(-this.transform.forward * bulletController.AttackPower * addPower, ForceMode.Impulse);

            Destroy(collision.gameObject);
        }
    }
}
