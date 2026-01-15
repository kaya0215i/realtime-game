using DG.Tweening;
using System;
using System.Linq;
using UnityEngine;
using static CharacterSettings;
using static GameManager;

public class HomingMissileController : WeaponManager {
    // 追跡対象
    private Transform targetTransform;

    private Vector3 offset = new Vector3(0, 1, 0);

    private void Start() {
        networkObject = this.GetComponent<NetworkObject>();
        myRb = this.GetComponent<Rigidbody>();
        myAudioSource = this.GetComponent<AudioSource>();

        // AudioSource設定
        AudioManager.Instance.SetAudioSouceVolume(myAudioSource);

        UltimateData ultimateData = CharacterSettings.Instance.UDSO.ultimateDataList.First(_ => _.Ultimate == PLAYER_ULTIMATE.HomingMissile);
        MaxLife = ultimateData.Life;
        Life = ultimateData.Life;
        AttackPower = ultimateData.AttackPower;
        SmashPower = ultimateData.SmashPower;
        ShotPower = ultimateData.ShotPower;
        Radius = ultimateData.Radius;
    }

    private void Update() {
        if (!networkObject.IsCreater()) {
            return;
        }

        SelectTarget();
        myRb.AddForce(this.transform.forward * ShotPower, ForceMode.Force);
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

            Vector3 origin = this.transform.position;
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
                if (Vector3.Distance(this.transform.position, targetTransform.position) > Vector3.Distance(this.transform.position, chara.playerObject.transform.position)) {
                    targetTransform = chara.playerObject.transform;
                }
            }
        }

        if (targetTransform != null) {
            this.transform.DOLookAt(targetTransform.position + offset, 0.1f);
        }
    }

    /// <summary>
    /// このスクリプトがアタッチされているゲームオブジェクトを削除する
    /// </summary>
    public void DestroyThisObject() {
        Destroy(this.gameObject);
    }
}
