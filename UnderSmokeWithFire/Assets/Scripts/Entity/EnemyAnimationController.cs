using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    protected Animator enemyAnimator;

    private void Start()
    {
        enemyAnimator = GetComponent<Animator>();
    }

    public void ResumeAnimations()
    {
        enemyAnimator.speed = 1f;
    }

    public virtual void PauseAnimations()
    {
        enemyAnimator.speed = 0f;
    }
}
