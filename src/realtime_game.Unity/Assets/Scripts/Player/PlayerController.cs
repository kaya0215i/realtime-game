using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static CharacterData;

public class PlayerController : MonoBehaviour {
    private GameManager gameManager;
    private PlayerManager playerManager;
    private Rigidbody myRb;

    // プレイヤーの入力
    private PlayerInputActions playerInputActions;

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
    // リザルト用シネマシンカメラ
    [SerializeField] public CinemachineCamera resultCinemachineCamera;

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
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerManager = this.GetComponent<PlayerManager>();
        myRb = this.GetComponent<Rigidbody>();
        cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();
        bulletParent = GameObject.Find("Bullets").GetComponent<Transform>();
        uiManager = GameObject.Find("UICanvas").GetComponent<GameUIManager>();

        SetPlayerCameraMode();

        // キャラクターのタイプをもとに設定
        SetPlayerCharacterType();

        // インプットアクションを有効に
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        bulletAmount = maxBulletAmount;
    }

    private void OnDestroy() {
        playerInputActions.Disable();
    }

    private void FixedUpdate() {
        if (!gameManager.IsPlaying) {
            return;
        }

        Movement();
    }

    private void Update() {
        if (!gameManager.IsPlaying) {
            return;
        }

        SetPlayerCameraMode();

        if (!playerManager.IsOwner()) {
            return;
        }

        InputMoveValue();

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
    private void InputMoveValue() {
        horizontal = playerInputActions.Player.Move.ReadValue<Vector2>().x * moveSpeed * Time.deltaTime;
        vertical = playerInputActions.Player.Move.ReadValue<Vector2>().y * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 移動処理
    /// </summary>
    private void Movement() {
        this.transform.Translate(horizontal, 0, vertical);
    }

    /// <summary>
    /// 視点処理
    /// </summary>
    private void Look() {
        bool touchLookActive = true;
        if (playerInputActions.Player.TouchStart.IsPressed()) {
            // primaryTouch の現在位置を取得
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch != null) {
                Vector2 startPos = ts.primaryTouch.position.ReadValue();
                Debug.Log(startPos);
                // タッチを始めた場所が右半分だったら
                touchLookActive = (startPos.x >= Screen.width * 0.5f);
            }
            else {
                touchLookActive = false;
            }
        }

        if (!touchLookActive) {
            return;
        }

        Vector2 mouseInput = new Vector2(playerInputActions.Player.Look.ReadValue<Vector2>().x * _sensX * Time.deltaTime,
            playerInputActions.Player.Look.ReadValue<Vector2>().y * _sensY * Time.deltaTime);

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
        if (playerInputActions.Player.Jump.triggered) {
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
                if (playerInputActions.Player.Shot.IsPressed()) {
                    // リロード中だったらタスクをキャンセル
                    if (isReloading) {
                        reloadCTS.Cancel();
                        isReloading = false;
                    }

                    bulletAmount -= 1;
                    shotCoolTimer = 0;

                    GameObject createdBullet = Instantiate(bulletList.First(_ => _.name == bulletName), Camera.main.transform.position + Camera.main.transform.forward, _head.transform.rotation, bulletParent);
                    BulletController createdBulletController = createdBullet.GetComponent<BulletController>();
                }

                break;

            case PLAYER_CHARACTER_TYPE.ShotGun:
                if (playerInputActions.Player.Shot.triggered) {
                    // リロード中だったらタスクをキャンセル
                    if (isReloading) {
                        reloadCTS.Cancel();
                        isReloading = false;
                    }

                    bulletAmount -= 1;
                    shotCoolTimer = 0;

                    for (int i = 0; i < ShotGunShotBulletAmount; i++) {
                        GameObject createdBullet = Instantiate(bulletList.First(_ => _.name == bulletName), Camera.main.transform.position + Camera.main.transform.forward, _head.transform.rotation, bulletParent);
                        BulletController createdBulletController = createdBullet.GetComponent<BulletController>();
                    }
                }

                break;

            case PLAYER_CHARACTER_TYPE.SniperRifle:
                if (playerInputActions.Player.Shot.triggered) {
                    // リロード中だったらタスクをキャンセル
                    if (isReloading) {
                        reloadCTS.Cancel();
                        isReloading = false;
                    }

                    bulletAmount -= 1;
                    shotCoolTimer = 0;

                    GameObject createdBullet = Instantiate(bulletList.First(_ => _.name == bulletName), Camera.main.transform.position + Camera.main.transform.forward, _head.transform.rotation, bulletParent);
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

        if (playerInputActions.Player.Reload.triggered) {
            // キャラクターのタイプごとにリロード方法を変更
            switch (playerManager.characterType) {
                case PLAYER_CHARACTER_TYPE.AssaultRifle:
                case PLAYER_CHARACTER_TYPE.SniperRifle:
                    // リロード中
                    await ReloadingBullet().SuppressCancellationThrow();

                    break;

                case PLAYER_CHARACTER_TYPE.ShotGun:
                    while (bulletAmount < maxBulletAmount) {
                        // リロード中
                        await ReloadingBullet().SuppressCancellationThrow();
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
        await UniTask.Delay(TimeSpan.FromSeconds(reloadTime), cancellationToken: reloadCTS.Token);
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
