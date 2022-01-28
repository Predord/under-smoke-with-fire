using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private int detectedHash = Animator.StringToHash("IsDetected");
    private int runningHash = Animator.StringToHash("Running");
    private int climbingHash = Animator.StringToHash("Climbing");
    private int jumpingHash = Animator.StringToHash("Jumping");
    private int coverTurnHash = Animator.StringToHash("CoverTurn");
    private int coverTurnedRightHash = Animator.StringToHash("CoverTurnedRight");
    private int coverHash = Animator.StringToHash("InCover");
    private int deadHash = Animator.StringToHash("Dead");

    private Animator playerAnimator;

    private void Start()
    {
        playerAnimator = GetComponent<Animator>();
    }

    public void SetDetection(bool isDetected)
    {
        playerAnimator.SetBool(detectedHash, isDetected);
    }

    public void SetRunningAnimation(bool isRunning)
    {
        playerAnimator.SetBool(runningHash, isRunning);
    }

    public void SetClimbingAnimation(bool isSmallCliff)
    {
        if (isSmallCliff)
        {
            playerAnimator.SetTrigger(jumpingHash);
        }
        else
        {
            playerAnimator.SetTrigger(climbingHash);
        }
    }

    public void TriggerCoverTurnAnimation()
    {
        playerAnimator.SetTrigger(coverTurnHash);
    }

    public void SetCoverTurnDirectionAnimation(bool isRightSide)
    {
        playerAnimator.SetBool(coverTurnedRightHash, isRightSide);
    }

    public void SetCoverAnimation(int coverState)
    {
        playerAnimator.SetInteger(coverHash, coverState);
    }

    public void SetDeathAnimation()
    {
        playerAnimator.SetTrigger(deadHash);
    }
}
