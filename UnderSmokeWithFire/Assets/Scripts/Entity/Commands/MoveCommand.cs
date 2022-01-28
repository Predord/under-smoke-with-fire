using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : Command
{
    private Coroutine moving;
    private List<QuadCell> pathToTravel;
    private Entity entity;
    private IMoveInput moveInput;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        moveInput = GetComponent<IMoveInput>();
    }

    public override void Execute()
    {
        pathToTravel = moveInput.GetPath();
        moveInput.MoveStart(pathToTravel[1]);

        if (moving == null && pathToTravel != null)
            moving = StartCoroutine(TravelPath());
    }

    public override void Stop()
    {
        if(moving != null)
        {
            StopCoroutine(moving);
        }
    }

    private IEnumerator TravelPath()
    {
        bool walkToCellCenter = false;
        float travelTime = Time.deltaTime * GameManager.Instance.GameRunningSpeed;
        Vector3 a, b, c;
        a = b = c = pathToTravel[0].Slope ?
            pathToTravel[0].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[0].Position;

        Vector3 rotationPoint = Vector3.zero;
        Quaternion fromRotation = Quaternion.identity, toRotation = Quaternion.identity;
        float angle = 0f, speed = 0f, rotationTime = 0f;

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            //check slope with direction
            if (pathToTravel[i - 1].Elevation == pathToTravel[i].Elevation || ((pathToTravel[i - 1].Slope || pathToTravel[i].Slope) &&
                (pathToTravel[i - 1].Elevation - pathToTravel[i].Elevation == 1 || pathToTravel[i].Elevation - pathToTravel[i - 1].Elevation == 1)))
            {
                b = pathToTravel[i - 1].Slope ? pathToTravel[i - 1].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[i - 1].Position;
                a = walkToCellCenter ? c : b;
                c = (b + (pathToTravel[i].Slope ? pathToTravel[i].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[i].Position)) * 0.5f;

                if (!walkToCellCenter)
                {
                    rotationPoint = pathToTravel[i].Position;
                    rotationPoint.y = transform.localPosition.y;
                    fromRotation = transform.localRotation;
                    toRotation = Quaternion.LookRotation(rotationPoint - transform.localPosition);
                    angle = Quaternion.Angle(fromRotation, toRotation);
                    speed = entity.rotationSpeed / angle;
                    rotationTime = Time.deltaTime * speed;
                }

                for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
                {
                    if (walkToCellCenter)
                    {
                        Vector3 d = Bezier.GetDerivative(a, b, c, travelTime);
                        d.y = 0f;
                        transform.localRotation = Quaternion.LookRotation(d);
                    }
                    else
                    {
                        if (angle > 0f)
                        {
                            rotationTime += Time.deltaTime * speed;
                            if (rotationTime >= 1f)
                            {
                                rotationPoint.y = transform.localPosition.y;
                                transform.LookAt(rotationPoint);
                                entity.Orientation = transform.localRotation.eulerAngles.y;
                                walkToCellCenter = true;
                            }
                            else
                            {
                                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, rotationTime);
                            }
                        }
                        else
                        {
                            walkToCellCenter = true;
                        }
                    }
                    transform.localPosition = Bezier.GetPoint(a, b, c, travelTime);
                    yield return null;
                }

                travelTime -= 1f;

                if (moveInput.IterationEndActionPointSpend(pathToTravel[i]))
                {
                    yield return new WaitWhile(() => entity.ActionPoints == 0f || entity.SpentActionPoints > Player.Instance.SpentActionPoints);
                }

                if (moveInput.MoveIterationEnd())
                {
                    if(entity == Player.Instance)
                    {
                        break;
                    }
                    else
                    {
                        pathToTravel = moveInput.GetPath();
                        i = pathToTravel.Count == 2 ? 0 : i;
                    }
                }

                if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
                {
                    entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
                    yield return new WaitWhile(() => entity.ActionPoints == 0f);
                }
            }
            else
            {
                if (walkToCellCenter)
                {
                    a = c;
                    b = pathToTravel[i - 1].Slope ? pathToTravel[i - 1].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[i - 1].Position;
                    c = b;
                    for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
                    {
                        transform.localPosition = Bezier.GetPoint(a, b, c, travelTime);
                        Vector3 d = Bezier.GetDerivative(a, b, c, travelTime);
                        d.y = 0f;
                        transform.localRotation = Quaternion.LookRotation(d);
                        yield return null;
                    }

                    travelTime -= 1f;
                    if (moveInput.ActionPointSpend())
                    {
                        yield return new WaitWhile(() => entity.ActionPoints == 0f || entity.SpentActionPoints > Player.Instance.SpentActionPoints);
                    }

                    if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
                    {
                        entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
                        yield return new WaitWhile(() => entity.ActionPoints == 0f);
                    }
                }

                rotationPoint = pathToTravel[i].Position;
                rotationPoint.y = transform.localPosition.y;
                transform.LookAt(rotationPoint);
                entity.Orientation = transform.localRotation.eulerAngles.y;

                //check for slope
                if (pathToTravel[i - 1].Elevation <= pathToTravel[i].Elevation - 3)
                {
                    moveInput.StartClimbing(false);

                    a = pathToTravel[i - 1].Slope ? pathToTravel[i - 1].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[i - 1].Position;
                    b = a + Vector3.up * QuadMetrics.elevationStep;
                    c = Vector3.Lerp(b, pathToTravel[i].Position, 0.5f);
                    c.y = b.y;

                    for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
                    {
                        transform.localPosition = Bezier.GetPoint(a, b, c, travelTime);
                        travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed;
                        yield return null;
                    }

                    if (moveInput.ActionPointSpend())
                    {
                        yield return new WaitWhile(() => entity.ActionPoints == 0f || entity.SpentActionPoints > Player.Instance.SpentActionPoints);
                    }

                    if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
                    {
                        entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
                        yield return new WaitWhile(() => entity.ActionPoints == 0f);
                    }

                    a = c;
                    c = pathToTravel[i].Slope ? pathToTravel[i].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[i].Position;
                    c.x = a.x;
                    c.z = a.z;

                    for(int j = 0; j < pathToTravel[i].Elevation - pathToTravel[i - 1].Elevation - 2f; j++)
                    {
                        travelTime -= 1f;
                        for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
                        {
                            transform.localPosition = Vector3.Lerp(a, c, (travelTime + j) / (pathToTravel[i].Elevation - pathToTravel[i - 1].Elevation - 2f));
                            yield return null;
                        }
                        
                        if (moveInput.ActionPointSpend())
                        {
                            yield return new WaitWhile(() => entity.ActionPoints == 0f || entity.SpentActionPoints > Player.Instance.SpentActionPoints);
                        }

                        if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
                        {
                            entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
                            yield return new WaitWhile(() => entity.ActionPoints == 0f);
                        }
                    }
                }
                else
                {
                    moveInput.StartClimbing(true);

                    a = pathToTravel[i - 1].Slope ? pathToTravel[i - 1].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[i - 1].Position;
                    c = pathToTravel[i].Slope ? pathToTravel[i].Position + Vector3.up * QuadMetrics.elevationStep / 2f : pathToTravel[i].Position;
                    c.x = c.x + (a.x - c.x) * 0.5f;
                    c.z = c.z + (a.z - c.z) * 0.5f;

                    if (pathToTravel[i - 1].Elevation < pathToTravel[i].Elevation)
                    {
                        b = a;
                        b.y = c.y;
                    }
                    else
                    {
                        b = c;
                        b.y = a.y;
                    }

                    //add additional point
                    for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
                    {
                        transform.localPosition = Bezier.GetPoint(a, b, c, travelTime);
                        Vector3 d = Bezier.GetDerivative(a, b, c, travelTime);
                        d.y = 0f;
                        transform.localRotation = Quaternion.LookRotation(d);
                        yield return null;
                    }
                }

                walkToCellCenter = true;
                travelTime -= 1f;

                if (moveInput.IterationEndActionPointSpend(pathToTravel[i]))
                {
                    yield return new WaitWhile(() => entity.ActionPoints == 0f || entity.SpentActionPoints > Player.Instance.SpentActionPoints);
                }

                if (moveInput.MoveIterationEnd())
                {
                    if (entity == Player.Instance)
                    {
                        break;
                    }
                    else
                    {
                        pathToTravel = moveInput.GetPath();
                        i = pathToTravel.Count == 2 ? 0 : i;
                    }
                }

                if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
                {
                    entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
                    yield return new WaitWhile(() => entity.ActionPoints == 0f);
                }
            }
        }

        if (walkToCellCenter)
        {
            a = c;
            b = entity.Location.Slope ? entity.Location.Position + Vector3.up * QuadMetrics.elevationStep / 2f : entity.Location.Position;
            c = b;

            for (; travelTime < 1f; travelTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, travelTime);
                Vector3 d = Bezier.GetDerivative(a, b, c, travelTime);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }

            if (moveInput.ActionPointSpend())
            {
                yield return new WaitWhile(() => entity.SpentActionPoints > Player.Instance.SpentActionPoints);
            }

            if (GameManager.Instance.stopCurrentAction && entity.ActionPoints > 0f)
            {
                entity.ActionPoints = Mathf.Min(Player.Instance.ActionPoints, entity.ActionPoints);
            }
        }

        transform.localPosition = entity.Location.Slope ? entity.Location.Position + Vector3.up * QuadMetrics.elevationStep / 2f : entity.Location.Position;
        entity.Orientation = transform.localRotation.eulerAngles.y;
        ListPool<QuadCell>.Add(pathToTravel);
        pathToTravel = null;
        
        moving = null;
        moveInput.MoveEnd();
    }
}
