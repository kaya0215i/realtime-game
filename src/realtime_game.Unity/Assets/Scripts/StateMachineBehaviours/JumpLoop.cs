using System.Linq;
using UnityEngine;

public class JumpLoop : StateMachineBehaviour {
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

    //}

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        // 地面に近づいたらジャンプ終わり
        float yDistance = 0;

        RaycastHit[] rayHits = Physics.RaycastAll(animator.transform.position, -animator.transform.up);
        rayHits = rayHits.OrderBy(hit => hit.distance).ToArray();

        foreach (RaycastHit ray in rayHits) {
            if (ray.collider.gameObject.CompareTag("Field")) {
                yDistance = animator.transform.position.y - ray.point.y;
                if (yDistance <= 0.5f) {
                    animator.SetTrigger("EndJump");
                }

                break;
            }
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
