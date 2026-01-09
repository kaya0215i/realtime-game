using Cysharp.Threading.Tasks.Triggers;
using System.Linq;
using UnityEngine;
using static CharacterSettings;
using static GameManager;

public class MeteorController : WeaponManager {
    private bool isExplosion = false;

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();

        networkObject.sendDestroyMessage = false;

        UltimateData ultimateData = CharacterSettings.Instance.UDSO.ultimateDataList.First(_ => _.Ultimate == PLAYER_ULTIMATE.Meteor);
        MaxLife = ultimateData.Life;
        Life = ultimateData.Life;
        AttackPower = ultimateData.AttackPower;
        SmashPower = ultimateData.SmashPower;
        ShotPower = ultimateData.ShotPower;
        Radius = ultimateData.Radius;
    }

    private void Update() {
        Life -= Time.deltaTime;

        // lifeの時間経過したら削除
        if (Life <= 0) {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Field")) {
            Explosion();
        }
    }

    /// <summary>
    /// 爆発
    /// </summary>
    private void Explosion() {
        if (!isExplosion) {
            isExplosion = true;

            // VFX生成
            GameObject vfx = Instantiate(fieldHitVFX, this.transform.position, Quaternion.identity, VFXParent);

            Destroy(vfx, 3);
            Destroy(this.gameObject, 2);

            if (networkObject.createrConnectionId == networkObject.GameManager.mySelf.ConnectionId) {
                return;
            }

            var myChara = networkObject.GameManager.MyCharacter;

            if (myChara == null) {
                return;
            }

            PlayerManager playerManager = myChara.gameObject.GetComponent<PlayerManager>();
            // ヒットパーセントが高いほど吹っ飛ぶように
            float addPowerFromHitPercent = 1 + (playerManager.HitPercent / 50);

            // 吹っ飛ばし力
            float smashPower = SmashPower * addPowerFromHitPercent;

            // 吹っ飛ばし
            Vector3 explosionForce = this.transform.position;
            explosionForce.y = 0;
            playerManager.myRb.AddExplosionForce(smashPower, explosionForce, Radius, 3f, ForceMode.Impulse);

            // プレイヤーとの距離を計算
            float dist = Vector3.Distance(myChara.transform.position, this.gameObject.transform.position);

            if (dist <= Radius) {
                // ヒットパーセントを増やす
                float damageDist = 1 + ((Radius - dist) / 10);
                float attackPower = AttackPower * damageDist;
                playerManager.AddHitPercent(attackPower, Death_Cause.Meteor, networkObject.createrConnectionId);
            }
        }
    }
}