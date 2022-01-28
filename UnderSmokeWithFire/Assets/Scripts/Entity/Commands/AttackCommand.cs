using System.Collections;
using UnityEngine;

public class AttackCommand : Command
{
    private Coroutine rotating;
    private Entity entity;
    private IAttackInput attackInput;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        attackInput = GetComponent<IAttackInput>();
    }

    public override void Execute()
    {
        if (rotating == null)
            rotating = StartCoroutine(RotateTo());
    }

    public override void Stop()
    {
        if (rotating != null)
        {
            StopCoroutine(rotating);
        }
    }

    private IEnumerator RotateTo()
    {
        float travelTime = Time.deltaTime * GameManager.Instance.GameRunningSpeed;

        while (attackInput.IsAttacking())
        {
            Entity target = attackInput.GetTarget();
            Vector3 rotationPoint;

            for (int i = 0; i < attackInput.TurnsToHit - 1; i++)
            {
                rotationPoint = target.gameObject.transform.position;
                rotationPoint.y = transform.localPosition.y;
                transform.LookAt(rotationPoint);
                entity.Orientation = transform.localRotation.eulerAngles.y;

                for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
                {
                    yield return null;
                }

                travelTime -= 1f;

                entity.SpentActionPoints++;
                yield return new WaitWhile(() => entity.SpentActionPoints > Player.Instance.SpentActionPoints);

                entity.ActionPoints -= 1f;
                if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
                {
                    entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
                }

                yield return new WaitWhile(() => entity.ActionPoints == 0f);

                if (!attackInput.IsAttacking())
                {
                    break;
                }
            }

            if (!attackInput.IsAttacking())
            {
                break;
            }

            rotationPoint = target.gameObject.transform.position;
            rotationPoint.y = transform.localPosition.y;
            transform.LookAt(rotationPoint);
            entity.Orientation = transform.localRotation.eulerAngles.y;

            for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
            {
                yield return null;
            }

            travelTime -= 1f;

            attackInput.InflicteDamage(target);
            entity.SpentActionPoints++;
            yield return new WaitWhile(() => entity.SpentActionPoints > Player.Instance.SpentActionPoints);

            entity.ActionPoints -= 1f;
            if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
            {
                entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
            }

            yield return new WaitWhile(() => entity.ActionPoints == 0f);
        }

        rotating = null;
        entity.InAction = false;
    }
}
