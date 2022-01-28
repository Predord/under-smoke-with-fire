using System.Collections;
using UnityEngine;

public class AttackAreaCommand : Command
{
    private Coroutine casting;
    private Entity entity;
    private IAttackAreaInput attackAreaInput;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        attackAreaInput = GetComponent<IAttackAreaInput>();
    }

    public override void Execute()
    {
        if (casting == null)
            casting = StartCoroutine(Casting());
    }

    public override void Stop()
    {
        if (casting != null)
        {
            StopCoroutine(casting);
        }
    }

    private IEnumerator Casting()
    {
        float travelTime = Time.deltaTime * GameManager.Instance.GameRunningSpeed;
        //Spent = 0
        while (attackAreaInput.IsWaitingForExecuteTurn())
        {
            for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
            {
                yield return null;
            }

            travelTime -= 1f;

            if (attackAreaInput.WaitingIterationEnd())
            {
                attackAreaInput.CancelAttack();
                StopCoroutine(casting);
                casting = null;
                yield return null;
            }

            yield return new WaitWhile(() => entity.SpentActionPoints > Player.Instance.SpentActionPoints);

            /*
            if(entity != Player.Instance)
            {
                entity.ActionPoints -= 1f;
                if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
                {
                    entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
                }

                yield return new WaitWhile(() => entity.ActionPoints == 0f);
            }*/
        }

        while (attackAreaInput.ExecuteAttack())
        {
            yield return null;
        }

        travelTime = Time.deltaTime * GameManager.Instance.GameRunningSpeed;

        for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
        {
            yield return null;
        }

        yield return new WaitWhile(() => entity.SpentActionPoints > Player.Instance.SpentActionPoints);

        attackAreaInput.AttackAreaEnd();
        casting = null;
    }
}
