using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour {
    private Animator myAnimator;

    private bool isAir = false;
    public bool isReloading = false;

    private int observeStateNum = -1;

    private void Start() {
        myAnimator = GetComponent<Animator>();
    }

    private void Update() {
        // 地面から離れてたら空中アニメーション
        float yDistance = 0;
        RaycastHit[] rayHits = Physics.RaycastAll(transform.position, -transform.up);
        rayHits = rayHits.OrderBy(hit => hit.distance).ToArray();

        foreach (RaycastHit ray in rayHits) {
            if (ray.collider.gameObject.CompareTag("Field")) {
                yDistance = transform.position.y - ray.point.y;
                if (yDistance > 1f &&
                    !isAir &&
                    !isReloading) {
                    isAir = true;
                    myAnimator.SetTrigger("JumpLoop");
                }
                else if (yDistance < 0.1f){
                    isAir = false;
                }

                break;
            }
        }
    }

    /// <summary>
    /// [サーバー通知]
    /// アニメーション通知(Trigger)
    /// </summary>
    public void OnAnimationTrigger(string animName) {
        myAnimator.SetTrigger(animName);
    }

    /// <summary>
    /// [サーバー通知]
    /// アニメーション通知(State)
    /// </summary>
    public void OnAnimationState(int state) {
        myAnimator.SetInteger("State", state);
    }

    /// <summary>
    /// ジャンプアニメーション
    /// </summary>
    public async void JumpAnimation() {
        if (myAnimator == null) {
            return;
        }

        if (isReloading) {
            return;
        }

        myAnimator.SetTrigger("Jump");
        await RoomModel.Instance.AnimationTriggerAsync("Jump");
    }

    /// <summary>
    /// 移動アニメーション
    /// </summary>
    public async void MoveAnimation(float horizontal, float vertical) {
        if (myAnimator == null) {
            return;
        }

        int stateNum = 0;

        if (vertical > 0) {
            stateNum = 1;

            if (horizontal > 3.5f) {
                stateNum = 4;
            }
            else if (horizontal < -3.5f) {
                stateNum = 3;
            }
        }
        else if (vertical < 0) {
            stateNum = 2;

            if (horizontal > 3.5f) {
                stateNum = 4;
            }
            else if (horizontal < -3.5f) {
                stateNum = 3;
            }
        }
        else {
            if (horizontal > 0) {
                stateNum = 4;
            }
            else if (horizontal < 0) {
                stateNum = 3;
            }
        }

        // 違ったら
        if (stateNum != observeStateNum) {
            observeStateNum = stateNum;

            myAnimator.SetInteger("State", stateNum);
            await RoomModel.Instance.AnimationStateAsync(stateNum);
        }
    }

    /// <summary>
    /// リロードアニメーション開始
    /// </summary>
    public async void StartReloadAnimation() {
        if (myAnimator == null) {
            return;
        }

        myAnimator.SetTrigger("Reload");
        await RoomModel.Instance.AnimationTriggerAsync("Reload");
    }

    /// <summary>
    /// リロードアニメーション終了
    /// </summary>
    public async void EndReloadAnimation() {
        if (myAnimator == null) {
            return;
        }

        myAnimator.SetTrigger("EndReload");
        await RoomModel.Instance.AnimationTriggerAsync("EndReload");
    }

    /// <summary>
    /// 射撃アニメーション
    /// </summary>
    public async void ShotAnimation() {
        if (myAnimator == null) {
            return;
        }

        myAnimator.SetTrigger("Shot");
        await RoomModel.Instance.AnimationTriggerAsync("Shot");
    }
}