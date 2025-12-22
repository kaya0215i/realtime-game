using DG.Tweening;
using System;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayerManager : MonoBehaviour {
    private GameManager gameManager;
    private PlayerController playerController;

    private Rigidbody myRb;

    [NonSerialized] private GameUIManager gameUIManager;

    // このキャラクターのコネクションID
    [NonSerialized] public Guid thisCharacterConnectionId;

    // Transform情報を送るタイマー
    private float updateTransformTime = 0;
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

    // キャラクタータイプ監視用
    private PLAYER_CHARACTER_TYPE characterTypeObserver = PLAYER_CHARACTER_TYPE.None;

    // 最後に自分に弾を当てたプレイヤーのコネクションID
    private Guid lastHitFromCreatedPlayerConnectionId = Guid.Empty;

    // 頭上ヒットパーセントテキスト
    private TextMeshProUGUI headUpHitPercentText;

    // ヒットパーセント
    [NonSerialized] public float HitPercent = 0f;

    private void Start() {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        playerController = this.GetComponent<PlayerController>();

        myRb = this.GetComponent <Rigidbody>();

        gameUIManager = GameObject.Find("UICanvas").GetComponent<GameUIManager>();

        headUpHitPercentText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private async void Update() {
        if (!gameManager.IsPlaying) {
            return;
        }

        if (!IsOwner()) {
            // カメラ情報を反映
            playerController._head.transform.DORotateQuaternion(cameraRotate, 0.1f).SetEase(Ease.InOutQuad);

            // ヒットパーセントをUIに反映
            headUpHitPercentText.text = Math.Round(HitPercent, 2, MidpointRounding.AwayFromZero).ToString() + "%";
            return;
        }
        // 自分の画面のヒットパーセントを反映
        gameUIManager.UpdateHitPercentText((float)Math.Round(HitPercent, 2, MidpointRounding.AwayFromZero));

        // キャラクターのTransformをサーバーに送信
        cameraRotate = playerController._head.transform.rotation;
        updateTransformTime += Time.deltaTime;
        if (updateTransformTime >= SEND_TRANSFORM_INTERVAL) {
            updateTransformTime = 0;

            // サーバーにキャラクターのTransformを送信
            await RoomModel.Instance.UpdateUserTransformAsync(this.transform.position, this.transform.rotation, cameraRotate);
        }

        // プレイヤーのキャラクタータイプが変わったら
        if (characterTypeObserver != characterType) {
            characterTypeObserver = characterType;
            Debug.Log("キャラクタータイプ変更");

            playerController.SetPlayerCharacterType();

            await RoomModel.Instance.ChangeCharacterTypeAsync(characterType);
        }
    }

    /// <summary>
    /// 自分自身かどうか
    /// </summary>
    public bool IsOwner() {
        if (thisCharacterConnectionId == Guid.Empty ||
            !gameManager.isJoined ||
            gameManager.mySelf.ConnectionId != thisCharacterConnectionId) {
            return false;
        }

        return true;
    }

    private async void OnCollisionEnter(Collision collision) {
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

            // 追加の吹っ飛び
            BulletController bulletController = collision.gameObject.GetComponent<BulletController>();
            // 弾のライフが多いほど吹っ飛ぶように
            float addPowerFromLife = 1 + (bulletController.Life / 100);

            // ヒットパーセントが高いほど吹っ飛ぶように
            float addPowerFromHitPercent = 1 + (HitPercent / 100);

            // 吹っ飛ばし
            myRb.linearVelocity = Vector3.zero;
            myRb.AddForce((-this.transform.forward + this.transform.up) * bulletController.SmashPower * addPowerFromLife * addPowerFromHitPercent, ForceMode.Impulse);

            // ゲームスタートしてたら
            if (gameManager.IsGameStartShared) {
                // 自分に当てたプレイヤーのコネクションIDを保持
                lastHitFromCreatedPlayerConnectionId = netObj.createrConnectionId;

                // ヒットパーセントを増やす
                HitPercent += bulletController.AttackPower;

                // サーバーに送信
                await RoomModel.Instance.HitPercentAsync(HitPercent);
            }

            // 弾を削除
            bulletController.DestroyThisObject();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsOwner()) {
            return;
        }

        // 死亡エリアに入ったら
        if (other.gameObject.CompareTag("DeathArea")) {
            // ゲームスタートしてたら
            if (gameManager.IsGameStartShared) {
                gameManager.Dead(lastHitFromCreatedPlayerConnectionId);
            }
            else {
                this.transform.position = Vector3.zero;
            }
        }
    }
}
