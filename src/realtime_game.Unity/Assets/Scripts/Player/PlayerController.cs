using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static CharacterSettings;

public class PlayerController : MonoBehaviour {
    private GameManager gameManager;
    private PlayerManager playerManager;

    private Rigidbody myRb;

    private PlayerAnimation playerAnimation;

    // プレイヤーの入力
    private PlayerInputActions playerInputActions;
    // インタラクト中のオブジェクト
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

    // プレイヤーのシネマシンカメラ
    [SerializeField] public CinemachineCamera cinemachineCamera;
    // リザルト用シネマシンカメラ
    [SerializeField] public CinemachineCamera resultCinemachineCamera;

    // プレイヤーフォローカメラ
    private CinemachineFollow cinemachineFollow;

    // 生成した弾を配置する親オブジェクト
    private Transform bulletParent;
    // 生成したオブジェクトを配置する親オブジェクト
    private Transform objectParent;

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

    // タッチ操作用
    private bool touchLookActive = true;

    // サブ武器のクールタイム
    private float subWeaponCoolTime;
    // サブ武器のクールタイムカウント用
    private float subWeaponCoolTimer = 0;

    // アルティメット最大チャージ量
    private float ultimateMaxChargeAmount;
    // アルティメットチャージ量
    private float ultimateChargeAmount = 0;

    private void Start() {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        playerManager = this.GetComponent<PlayerManager>();
        myRb = this.GetComponent<Rigidbody>();
        playerAnimation = this.GetComponent<PlayerAnimation>();
        cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();
        bulletParent = GameObject.Find("Bullets").GetComponent<Transform>();
        objectParent = GameObject.Find("Objects").GetComponent<Transform>();
        uiManager = GameObject.Find("UICanvas").GetComponent<GameUIManager>();

        SetPlayerCameraMode();

        // キャラクターのロードアウトをもとに設定
        SetCharacterSettingsFromLoadout();

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

        //Interact();

        //DisInteract();

        ReloadBullet();

        ShotBullet();

        UseSubWeapon();

        UseAltimate();
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
    /// キャラクターのロードアウトをもとに設定
    /// </summary>
    public void SetCharacterSettingsFromLoadout() {
        // キャラクタータイプ
        CharacterData characterData = CharacterSettings.Instance.CDSO.characterDataList.First(_=>_.characterType == playerManager.characterType);
        maxBulletAmount = characterData.maxBulletAmount;
        shotCoolTime = characterData.shotCoolTime;
        reloadTime = characterData.reloadTime;

        // 弾数を設定
        bulletAmount = maxBulletAmount;

        // 弾数をUIに反映
        uiManager.UpdateBulletAmountText(bulletAmount, maxBulletAmount);

        // サブ武器
        SubWeaponData subWeaponData = CharacterSettings.Instance.SWDSO.subWeaponDataList.First(_=>_.SubWeapon == playerManager.subWeapon);
        subWeaponCoolTime = subWeaponData.CoolTime;
        subWeaponCoolTimer = 0;

        // アルティメット
        UltimateData ultimateData = CharacterSettings.Instance.UDSO.ultimateDataList.First(_ => _.Ultimate == playerManager.ultimate);
        ultimateMaxChargeAmount = ultimateData.ChargeAmount;
        ultimateChargeAmount = 0;
    }

    /// <summary>
    /// 移動量取得
    /// </summary>
    private void InputMoveValue() {
        horizontal = playerInputActions.Player.Move.ReadValue<Vector2>().x * moveSpeed;
        vertical = playerInputActions.Player.Move.ReadValue<Vector2>().y * moveSpeed;
    }

    /// <summary>
    /// 移動処理
    /// </summary>
    private void Movement() {
        this.transform.Translate(horizontal * Time.deltaTime, 0, vertical * Time.deltaTime);
        playerAnimation.MoveAnimation(horizontal, vertical);
    }

    /// <summary>
    /// 視点処理
    /// </summary>
    private void Look() {
        // タッチ操作だったら
        if (playerInputActions.Player.TouchStart.triggered) {
            // primaryTouch の現在位置を取得
            var ts = Touchscreen.current;
            if (ts != null && ts.primaryTouch != null) {
                Vector2 startPos = ts.primaryTouch.position.ReadValue();
                // タッチを始めた場所が右半分だったら
                touchLookActive = (startPos.x >= Screen.width * 0.5f);
            }
            else {
                touchLookActive = false;
            }
        }
        // マウス操作だったら
        else if (playerInputActions.Player.MouseStart.triggered) {
            touchLookActive = true;
        }

        if (!touchLookActive) {
            return;
        }

        Vector2 mouseInput = new Vector2(playerInputActions.Player.Look.ReadValue<Vector2>().x * CharacterSettings.Instance.SensX * Time.deltaTime,
            playerInputActions.Player.Look.ReadValue<Vector2>().y * CharacterSettings.Instance.SensY * Time.deltaTime);

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
                playerAnimation.JumpAnimation();
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

                    GameObject createdBullet = Instantiate(CharacterSettings.Instance.BDSO.bulletDataList.First(_=>_.CharacterType == playerManager.characterType).ObjectPrefab, 
                        Camera.main.transform.position + Camera.main.transform.forward, 
                        _head.transform.rotation, 
                        bulletParent);
                    // 弾投げアニメーション
                    playerAnimation.ShotAnimation();
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
                        GameObject createdBullet = Instantiate(CharacterSettings.Instance.BDSO.bulletDataList.First(_ => _.CharacterType == playerManager.characterType).ObjectPrefab, 
                            Camera.main.transform.position + Camera.main.transform.forward, 
                            _head.transform.rotation, 
                            bulletParent);
                    }
                    // 弾投げアニメーション
                    playerAnimation.ShotAnimation();
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

                    GameObject createdBullet = Instantiate(CharacterSettings.Instance.BDSO.bulletDataList.First(_ => _.CharacterType == playerManager.characterType).ObjectPrefab, 
                        Camera.main.transform.position + Camera.main.transform.forward, 
                        _head.transform.rotation, 
                        bulletParent);
                    // 弾投げアニメーション
                    playerAnimation.ShotAnimation();
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
        // 弾が減ってなかったら何もしない
        if (bulletAmount == maxBulletAmount) {
            return;
        }

        // リロード中だったらなにもしない
        if (isReloading) {
            reloadTimer += Time.deltaTime;
            uiManager.UpdateShotCoolTimeImage(1 * reloadTimer / reloadTime);
            return;
        }

        if (playerInputActions.Player.Reload.triggered) {
            // リロードアニメーション開始
            playerAnimation.StartReloadAnimation();

            // キャラクターのタイプごとにリロード方法を変更
            switch (playerManager.characterType) {
                case PLAYER_CHARACTER_TYPE.AssaultRifle:
                case PLAYER_CHARACTER_TYPE.SniperRifle:
                    // リロード中
                    try {
                        await ReloadingBullet();
                    }
                    // リロードがキャンセルされたら
                    catch {
                        isReloading = false;
                        reloadCTS = new CancellationTokenSource();
                        return;
                    }

                    break;

                case PLAYER_CHARACTER_TYPE.ShotGun:
                    while (bulletAmount < maxBulletAmount) {
                        // リロード中
                        try {
                            await ReloadingBullet();
                        }
                        // リロードがキャンセルされたら
                        catch {
                            isReloading = false;
                            reloadCTS = new CancellationTokenSource();
                            return;
                        }
                    }

                    break;
            }

            // 弾数をUIに反映
            uiManager.UpdateBulletAmountText(bulletAmount, maxBulletAmount);
            // リロードアニメーション終了
            playerAnimation.EndReloadAnimation();
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
    /// サブ武器使用
    /// </summary>
    private void UseSubWeapon() {
        if (!gameManager.IsGameStartShared) {
            return;
        }

        // クールタイムを加算
        if (subWeaponCoolTimer < subWeaponCoolTime) {
            subWeaponCoolTimer += Time.deltaTime;

            // ボタン画像に反映
            uiManager.UpdateSubWeaponCoolTime(1 * subWeaponCoolTimer / subWeaponCoolTime);
            return;
        }

        if (playerInputActions.Player.SubWeapon.triggered) {
            // リロード中だったらタスクをキャンセル
            if (isReloading) {
                reloadCTS.Cancel();
                isReloading = false;
            }

            // オブジェクト生成
            GameObject createdSubWeapon = Instantiate(CharacterSettings.Instance.SWDSO.subWeaponDataList.First(_ => _.SubWeapon == playerManager.subWeapon).ObjectPrefab,
                Camera.main.transform.position + Camera.main.transform.forward,
                _head.transform.rotation,
                objectParent);

            // 弾投げアニメーション
            playerAnimation.ShotAnimation();

            // クールタイムリセット
            subWeaponCoolTimer = 0;
        }
    }

    /// <summary>
    /// アルティメット使用
    /// </summary>
    private void UseAltimate() {
        if (!gameManager.IsGameStartShared) {
            return;
        }

        // チャージ量加算
        if (ultimateChargeAmount < ultimateMaxChargeAmount) {
            ultimateChargeAmount += Time.deltaTime;

            // ボタン画像に反映
            uiManager.UpdateUltimateCoolTime(1 * ultimateChargeAmount / ultimateMaxChargeAmount);
            return;
        }

        if (playerInputActions.Player.Ultimate.triggered) {
            // リロード中だったらタスクをキャンセル
            if (isReloading) {
                reloadCTS.Cancel();
                isReloading = false;
            }


            switch (playerManager.ultimate) {
                case PLAYER_ULTIMATE.Meteor:
                    // オブジェクト生成
                    GameObject createdMeteor = Instantiate(CharacterSettings.Instance.UDSO.ultimateDataList.First(_ => _.Ultimate == playerManager.ultimate).ObjectPrefab,
                        this.transform.position + new Vector3(0, 100, 0),
                        Quaternion.identity,
                        objectParent);

                    break;

                case PLAYER_ULTIMATE.HomingMissile:
                    Vector3 position = this.transform.position + new Vector3(0, 1, 0);

                    for (int i = 0; i < 5; i++) {
                        float angleRange = Mathf.Deg2Rad * 100f;
                        float theta = angleRange / (5 - 1) * i + Mathf.Deg2Rad * (90f - 100f / 2f);
                        Vector3 dir = position + new Vector3(Mathf.Cos(theta), Mathf.Sin(theta)) - position;
                        // オブジェクト生成
                        GameObject createdHomingMissile = Instantiate(CharacterSettings.Instance.UDSO.ultimateDataList.First(_ => _.Ultimate == playerManager.ultimate).ObjectPrefab,
                            position + dir * 1.2f,
                            Quaternion.FromToRotation(transform.forward, dir),
                            objectParent);
                    }

                    break;
            }

            // チャージ量リセット
            ultimateChargeAmount = 0;
        }
    }

    /// <summary>
    /// キルしたときのアルティメットチャージ
    /// </summary>
    public void AddUltimateChargeAmount() {
        ultimateChargeAmount += ultimateMaxChargeAmount / 5;
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
