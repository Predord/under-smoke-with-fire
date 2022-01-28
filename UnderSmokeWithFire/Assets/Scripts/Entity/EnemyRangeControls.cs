using System.Collections.Generic;
using UnityEngine;

public class EnemyRangeControls : AIControls, IMoveInput, IAttackInput
{
    public int turnsToHit = 4;

    private bool readyToFire;
    private int turnsFindNewCover = 1;
    private EnemyRangeAnimationController animationController;

    public int TurnsToHit
    {
        get
        {
            return turnsToHit;
        }
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        activeCommand = noActionCommand;
    }

    private void Start()
    {
        animationController = GetComponent<EnemyRangeAnimationController>();
    }

    private void OnDestroy()
    {
        activeCommand.Stop();

        if (enemy.lockedCell)
        {
            enemy.lockedCell.LockedForTravel = false;
            enemy.lockedCell = null;
        }

        if (lockedForCoverCell)
        {
            lockedForCoverCell.LockedForCover = false;
            lockedForCoverCell = null;
        }

        animationController.SetDeathAnimationTrigger();
    }

    public override void SetActiveAction()
    {
        path.Clear();

        if (enemy.CurrentBehavior == AIBehaviors.Patrol)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            if (currentTurnsWaited < patrolTurnsToWait)
            {
                activeCommand = noActionCommand;
                currentTurnsWaited++;
                return;
            }

            currentTurnsWaited = 0;

            if (enemy.patrolCells.Count > 1)
            {
                if (currentPatrolIndex >= enemy.patrolCells.Count)
                {
                    enemy.patrolCells.Reverse();
                    cellToTravel = enemy.patrolCells[1];
                    currentPatrolIndex = 1;
                }
                else
                {
                    cellToTravel = enemy.patrolCells[currentPatrolIndex];
                }

                GetPath(true, true);

                return;
            }

            activeCommand = noActionCommand;
            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Attack)
        {
            currentTurnsWaited = 0;

            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            if (readyToFire)
            {
                activeCommand = attackCommand;
                return;
            }

            turnsFindNewCover = 1;         
            //add melee attack when player close
            QuadDirection playerRelativeDirection = 
                enemy.Location.coordinates.GetRelativeDirection(enemy.fov.CanSeePlayer ? Player.Instance.Location.coordinates : enemy.lastSeenPlayerLocation.coordinates);
            
            if((int)playerRelativeDirection % 2 == 0)
            {
                if (enemy.InCover && enemy.CoverDirection == playerRelativeDirection)
                {
                    activeCommand = attackCommand;
                    return;
                }
                else
                {
                    cellToTravel = GameManager.Instance.grid.FindCellWithCover(enemy, playerRelativeDirection, playerRelativeDirection, 7);
                }
            }
            else
            {
                if (enemy.InCover && enemy.CoverDirection == playerRelativeDirection.Previous() || enemy.CoverDirection == playerRelativeDirection.Next())
                {
                    activeCommand = attackCommand;
                    return;
                }
                else
                {
                    cellToTravel = GameManager.Instance.grid.FindCellWithCover(enemy, playerRelativeDirection.Previous(), playerRelativeDirection.Next(), 7);
                }              
            }

            if(enemy.Location == cellToTravel)
            {
                activeCommand = attackCommand;
                return;
            }

            lockedForCoverCell = cellToTravel;
            lockedForCoverCell.LockedForCover = true;

            GetPath(true, false);
        
            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Wait)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            activeCommand = noActionCommand;

            if (currentTurnsWaited + 1 < waitInPositionTurns)
            {
                currentTurnsWaited++;
                return;
            }

            currentTurnsWaited = 0;            

            if (enemy.enemyTroop.IsReinforcements)
            {
                enemy.enemyTroop.RefreshReinforcement();           
            }
            else
            {
                enemy.CurrentBehavior = AIBehaviors.DefendPosition;
            }

            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.DefendPosition)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            readyToFire = false;

            if (enemy.AssignedDefencePositionIndex == -1)
            {
                activeCommand = noActionCommand;
                return;
            }

            List<QuadCell> defenceCells = GameManager.Instance.grid.specialZones[enemy.AssignedDefencePositionIndex].GetSpecialZoneCells();
            QuadCell priorityCell = defenceCells[0];
            QuadDirection playerRelativeDirection = priorityCell.coordinates.GetRelativeDirection(Player.Instance.Location.coordinates);

            int currentPriorityValue = priorityCell.Elevation;
            if ((int)playerRelativeDirection % 2 == 0)
            {
                if (priorityCell.HasCoverInDirection((int)playerRelativeDirection))
                {
                    currentPriorityValue += priorityCell.IsSmallCover((int)playerRelativeDirection) ? 6 : 9;
                }
            }
            else
            {
                if (priorityCell.HasCoverInDirection((int)playerRelativeDirection.Previous()) || priorityCell.HasCoverInDirection((int)playerRelativeDirection.Next()))
                {
                    currentPriorityValue += priorityCell.IsSmallCover((int)playerRelativeDirection.Previous()) &&
                        priorityCell.IsSmallCover((int)playerRelativeDirection.Next()) ? 6 : 9;
                }
            }

            for (int i = 1; i < defenceCells.Count; i++)
            {
                if (defenceCells[i].LockedForCover)
                {
                    continue;
                }

                playerRelativeDirection = defenceCells[i].coordinates.GetRelativeDirection(Player.Instance.Location.coordinates);
                int priorityValue = defenceCells[i].Elevation;
                if ((int)playerRelativeDirection % 2 == 0)
                {
                    if (defenceCells[i].HasCoverInDirection((int)playerRelativeDirection))
                    {
                        priorityValue += defenceCells[i].IsSmallCover((int)playerRelativeDirection) ? 6 : 9;
                    }
                }
                else
                {
                    if (defenceCells[i].HasCoverInDirection((int)playerRelativeDirection.Previous()) || defenceCells[i].HasCoverInDirection((int)playerRelativeDirection.Next()))
                    {
                        priorityValue += defenceCells[i].IsSmallCover((int)playerRelativeDirection.Previous()) &&
                            defenceCells[i].IsSmallCover((int)playerRelativeDirection.Next()) ? 6 : 9;
                    }
                }

                if (priorityValue > currentPriorityValue)
                {
                    currentPriorityValue = priorityValue;
                    priorityCell = defenceCells[i];
                }
            }

            if (priorityCell.LockedForCover)
            {
                activeCommand = noActionCommand;
                return;
            }

            cellToTravel = priorityCell;

            if (enemy.Location == cellToTravel)
            {
                activeCommand = noActionCommand;
                return;
            }

            lockedForCoverCell = cellToTravel;
            lockedForCoverCell.LockedForCover = true;

            GetPath(true, false);

            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Reinforce)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            if (currentTurnsWaited < pursueTurnsToWait)
            {
                activeCommand = noActionCommand;
                currentTurnsWaited++;
                return;
            }

            currentTurnsWaited = 0;

            cellToTravel = GameManager.Instance.grid.specialZones[enemy.enemyTroop.zoneIndex]
                .GetRandomCell(enemy.Location, false, true);

            lockedForCoverCell = cellToTravel;
            lockedForCoverCell.LockedForCover = true;

            GetPath(true, false);

            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Pursue)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            QuadDirection playerRelativeDirection =
                enemy.Location.coordinates.GetRelativeDirection(enemy.fov.CanSeePlayer ? Player.Instance.Location.coordinates : enemy.lastSeenPlayerLocation ? enemy.lastSeenPlayerLocation.coordinates : enemy.Location.coordinates);

            if ((int)playerRelativeDirection % 2 == 0)
            {
                cellToTravel = GameManager.Instance.grid.FindCellWithCover(enemy, playerRelativeDirection, playerRelativeDirection, 7);
            }
            else
            {
                cellToTravel = GameManager.Instance.grid.FindCellWithCover(enemy, playerRelativeDirection.Previous(), playerRelativeDirection.Next(), 7);
            }

            if (enemy.Location == cellToTravel)
            {
                if (enemy.fov.CanSeePlayer)
                {
                    activeCommand = attackCommand;
                }
                else
                {
                    activeCommand = noActionCommand;
                    enemy.CurrentBehavior = AIBehaviors.Wait;
                    enemy.enemyTroop.RefreshReinforcement();
                }
                
                return;
            }

            lockedForCoverCell = cellToTravel;
            lockedForCoverCell.LockedForCover = true;

            GetPath(true, false);

            return;
        }

        activeCommand = noActionCommand;
        return;
    }

    public List<QuadCell> GetPath()
    {
        return path;
    }

    public void MoveStart(QuadCell endCell)
    {
        enemy.audioController.PlayAudio("Steps");
        animationController.SetRunningAnimation(true);
        previousCellsVisibility = enemy.Location.IsVisible;
        previousCell = enemy.Location;

        if (enemy.Location.IsVisible && (enemy.Location.coordinates.DistanceTo(Player.Instance.Location.coordinates) <= Player.Instance.VisionRange))
        {
            if (enemy.Location.coordinates.X == Player.Instance.Location.coordinates.X ||
                enemy.Location.coordinates.Z == Player.Instance.Location.coordinates.Z)
            {
                previousCellsVisibility = Player.Instance.Location.IsCellNeighbor(enemy.Location) ||
                    GameManager.Instance.grid.GetCellLineVision(Player.Instance.Location, enemy.Location,
                        Player.Instance.Location.coordinates.GetRelativeDirection(enemy.Location.coordinates),
                        Player.Instance.Location.coordinates.DistanceTo(enemy.Location.coordinates), Player.Instance.Height, enemy.Height);
            }
            else
            {
                Vector3 position = new Vector3(enemy.Location.Position.x - Player.Instance.Location.Position.x, 0f, enemy.Location.Position.z - Player.Instance.Location.Position.z).normalized
                    * QuadMetrics.radius * 0.98f;
                position = Quaternion.Euler(0, 90f, 0) * position;

                previousCellsVisibility = GameManager.Instance.grid.GetCellAngleVision(Player.Instance.Location, enemy.Location,
                        new Vector2(enemy.Location.Position.x - 0.1f * QuadMetrics.radius, enemy.Location.Position.z - 0.1f * QuadMetrics.radius),
                        enemy.Location.TargetViewElevation, Player.Instance.Height) ||
                    GameManager.Instance.grid.GetCellAngleVision(Player.Instance.Location, enemy.Location,
                        new Vector2(enemy.Location.Position.x - position.x, enemy.Location.Position.z - position.z),
                        enemy.Location.TargetViewElevation, Player.Instance.Height) ||
                    GameManager.Instance.grid.GetCellAngleVision(Player.Instance.Location, enemy.Location,
                        new Vector2(enemy.Location.Position.x + position.x, enemy.Location.Position.z + position.z),
                        enemy.Location.TargetViewElevation, Player.Instance.Height);
            }
        }

        enemy.SetPositionForMove(endCell);
        enemy.InAction = true;
    }

    public bool MoveIterationEnd()
    {
        previousCell.IsVisible = previousCellsVisibility;
        previousCellsVisibility = enemy.Location.IsVisible;
        GameManager.Instance.grid.CheckCellVisible(enemy.Location, enemy.Height);
        enemy.body.gameObject.SetActive(enemy.Location.IsVisible);
        previousCell = enemy.Location;

        if(enemy.CurrentBehavior == AIBehaviors.Attack)
        {
            if (GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, cellToTravel, true, false, true))
            {
                if (cellToTravel.Unit && cellToTravel.Unit != enemy)
                {
                    path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel.PathFrom);
                }
                else
                {
                    path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel);
                }

                if (path.Count > 1)
                {
                    if(path[1] == cellToTravel && path[1].Unit != null)
                    {
                        if(turnsFindNewCover > 0)
                        {
                            turnsFindNewCover--;

                            QuadDirection playerRelativeDirection = 
                                enemy.Location.coordinates.GetRelativeDirection(enemy.fov.CanSeePlayer ? Player.Instance.Location.coordinates : enemy.lastSeenPlayerLocation.coordinates);

                            if ((int)playerRelativeDirection % 2 == 0)
                            {
                                if (enemy.InCover && enemy.CoverDirection == playerRelativeDirection)
                                {
                                    readyToFire = true;
                                    return false;
                                }
                                else
                                {
                                    if (lockedForCoverCell)
                                        lockedForCoverCell.LockedForCover = false;

                                    cellToTravel = GameManager.Instance.grid.FindCellWithCover(enemy, playerRelativeDirection, playerRelativeDirection, 7);

                                    lockedForCoverCell = cellToTravel;
                                    lockedForCoverCell.LockedForCover = true;
                                }
                            }
                            else
                            {
                                if (enemy.InCover && enemy.CoverDirection == playerRelativeDirection.Previous() || enemy.CoverDirection == playerRelativeDirection.Next())
                                {
                                    readyToFire = true;
                                    return false;
                                }
                                else
                                {
                                    if (lockedForCoverCell)
                                        lockedForCoverCell.LockedForCover = false;

                                    cellToTravel = GameManager.Instance.grid.FindCellWithCover(enemy, playerRelativeDirection.Previous(), playerRelativeDirection.Next(), 7);
                                    
                                    lockedForCoverCell = cellToTravel;
                                    lockedForCoverCell.LockedForCover = true;
                                }
                            }

                            if(GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, cellToTravel, true, false, true))
                            {
                                path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel.PathFrom);
                            }
                        }
                        else
                        {
                            readyToFire = true;
                            return false;
                        }
                    }
                    enemy.lockedCell = path[1];
                    enemy.lockedCell.LockedForTravel = true;
                    return true;
                }
                else
                {
                    readyToFire = true;
                    return true;
                }
            }

            readyToFire = true;
            return false;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.DefendPosition || enemy.CurrentBehavior == AIBehaviors.Reinforce || enemy.CurrentBehavior == AIBehaviors.Pursue)
        {
            if (GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, cellToTravel, true, false, true))
            {
                if (cellToTravel.Unit && cellToTravel.Unit != enemy)
                {
                    path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel.PathFrom);
                }
                else
                {
                    path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel);
                }

                if (path.Count > 1)
                {
                    if (path[1] == cellToTravel && path[1].Unit != null)
                    {
                        return false;
                    }

                    enemy.lockedCell = path[1];
                    enemy.lockedCell.LockedForTravel = true;
                }

                return true;
            }

            return false;
        }

        return false;
    }

    public void StartClimbing(bool isSmallCliff)
    {
        animationController.SetClimbingAnimation(isSmallCliff);
    }

    public bool ActionPointSpend()
    {
        enemy.SpentActionPoints++;
        enemy.ActionPoints -= 1f;

        return enemy.ActionPoints == 0f || enemy.SpentActionPoints > Player.Instance.SpentActionPoints;
    }

    public bool IterationEndActionPointSpend(QuadCell currentCell)
    {
        enemy.SpentActionPoints++;
        enemy.SetPositionForMove(currentCell);
        enemy.ActionPoints -= 1f;

        return enemy.ActionPoints == 0f || enemy.SpentActionPoints > Player.Instance.SpentActionPoints;
    }

    public void MoveEnd()
    {
        if (enemy.CurrentBehavior == AIBehaviors.Reinforce)
        {
            enemy.AssignedDefencePositionIndex = enemy.enemyTroop.zoneIndex;
            currentTurnsWaited = patrolTurnsToWait;
            enemy.CurrentBehavior = AIBehaviors.DefendPosition;
        }
        else if(enemy.CurrentBehavior == AIBehaviors.Pursue)
        {
            enemy.CurrentBehavior = AIBehaviors.Wait;
            enemy.enemyTroop.RefreshReinforcement();
        }

        enemy.audioController.StopAudio("Steps");
        animationController.SetRunningAnimation(false);

        enemy.GetCover();
        enemy.InAction = false;
    }

    public Entity GetTarget()
    {
        animationController.SetAttackAnimationTrigger();

        enemy.InAction = true;
        return Player.Instance;
    }

    public void InflicteDamage(Entity target)
    {
        Player.Instance.GetHit(enemy.Damage);
    }
    //add range
    public bool IsAttacking()
    {
        return enemy.fov.CanSeePlayer;
    }
}
 