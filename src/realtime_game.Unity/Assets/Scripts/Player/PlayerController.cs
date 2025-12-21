using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Cinemachine;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
using UnityEngine;
using static PlayerManager;

public class PlayerController : MonoBehaviour {
    private PlayerManager playerManager;
    private Rigidbody myRb;

    private NetworkObject myInteractedObj;

    [NonSerialized] private GameUIManager uiManager;

    // プレイヤーのTransform
    [SerializeField] public Transform _head;
    [SerializeField] private Transform _body;

    // プレイヤーパラメータ
    [Header("プレイヤーパラメータ")]
    // 移動速度
    [SerializeField] private float moveSpeed;
    //ジャンプ量
    [SerializeField] private float jumpValue;
    // カメラ感度
    [SerializeField] private float _sensX = 5f, _sensY = 5f;

    // プレイヤーのシネマシンカメラ
    [SerializeField] public CinemachineCamera cinemachineCamera;

    // プレイヤーフォローカメラ
    private CinemachineFollow cinemachineFollow;

    // 生成した弾を配置する親オブジェクト
    private Transform bulletParent;

    // プレイヤーカメラモードの列挙型
    private enum PLAYER_CAMERA_MODE {
        None,
        FPS,
        TPS,
    }

    // プレイヤーカメラモード
    [SerializeField] private PLAYER_CAMERA_MODE playerCameraMode;

    // 移動量
    private float horizontal, vertical;
    // 視点の移動量
    private float _yRotation, _xRotation;

    // 弾のプレハブリスト
    [SerializeField] private List<GameObject> bulletList;
    // 最大の弾の弾数
    private int maxBulletAmount;
    // 現在の弾の弾数
    private int bulletAmount;

    // 射撃間隔
    private float shotCoolTime;
    // 射撃間隔カウント用
    private float shotCoolTimer = 0;

    // ショットガンの同時に出る弾の数
    private const int ShotGunShotBulletAmount = 5;

    // リロード時間
    private float reloadTime;
    // リロード時間カウント用
    private float reloadTimer = 0;
    // リロード処理用CancellationTokenSource
    private CancellationTokenSource reloadCTS = new CancellationTokenSource();
    // リロードしているか
    private bool isReloading = false;

    private void Start() {
        playerManager = this.GetComponent<PlayerManager>();
        myRb = this.GetComponent<Rigidbody>();
        cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();
        bulletParent = GameObject.Find("Bullets").GetComponent<Transform>();
        uiManager = GameObject.Find("UICanvas").GetComponent<GameUIManager>();

        SetPlayerCameraMode();

        // キャラクターのタイプをもとに設定
        SetPlayerCharacterType();

        bulletAmount = maxBulletAmount;
    }

    private void FixedUpdate() {
        Movement();
    }

    private void Update() {
        SetPlayerCameraMode();

        if (!playerManager.IsOwner()) {
            return;
        }

        InoutMoveValue();

        Look();

        Jump();

        Interact();

        DisInteract();

        ReloadBullet();

        ShotBullet();
    }

    /// <summary>
    /// プレイヤーのカメラモードを設定
    /// </summary>
    private void SetPlayerCameraMode() {
        switch (playerCameraMode) {
            case PLAYER_CAMERA_MODE.FPS:
                cinemachineFollow.TrackerSettings.RotationDamping = new Vector3(0, 0, 0);
                cinemachineFollow.TrackerSettings.PositionDamping = new Vector3(0, 0, 0);
                cinemachineFollow.FollowOffset = new Vector3(0, 0, 0.2f);
                break;

            case PLAYER_CAMERA_MODE.TPS:
                cinemachineFollow.TrackerSettings.RotationDamping = new Vector3(0.1f, 0.1f, 0.1f);
                cinemachineFollow.TrackerSettings.PositionDamping = new Vector3(0.1f, 0.1f, 0.1f);
                cinemachineFollow.FollowOffset = new Vector3(0, 0.7f, -2.5f);
                break;
        }
    }

    /// <summary>
    /// キャラクターのタイプをもとに設定
    /// </summary>
    public void SetPlayerCharacterType() {
        switch (playerManager.characterType) {
            case PLAYER_CHARACTER_TYPE.AssaultRifle:
                maxBulletAmount = 15;
                shotCoolTime = 0.5f;
                reloadTime = 2;
                break;

            case PLAYER_CHARACTER_TYPE.ShotGun:
                maxBulletAmount = 5;
                shotCoolTime = 1;
                reloadTime = 0.5f;
                break;

            case PLAYER_CHARACTER_TYPE.SniperRifle:
                maxBulletAmount = 5;
                shotCoolTime = 2f;
                reloadTime = 3f;
                break;
        }

        // 弾数を設定
        bulletAmount = maxBulletAmount;

        // 弾数をUIに反映
        uiManager.UpdateBulletAmountText(bulletAmount, maxBulletAmount);
    }

    /// <summary>
    /// 移動量取得
    /// </summary>
    private void InoutMoveValue() {
        horizontal = Input.GetAxis("Horizontal") * moveSpeed;
        vertical = Input.GetAxis("Vertical") * moveSpeed;
    }

    /// <summary>
    /// 移動処理
    /// </summary>
    private void Movement() {
        this.transform.Translate(horizontal * Time.fixedDeltaTime, 0, vertical * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 視点処理
    /// </summary>
    private void Look() {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X") * _sensX,
            Input.GetAxis("Mouse Y") * _sensY);

        _xRotation -= mouseInput.y;
        _yRotation += mouseInput.x;
        _yRotation %= 360; // 絶対値が大きくなりすぎないように

        // 上下の視点移動量をClamp
        _xRotation = Mathf.Clamp(_xRotation, -70, 70);

        // 頭、体の向きの適用
        if (_head != null) {
            _head.transform.localRotation = Quaternion.Euler(_xRotation, 0, 0);
        }
        if (_body != null) {
            _body.transform.localRotation = Quaternion.Euler(0, _yRotation, 0);
        }
    }

    /// <summary>
    /// ジャンプ処理
    /// </summary>
    private void Jump() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (IsField()) {
                myRb.AddForce(Vector3.up * jumpValue, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// 地面についているか
    /// </summary>
    /// <returns>地面についていればTrue, ついていなければFalseを返す</returns>
    private bool IsField() {
        foreach (RaycastHit rayHit in Physics.RaycastAll(transform.position, Vector3.down, 0.3f)) {
            if (rayHit.collider.gameObject.CompareTag("Field")) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 弾を発射する処理
    /// </summary>
    private void ShotBullet() {
        // 射撃間隔
        shotCoolTimer += Time.deltaTime;
        if (!isReloading) {
            // 射撃間隔をUIに反映
            uiManager.UpdateShotCoolTimeImage(1 * shotCoolTimer / shotCoolTime);
        }

        // クールタイム経過してなかったらなにもしない
        if(shotCoolTimer < shotCoolTime) {
            return;
        }

        // 弾数が0だったらなにもしない
        if (bulletAmount <= 0 ) {
            return;
        }

        // キャラクターのタイプごとに射撃方法を変更
        string bulletName = playerManager.characterType.ToString() + "Bullet";

        switch (playerManager.characterType) {
            case PLAYER_CHARACTER_TYPE.AssaultRifle:
                if (Input.GetMouseButton(0)) {
                    // リロード中だったらタスクをキャンセル
                    if (isReloading) {
                        reloadCTS.Cancel();
                        isReloading = false;
                    }

                    bulletAmount -= 1;
                    shotCoolTimer = 0;

                    GameObject createdBullet = Instantiate(bulletList.First(_ => _.name == bulletName), Camera.main.transform.position + Camera.main.transform.forward, Quaternion.identity, bulletParent);
                    BulletController createdBulletController = createdBullet.GetComponent<BulletController>();
                }

                break;

            case PLAYER_CHARACTER_TYPE.ShotGun:
                if (Input.GetMouseButtonDown(0)) {
                    // リロード中だったらタスクをキャンセル
                    if (isReloading) {
                        reloadCTS.Cancel();
                        isReloading = false;
                    }

                    bulletAmount -= 1;
                    shotCoolTimer = 0;

                    for (int i = 0; i < ShotGunShotBulletAmount; i++) {
                        GameObject createdBullet = Instantiate(bulletList.First(_ => _.name == bulletName), Camera.main.transform.position + Camera.main.transform.forward, Quaternion.identity, bulletParent);
                        BulletController createdBulletController = createdBullet.GetComponent<BulletController>();
                    }
                }

                break;

            case PLAYER_CHARACTER_TYPE.SniperRifle:
                if (Input.GetMouseButtonDown(0)) {
                    // リロード中だったらタスクをキャンセル
                    if (isReloading) {
                        reloadCTS.Cancel();
                        isReloading = false;
                    }

                    bulletAmount -= 1;
                    shotCoolTimer = 0;

                    GameObject createdBullet = Instantiate(bulletList.First(_ => _.name == bulletName), Camera.main.transform.position + Camera.main.transform.forward, Quaternion.identity, bulletParent);
                    BulletController createdBulletController = createdBullet.GetComponent<BulletController>();
                }

                break;
        }

        // 弾数をUIに反映
        uiManager.UpdateBulletAmountText(bulletAmount, maxBulletAmount);
    }

    /// <summary>
    /// 弾のリロード
    /// </summary>
    private async void ReloadBullet() {
        // リロード中だったらなにもしない
        if (isReloading) {
            reloadTimer += Time.deltaTime;
            uiManager.UpdateShotCoolTimeImage(1 * reloadTimer / reloadTime);
            return;
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            // キャラクターのタイプごとにリロード方法を変更
            switch (playerManager.characterType) {
                case PLAYER_CHARACTER_TYPE.AssaultRifle:
                case PLAYER_CHARACTER_TYPE.SniperRifle:
                    // リロード中
                    await ReloadingBullet();

                    break;

                case PLAYER_CHARACTER_TYPE.ShotGun:
                    while (bulletAmount < maxBulletAmount) {
                        // リロード中
                        await ReloadingBullet();
                    }

                    break;
            }

            // 弾数をUIに反映
            uiManager.UpdateBulletAmountText(bulletAmount, maxBulletAmount);
        }
    }

    /// <summary>
    /// リロード中
    /// </summary>
    private async UniTask ReloadingBullet() {
        isReloading = true;
        reloadTimer = 0;
        uiManager.UpdateShotCoolTimeImage(0);

        // リロード時間分待つ
        await UniTask.Delay(TimeSpan.FromSeconds(reloadTime), cancellationToken: reloadCTS.Token).SuppressCancellationThrow();
        // リロードがキャンセルされたら
        if (reloadCTS.Token.IsCancellationRequested) {
            isReloading = false;
            reloadCTS = new CancellationTokenSource();
            return;
        }

        // キャラクターのタイプごとにリロード方法を変更
        switch (playerManager.characterType) {
            case PLAYER_CHARACTER_TYPE.AssaultRifle:
            case PLAYER_CHARACTER_TYPE.SniperRifle:
                bulletAmount = maxBulletAmount;

                break;

            case PLAYER_CHARACTER_TYPE.ShotGun:
                if (bulletAmount < maxBulletAmount) {
                    bulletAmount += 1;
                }

                break;
        }

        isReloading = false;
    }

    /// <summary>
    /// インタラクト処理
    /// </summary>
    private async void Interact() {
        NetworkObject netObj;
        bool canInteract = false;
        foreach (RaycastHit rayHit in Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward.normalized, 8f)) {
            if (rayHit.collider.gameObject.CompareTag("ItemObject")) {
                netObj = rayHit.collider.gameObject.GetComponent<NetworkObject>();
                if(netObj.updateType != NetworkObject.UpdateObjectTypes.Interactor) {
                    continue;
                }
                if (!netObj.isInteracting) {
                    canInteract = true;
                }
                if(Input.GetKeyDown(KeyCode.E)) {
                    if(await netObj.InteractObject()) {
                        myInteractedObj = netObj;
                    }
                }
            }
        }
    }

    /// <summary>
    /// インタラクトオブジェクトの破棄
    /// </summary>
    private void DisInteract() {
        if (myInteractedObj != null &&
            myInteractedObj.updateType == NetworkObject.UpdateObjectTypes.Interactor) {
            myInteractedObj.transform.position = this.gameObject.transform.position + new Vector3(0, 3f, 0);

            if (Input.GetKeyDown(KeyCode.F)) {
                myInteractedObj.DisInteractObject();
                myInteractedObj = null;
            }
        }
    }
}
