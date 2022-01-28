using System.Collections;
using UnityEngine;

public class AttackModeCommand : Command
{
    private IAttackModeInput attackModeInput;
    private Vector3 direction;
    private Coroutine rotate;

    private void Awake()
    {
        attackModeInput = GetComponent<IAttackModeInput>();
    }

    public override void Execute()
    {
        if (rotate == null)
            rotate = StartCoroutine(Rotation());
    }

    public override void Stop()
    {
        if (rotate != null)
        {
            StopCoroutine(rotate);
        }
    }

    private IEnumerator Rotation()
    {
        while (attackModeInput.AttackMode)
        {           
            direction = attackModeInput.RotationDirection();
            transform.rotation = Quaternion.LookRotation(direction - transform.position);
            attackModeInput.OnAttackModeIterationEnd();
            yield return null;
        }

        rotate = null;
    }
}
