using System.Collections;
using UnityEngine;

public class NoActionCommand : Command
{
    private Coroutine waiting;
    private Entity entity;

    private void Awake()
    {
        entity = GetComponent<Entity>();
    }

    public override void Execute()
    {
        entity.InAction = true;

        if (waiting == null)
            waiting = StartCoroutine(Wait());
    }

    public override void Stop()
    {
        if (waiting != null)
        {
            StopCoroutine(waiting);
        }
    }

    private IEnumerator Wait()
    {
        float travelTime = Time.deltaTime * GameManager.Instance.GameRunningSpeed;

        for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
        {
            yield return null;
        }

        entity.SpentActionPoints++;

        yield return new WaitWhile(() => entity.SpentActionPoints > Player.Instance.SpentActionPoints);

        entity.ActionPoints -= 1f;
        if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
        {
            entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
        }

        waiting = null;
        entity.InAction = false;
    }
}
