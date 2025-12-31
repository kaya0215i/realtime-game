using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using static CharacterSettings;
using static GameManager;

public class PlayerManager : MonoBehaviour {
    private GameManager gameManager;
    private PlayerController playerController;

    private Rigidbody myRb;

    [NonSerialized] private GameUIManager gameUIManager;

    // VFXのリスト
    [SerializeField] private List<GameObject> VFXList;
    // VFXの親Transform
    private Transform VFXParent;

    // このキャラクターのコネクションID
    [NonSerialized] public Guid thisCharacterConnectionId;

    // 帽子
    [SerializeField] public SkinnedMeshRenderer Hat;
    // アクセサリー
    [SerializeField] public SkinnedMeshRenderer Accessories;
    // パンツ
    [SerializeField] public SkinnedMeshRenderer Pants;
    // 髪型
    [SerializeField] public SkinnedMeshRenderer Hairstyle;
    // アウター
    [SerializeField] public SkinnedMeshRenderer Outerwear;
    // 靴
    [SerializeField] public SkinnedMeshRenderer Shoes;

    // Transform情報を送るタイマー
    private float updateTransformTime = 0;
    // Transform情報を送る間隔
    private const float SEND_TRANSFORM_INTERVAL = 0.1f;

    // プレイヤー視点カメラのRotate
    [NonSerialized] public Quaternion cameraRotate;

    // キャラクターのタイプ
    public PLAYER_CHARACTER_TYPE characterType = PLAYER_CHARACTER_TYPE.AssaultRifle;
    // キャラクターのサブ武器
    public PLAYER_SUB_WEAPON subWeapon = PLAYER_SUB_WEAPON.Grenade;
    // キャラクターのアルティメット
    public PLAYER_ULTIMATE ultimate = PLAYER_ULTIMATE.Meteor;

    // 最後に自分に弾を当てたプレイヤーのコネクションID
    private Guid lastHitPlayerConnectionId = Guid.Empty;

    // 死因
    private Death_Cause deathCause = Death_Cause.None;

    private bool isStartReleseTimer = false;
    // 解除処理用CancellationTokenSource
    private CancellationTokenSource releseCTS = new CancellationTokenSource();

    // 頭上ヒットパーセントテキスト
    private TextMeshProUGUI headUpHitPercentText;

    // ヒットパーセント
    [NonSerialized] public float HitPercent = 0f;



    private void Start() {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        playerController = this.GetComponent<PlayerController>();

        myRb = this.GetComponent <Rigidbody>();

        gameUIManager = GameObject.Find("UICanvas").GetComponent<GameUIManager>();

        VFXParent = GameObject.Find("VFX").transform;

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

        // プレイヤーのキャラクタータイプか武器が変わったら
        if (characterType != CharacterSettings.Instance.CharacterType ||
            subWeapon != CharacterSettings.Instance.SubWeapon || 
            ultimate != CharacterSettings.Instance.Ultimate) {
            // 変更を適応
            characterType = CharacterSettings.Instance.CharacterType;
            subWeapon = CharacterSettings.Instance.SubWeapon;
            ultimate = CharacterSettings.Instance.Ultimate;

            playerController.SetPlayerCharacterType();

            await RoomModel.Instance.ChangeLoadoutGameAsync(CharacterSettings.Instance.GetLoadoutData());
        }

        if (lastHitPlayerConnectionId != Guid.Empty &&
            !isStartReleseTimer) {
            try {
                await ReleseLastHitConnectionId();
            }
            // キャンセルされたら
            catch {
                releseCTS = new CancellationTokenSource();
            }
        }
    }

    /// <summary>
    /// 自分に当てたプレイヤーのコネクションIDを解除
    /// </summary>
    private async UniTask ReleseLastHitConnectionId() {
        isStartReleseTimer = true;

        // 10秒待つ
        await UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: releseCTS.Token);

        lastHitPlayerConnectionId = Guid.Empty;
        deathCause = Death_Cause.None;

        isStartReleseTimer = false;
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
            float addPowerFromLife = bulletController.Life / bulletController.MaxLife;

            // ヒットパーセントが高いほど吹っ飛ぶように
            float addPowerFromHitPercent = 1 + (HitPercent / 50);

            // 吹っ飛ばし
            myRb.linearVelocity = Vector3.zero;
            myRb.AddForce((-this.transform.forward + this.transform.up) * bulletController.SmashPower * addPowerFromLife * addPowerFromHitPercent, ForceMode.Impulse);

            // VFXの生成
            Instantiate(VFXList.First(_ => _.gameObject.name == "Hit_VFX"), collision.transform.position, Quaternion.identity, VFXParent);

            // ゲームスタートしてたら
            if (gameManager.IsGameStartShared) {
                // 自分に当てたプレイヤーのコネクションIDを保持
                lastHitPlayerConnectionId = netObj.createrConnectionId;

                // ヒットパーセントを増やす
                HitPercent += bulletController.AttackPower * (bulletController.Life / bulletController.MaxLife);

                // 死因に設定
                deathCause = Death_Cause.Shot;

                // サーバーに送信
                await RoomModel.Instance.HitPercentAsync(HitPercent);
            }

            // 弾を削除
            if (bulletController != null) {
                bulletController.DestroyThisObject();
            }
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
                if (deathCause == Death_Cause.None) {
                    deathCause = Death_Cause.Fall;
                }

                releseCTS.Cancel();

                gameManager.Dead(lastHitPlayerConnectionId, deathCause);
            }
            else {
                this.transform.position = Vector3.zero;
            }
        }
    }
}
