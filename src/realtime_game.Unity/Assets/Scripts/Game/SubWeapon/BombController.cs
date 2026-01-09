using System.Linq;
using UnityEngine;
using static CharacterSettings;
using static GameManager;

public class BombController : WeaponManager {
     private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();

        networkObject.sendDestroyMessage = false;

        // 投げたプレイヤーのPlayerManagerを取得
        playerManager = networkObject.GameManager.FindCharacterObject(networkObject.createrConnectionId).GetComponent<PlayerManager>();

        // サブ武器ごとに設定
        SubWeaponData subWeaponData = CharacterSettings.Instance.SWDSO.subWeaponDataList.First(_ => _.SubWeapon == PLAYER_SUB_WEAPON.Bomb);
        MaxLife = subWeaponData.Life;
        Life = subWeaponData.Life;
        AttackPower = subWeaponData.AttackPower;
        SmashPower = subWeaponData.SmashPower;
        ShotPower = subWeaponData.ShotPower;
        Radius = subWeaponData.Radius;

        // 飛ばす処理
        myRb.AddForce(this.transform.forward * ShotPower, ForceMode.Impulse);
    }

    private void Update() {
        Life -= Time.deltaTime;

        // lifeの時間経過したら爆発
        if (Life <= 0) {
            Explosion();
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 爆発
    /// </summary>
    private void Explosion() {
        // VFX生成
        Instantiate(fieldHitVFX, this.transform.position, Quaternion.identity, VFXParent);

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
        playerManager.myRb.AddExplosionForce(smashPower, this.transform.position, Radius, 3f, ForceMode.Impulse);

        // プレイヤーとの距離を計算
        float dist = Vector3.Distance(myChara.transform.position, this.gameObject.transform.position);

        if (dist <= Radius) {
            // ヒットパーセントを増やす
            float damageDist = 1 + ((Radius - dist) / 10);
            float attackPower = AttackPower * damageDist;
            playerManager.AddHitPercent(attackPower, Death_Cause.Bomb, networkObject.createrConnectionId);
        }
    }
}
