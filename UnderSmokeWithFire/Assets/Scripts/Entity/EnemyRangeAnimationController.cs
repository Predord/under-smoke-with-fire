using System.Collections;
using UnityEngine;

public class EnemyRangeAnimationController : EnemyAnimationController
{
    private int idleStateHash = Animator.StringToHash("Base Layer.Idle Range");
    private int runningHash = Animator.StringToHash("RangeRunning");
    private int climbingHash = Animator.StringToHash("RangeClimbing");
    private int jumpingHash = Animator.StringToHash("RangeJumping");
    private int attackHash = Animator.StringToHash("RangeAttack");
    private int deadHash = Animator.StringToHash("RangeDead");

    public override void PauseAnimations()
    {
        AnimatorStateInfo stateInfo = enemyAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.fullPathHash != idleStateHash)
        {
            enemyAnimator.speed = 0f;
        }
    }

    public void SetClimbingAnimation(bool isSmallCliff)
    {
        if (isSmallCliff)
        {
            enemyAnimator.SetTrigger(jumpingHash);
        }
        else
        {
            enemyAnimator.SetTrigger(climbingHash);
        }
    }

    public void SetRunningAnimation(bool isRunning)
    {
        enemyAnimator.SetBool(runningHash, isRunning);
    }

    public void SetAttackAnimationTrigger()
    {
        enemyAnimator.SetTrigger(attackHash);
    }

    public void SetDeathAnimationTrigger()
    {
        enemyAnimator.SetTrigger(deadHash);
    }

    public void EnableRagdoll()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (var rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
        }

        enemyAnimator.enabled = false;
        StartCoroutine(DisableRagdoll());
    }

    private IEnumerator DisableRagdoll()
    {
        yield return new WaitForSeconds(3f);

        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (var rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }
    }
}
