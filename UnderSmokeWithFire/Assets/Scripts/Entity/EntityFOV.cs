using System.Collections;
using UnityEngine;

public class EntityFOV : MonoBehaviour
{
    [Range(0, 360)]
    public float angle;

    public LayerMask targetMask;
    public LayerMask obstructionMask;
    public Enemy enemy;

    //temp
    public Transform detectionSign;

    private Coroutine lookForPlayer;
    private int turnsToDetectPlayer = 3;
    private const float upperVisionAngle = 30f;
    private const float lowerVisionAngle = 20f;

    public bool CanSeePlayer
    {
        get
        {
            return canSeePlayer;
        }
        set
        {
            if (value == canSeePlayer)
                return;

            canSeePlayer = value;

            if(enemy.enemyTroop != null && enemy.enemyTroop.IsPlayerDetected)
            {
                detectionSign.gameObject.SetActive(false);

                if (canSeePlayer)
                {
                    enemy.CurrentBehavior = AIBehaviors.Attack;
                    enemy.enemyTroop.UnitsCanSeePlayer++;
                }
                else
                {
                    if (enemy.enemyTroop.IsReinforcements)
                    {
                        enemy.CurrentBehavior = AIBehaviors.Pursue;
                    }
                    else
                    {
                        enemy.CurrentBehavior = enemy.priorityBehaviour;
                    }

                    enemy.enemyTroop.UnitsCanSeePlayer--;
                }
            }
            else
            {
                if (canSeePlayer)
                {
                    detectionSign.gameObject.SetActive(true);
                }
                else
                {
                    CurrentDetectionTurns = 0;
                    detectionSign.gameObject.SetActive(false);
                }
            }
        }
    }

    private bool canSeePlayer;

    public int CurrentDetectionTurns
    {
        get
        {
            return currentDetectionTurns;
        }
        set
        {
            currentDetectionTurns = value;

            if(currentDetectionTurns >= turnsToDetectPlayer)
            {
                enemy.CurrentBehavior = AIBehaviors.Attack;
            }
        }
    }

    private int currentDetectionTurns;

    private void OnDestroy()
    {
        detectionSign.gameObject.SetActive(false);

        if (enemy.enemyTroop != null)
        {
            if (canSeePlayer)
            {
                enemy.enemyTroop.UnitsCanSeePlayer--;
            }
        }
    }

    public float GetAngle()
    {
        if (enemy.CurrentBehavior == AIBehaviors.Patrol)
        {
            return angle;
        }
        else
        {
            if (enemy.CurrentBehavior == AIBehaviors.Attack)
            {
                return 360f;
            }
            else
            {
                return angle * 2f;
            }
        }
    }

    public void StartLooking()
    {
        if (lookForPlayer == null)
            lookForPlayer = StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        //mb change isactonpossible specific for only move and crouch
        while (Player.Instance && GameManager.Instance.IsPlayerInAction)
        {
            QuadCell previousPlayerLocation = GameManager.Instance.grid.GetCell(Player.Instance.transform.position);
            QuadCell currentPlayerLocation = previousPlayerLocation;

            while(currentPlayerLocation == previousPlayerLocation)
            {
                yield return wait;

                currentPlayerLocation = GameManager.Instance.grid.GetCell(Player.Instance.transform.position);
            }

            FieldOfViewCheck(currentPlayerLocation);
        }

        lookForPlayer = null;
    }

    private void FieldOfViewCheck(QuadCell currentPlayerLocation)
    {
        if (enemy.Location.coordinates.DistanceTo(currentPlayerLocation.coordinates) <= enemy.VisionRange)
        {
            Transform target = Player.Instance.transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if(Vector3.Angle(transform.forward, directionToTarget) < GetAngle() / 2)
            {
                directionToTarget = (target.position - transform.position + (Player.Instance.Height - enemy.Height) * Vector3.up).normalized;

                if(Vector3.Angle(transform.up, directionToTarget) >= upperVisionAngle &&
                    Vector3.Angle(transform.up * -1f, directionToTarget) >= lowerVisionAngle)
                {
                    if (enemy.Location.coordinates.X == currentPlayerLocation.coordinates.X ||
                        enemy.Location.coordinates.Z == currentPlayerLocation.coordinates.Z)
                    {
                        QuadDirection direction = enemy.Location.coordinates.GetRelativeDirection(currentPlayerLocation.coordinates);

                        CanSeePlayer = !(!enemy.enemyTroop.IsPlayerDetected && Player.Instance.InCover && Player.Instance.CoverDirection == direction.Opposite()) && 
                            (enemy.Location.IsCellNeighbor(currentPlayerLocation) || GameManager.Instance.grid.GetCellLineVision(enemy.Location, currentPlayerLocation,
                                enemy.Location.coordinates.GetRelativeDirection(currentPlayerLocation.coordinates),
                                enemy.Location.coordinates.DistanceTo(currentPlayerLocation.coordinates), enemy.Height, Player.Instance.Height));
                    }
                    else
                    {
                        Vector3 position = new Vector3(currentPlayerLocation.Position.x - enemy.Location.Position.x, 0f, currentPlayerLocation.Position.z - enemy.Location.Position.z).normalized
                            * QuadMetrics.radius * (enemy.enemyTroop.IsPlayerDetected ? 0.65f : 0.25f);
                        position = Quaternion.Euler(0, 90f, 0) * position;

                        float targetViewElevation = currentPlayerLocation.Unit ? currentPlayerLocation.TargetViewElevation : currentPlayerLocation.TargetViewElevation + Player.Instance.Height;
                        //mb change - 0.1f to smth more accurate
                        QuadDirection direction = enemy.Location.coordinates.GetRelativeDirection(currentPlayerLocation.coordinates).Opposite();
                        
                        CanSeePlayer = !(!enemy.enemyTroop.IsPlayerDetected && Player.Instance.InCover && (Player.Instance.CoverDirection == direction.Previous() || Player.Instance.CoverDirection == direction.Next())) && 
                            GameManager.Instance.grid.GetCellAngleVision(
                                enemy.Location, currentPlayerLocation,
                                new Vector2(currentPlayerLocation.Position.x - 0.1f * QuadMetrics.radius, currentPlayerLocation.Position.z - 0.1f * QuadMetrics.radius),
                                targetViewElevation, enemy.Height) ||
                            GameManager.Instance.grid.GetCellAngleVision(
                                enemy.Location, currentPlayerLocation,
                                new Vector2(currentPlayerLocation.Position.x - position.x, currentPlayerLocation.Position.z - position.z),
                                targetViewElevation, enemy.Height) ||
                            GameManager.Instance.grid.GetCellAngleVision(
                                enemy.Location, currentPlayerLocation,
                                new Vector2(currentPlayerLocation.Position.x + position.x, currentPlayerLocation.Position.z + position.z),
                                targetViewElevation, enemy.Height);
                    }

                    return;
                }
            }
        }

        CanSeePlayer = false;
    }
}
