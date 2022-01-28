using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{   
    private bool leaveTrail;
    private bool activate;
    private float turnsSpent;
    private float power;
    private CellHazards hazard;
    private Coroutine move;
    private Transform _transform;
    private ParticleSystem projectile;
    private GameObject explosion;
    private List<QuadCell> targetedCells;

    private bool isKilled = false;

    private void Awake()
    {
        _transform = transform;
    }

    public bool Moving
    {
        get
        {
            return moving;
        }
        set
        {
            if (value == moving || isKilled)
                return;

            moving = value;
            if (!value)
            {
                
                GameManager.Instance.CheckForActionsFinished();
                if (activate)
                {
                    GameManager.Instance.grid.ApplyCellsDamage(power, targetedCells);
                    GameManager.Instance.grid.ApplyHazardToTargetedCells(targetedCells, hazard);

                    if (explosion != null)
                    {
                        GameObject e = Instantiate(explosion);
                        e.transform.SetParent(_transform, false);

                        isKilled = true;
                        StartCoroutine(DestroyProjectile());
                    }
                    else
                    {
                        GameManager.Instance.grid.RemoveProjectile(this);
                    }                   
                }
            }
        }
    }

    private bool moving;

    public float TurnsToTravel
    {
        get
        {
            return turnsToTravel;
        }
        set
        {
            turnsToTravel = value;
            if(turnsToTravel <= 0f)
            {
                turnsSpent = 0f;
                Moving = false;
            }
        }
    }

    private float turnsToTravel;

    public void FireProjectile(bool leaveTrail, int turns, float speedMultiplier, float power, string title, CellHazards hazard, 
        List<QuadCell> targetedCells, List<Vector3> points, List<Vector3> normals)
    {
        if(points.Count == 0)
        {
            Debug.LogWarning("Points must contain at least one item");
            GameManager.Instance.grid.RemoveProjectile(this);
        }

        if(points.Count != normals.Count + 1)
        {
            Debug.LogWarning("Points count must be the same as normals count - 1");
            Debug.Log("PointsCount: " + points.Count);
            Debug.Log("NormalsCount: " + normals.Count);
            GameManager.Instance.grid.RemoveProjectile(this);
        }

        this.leaveTrail = leaveTrail;
        this.power = power;
        this.hazard = hazard;      
        this.targetedCells = targetedCells;

        explosion = Resources.Load("Abilities/Effects/" + title + "Explosion") as GameObject;

        if(points.Count > 1)
        {
            projectile = (Resources.Load("Abilities/Effects/" + title + "Projectile") as GameObject).GetComponent<ParticleSystem>();

            if (projectile == null)
            {
                Debug.Log("Abilities/Effects/" + title + "Projectile");
                Debug.LogWarning("Projectile prefab was not found");
                GameManager.Instance.grid.RemoveProjectile(this);
            }
        }

        if (move == null)
            move = StartCoroutine(MoveProjectile(speedMultiplier, turns, points, normals));
    }

    private IEnumerator MoveProjectile(float speedMultiplier, int turns, List<Vector3> points, List<Vector3> normals)
    {
        turnsSpent = 0f;
        _transform.localPosition = points[0];

        yield return new WaitUntil(() => TurnsToTravel > 0f);

        if(points.Count > 1)
        {
            ParticleSystem p = Instantiate(projectile);
            p.transform.SetParent(_transform, false);
            p.Play();

            _transform.localRotation = Quaternion.LookRotation(normals[0]);
            float moveTime;
            float step = 1f / (points.Count - 1f);
            float timeStep;
            QuadCell startCell = GameManager.Instance.grid.GetCell(points[0]);
            QuadCell currentCell = startCell;

            if (turns > 1)
            {
                moveTime = Time.deltaTime * GameManager.Instance.GameRunningSpeed;
                timeStep = 1f;
            }
            else
            {
                moveTime = Time.deltaTime * GameManager.Instance.GameRunningSpeed * speedMultiplier;
                timeStep = speedMultiplier;
            }

            float currentTurn = 1f;
            List<Entity> struckedEntities = new List<Entity>();

            for (; moveTime < turns; moveTime += Time.deltaTime * GameManager.Instance.GameRunningSpeed * timeStep)
            {
                if (leaveTrail)
                {
                    if(currentCell != startCell)
                    {
                        currentCell.CellHazard = hazard;
                        if (currentCell.Unit && !struckedEntities.Contains(currentCell.Unit))
                        {
                            currentCell.Unit.Health -= power;
                            struckedEntities.Add(currentCell.Unit);
                        }
                    }

                    currentCell = GameManager.Instance.grid.GetCell(_transform.localPosition);
                }

                int firstPoint = Mathf.FloorToInt((points.Count - 1f) * (moveTime / turns));
                int secondPoint = firstPoint + 1;
                _transform.localPosition = Vector3.Lerp(points[firstPoint], points[secondPoint], ((moveTime / turns) - step * firstPoint) / step);
                _transform.localRotation = Quaternion.LookRotation(normals[firstPoint]);

                if (moveTime > currentTurn)
                {
                    if(currentTurn + 1f == turns)
                    {
                        timeStep = speedMultiplier;
                    }

                    currentTurn++;
                    turnsSpent++;
                    TurnsToTravel--;
                    
                    yield return new WaitUntil(() => moving && turnsSpent <= Player.Instance.SpentActionPoints);

                    if (GameManager.Instance.stopCurrentAction && turnsToTravel > 0f)
                    {
                        TurnsToTravel = Mathf.Min(Player.Instance.ActionPoints, turnsToTravel);
                    }
                }

                yield return null;
            }

            Destroy(p.gameObject);
        }
        else
        {
            while (turns > 0)
            {
                turns--;
                turnsSpent++;
                TurnsToTravel--;
                
                yield return new WaitUntil(() => moving && turnsSpent <= Player.Instance.SpentActionPoints);

                if (GameManager.Instance.stopCurrentAction && turnsToTravel > 0f)
                {
                    TurnsToTravel = Mathf.Min(Player.Instance.ActionPoints, turnsToTravel);
                }
            }           
        }

        activate = true;
        Moving = false;
        move = null;
    }

    private IEnumerator DestroyProjectile()
    {
        ParticleSystem e = explosion.GetComponentInChildren<ParticleSystem>();

        yield return new WaitForSeconds(e.main.duration + e.main.startLifetimeMultiplier);

        GameManager.Instance.grid.RemoveProjectile(this);
    }
}
