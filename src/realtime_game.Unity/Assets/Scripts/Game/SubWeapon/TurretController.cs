using System.Linq;
using UnityEngine;
using static CharacterSettings;

public class TurretController : WeaponManager {
    // 弾を出すTransform
    [SerializeField] private Transform turretTip;

    // 弾のプレハブ
    [SerializeField] private GameObject bulletPrefab;
    // 弾を生成する親オブジェクト
    private Transform bulletParent;

    private bool isSetup = false;

    private float shotCoolTime = 1.5f;
    private float shotCoolTimer = 0f;

    private Transform targetTransform;

    private Vector3 offset = new Vector3(0, 1, 0);

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();

        bulletParent = GameObject.Find("Bullets").transform;

        networkObject.sendDestroyMessage = false;

        // 投げたプレイヤーのPlayerManagerを取得
        playerManager = networkObject.GameManager.FindCharacterObject(networkObject.createrConnectionId).GetComponent<PlayerManager>();

        // サブ武器ごとに設定
        SubWeaponData subWeaponData = CharacterSettings.Instance.SWDSO.subWeaponDataList.First(_ => _.SubWeapon == PLAYER_SUB_WEAPON.Turret);
        MaxLife = subWeaponData.Life;
        Life = 10;
        AttackPower = subWeaponData.AttackPower;
        SmashPower = subWeaponData.SmashPower;
        ShotPower = subWeaponData.ShotPower;
        Radius = subWeaponData.Radius;

        // 飛ばす処理
        myRb.AddForce(this.transform.forward * ShotPower, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision) {
        if (isSetup) {
            return;
        }

        if (collision.gameObject.CompareTag("Field")) {
            isSetup = true;
            Life = MaxLife;
            myRb.linearVelocity = Vector3.zero;
            myRb.isKinematic = true;
        }
    }

    private void Update() {
        Life -= Time.deltaTime;

        // lifeの時間経過したら削除
        if (Life <= 0) {
            Destroy(this.gameObject);
        }

        if (!isSetup) {
            return;
        }

        SelectTarget();

        ShotBullet();
    }

    /// <summary>
    /// ターゲットを選定
    /// </summary>
    private void SelectTarget() {
        targetTransform = null;

        // ターゲットを選定
        foreach (var chara in networkObject.GameManager.CharacterList.Values) {
            if (chara.playerObject == null ||
                chara.joinedData.ConnectionId == networkObject.createrConnectionId) {
                continue;
            }

            Vector3 origin = turretTip.position;
            Vector3 target = chara.playerObject.transform.position + offset;
            Vector3 dir = (target - origin).normalized;
            float maxDistance = Vector3.Distance(origin, target);
            int layerMask = 1 << 6 | 1 << 7;

            // rayを飛ばし壁に当たったら次のプレイヤーへ
            Physics.Raycast(origin, dir, out RaycastHit rayHit, maxDistance, layerMask);
            Debug.DrawRay(origin, dir * maxDistance, Color.red);

            if (rayHit.collider != null) {
                Debug.Log(rayHit.collider.gameObject.name);
            }

            if (rayHit.collider == null ||
                !rayHit.collider.gameObject.CompareTag("Player")) {
                continue;
            }

            // ターゲットに値がなかったら代入
            if (targetTransform == null) {
                targetTransform = chara.playerObject.transform;
            }
            else {
                // 距離が近かったら更新
                if (Vector3.Distance(turretTip.transform.position, targetTransform.position) > Vector3.Distance(turretTip.transform.position, chara.playerObject.transform.position)) {
                    targetTransform = chara.playerObject.transform;
                }
            }
        }

        if (targetTransform != null) {
            this.transform.LookAt(targetTransform.position + offset);
        }
    }

    /// <summary>
    /// 弾を発射
    /// </summary>
    private void ShotBullet() {
        // 作成者だったら先へ
        if (!networkObject.IsCreater()) {
            return;
        }

        // ターゲットがいなかったら何もしない
        if (targetTransform == null) {
            return;
        }

        shotCoolTimer += Time.deltaTime;

        if (shotCoolTimer >= shotCoolTime) {
            shotCoolTimer = 0;
            
            Debug.Log("発射");
            GameObject createdBullet = Instantiate(bulletPrefab,
                        this.transform.position + this.transform.forward,
                        this.transform.rotation,
                        bulletParent);
            TurretBulletController turretBulletController = createdBullet.GetComponent<TurretBulletController>();
            turretBulletController.turretController = this;
        }
    }
}
