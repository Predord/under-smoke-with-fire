using System;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class EnemyMeleeControls : AIControls, IMoveInput, IAttackInput
{
    public int turnsToHit;

    private int searchTurnsCount;
    private SpecialZone searchZone;
    private EnemyMeleeAnimationController animationController;

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
        animationController = GetComponent<EnemyMeleeAnimationController>();
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
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            currentTurnsWaited = 0;

            if (enemy.Location.IsCellNeighbor(Player.Instance.PreviousLocation) || enemy.Location.IsCellNeighbor(Player.Instance.Location))
            {
                activeCommand = attackCommand;
            }
            else
            {
                cellToTravel = Player.Instance.Location;

                GetPath(false, false);
            }

            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Pursue)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            if(enemy.lastSeenPlayerLocation == null)
            {
                SetSearchZone(enemy.Location);

                activeCommand = noActionCommand;
                return;
            }

            if (currentTurnsWaited < pursueTurnsToWait)
            {
                activeCommand = noActionCommand;
                currentTurnsWaited++;
                return;
            }

            currentTurnsWaited = 0;

            cellToTravel = enemy.lastSeenPlayerLocation;

            GetPath(true, false);
            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Search)
        {
            if (currentTurnsWaited < searchTurnsToWait)
            {
                activeCommand = noActionCommand;
                currentTurnsWaited++;
                return;
            }

            currentTurnsWaited = 0;

            cellToTravel = searchZone.GetRandomCell(enemy.Location, true, true);
            GetPath(false, false);
            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.DefendPosition)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }

            if (enemy.AssignedDefencePositionIndex == -1)
            {
                activeCommand = noActionCommand;
                return;
            }

            if (currentTurnsWaited < patrolTurnsToWait)
            {
                activeCommand = noActionCommand;
                currentTurnsWaited++;
                return;
            }

            currentTurnsWaited = 0;

            cellToTravel = GameManager.Instance.grid.specialZones[enemy.AssignedDefencePositionIndex]
                .GetRandomCell(enemy.Location, false, true);

            lockedForCoverCell = cellToTravel;
            lockedForCoverCell.LockedForCover = true;

            GetPath(true, false);

            return;
        }
        else if(enemy.CurrentBehavior == AIBehaviors.Reinforce)
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

        if(enemy.CurrentBehavior == AIBehaviors.Patrol)
        {
            if(GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, cellToTravel, false, false, true, true))
            {                
                path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel);
                if (path.Count > 1)
                {
                    enemy.lockedCell = path[1];
                    enemy.lockedCell.LockedForTravel = true;
                }

                return true;
            }

            return false;
        }
        else if(enemy.CurrentBehavior == AIBehaviors.Attack)
        {
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }               

            cellToTravel = Player.Instance.Location;
            if (!enemy.Location.IsCellNeighbor(Player.Instance.Location) &&
                GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, cellToTravel, true, false, true))
            {
                path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel.PathFrom);
                if (path.Count > 1)
                {
                    enemy.lockedCell = path[1];
                    enemy.lockedCell.LockedForTravel = true;
                }

                return true;
            }

            return false;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Pursue)
        {
            if (GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, enemy.lastSeenPlayerLocation, true, false, true, true))
            {
                path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, enemy.lastSeenPlayerLocation.PathFrom);                
                if (path.Count > 1)
                {
                    enemy.lockedCell = path[1];
                    enemy.lockedCell.LockedForTravel = true;
                }

                return true;
            }

            return false;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Search)
        {
            searchTurnsCount--;

            if (searchTurnsCount <= 0)
            {
                return false;
            }

            if (cellToTravel == enemy.Location)
            {
                cellToTravel = searchZone.GetRandomCell(enemy.Location);
            }

            if (GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, cellToTravel, true, false, true, true))
            {
                path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel.PathFrom);
                if (path.Count > 1)
                {
                    enemy.lockedCell = path[1];
                    enemy.lockedCell.LockedForTravel = true;
                }              

                return true;
            }

            return false;       
        }
        else if (enemy.CurrentBehavior == AIBehaviors.DefendPosition || enemy.CurrentBehavior == AIBehaviors.Reinforce)
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
        if(enemy.CurrentBehavior == AIBehaviors.Patrol && enemy.Location == enemy.patrolCells[currentPatrolIndex])
        {
            currentPatrolIndex++;
            animationController.SetRunningAnimation(false);
            enemy.InAction = false;

            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Pursue)
        {
            if (enemy.lastSeenPlayerLocation)
            {
                SetSearchZone(enemy.lastSeenPlayerLocation);
            }
            else
            {
                SetSearchZone(enemy.Location);
            }
            
            animationController.SetRunningAnimation(false);
            enemy.InAction = false;

            return;
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Search && searchTurnsCount <= 0)
        {
            if (enemy.enemyTroop.IsReinforcements)
            {
                enemy.CurrentBehavior = AIBehaviors.Wait;
                enemy.enemyTroop.RefreshReinforcement();
            }
            else
            {
                currentTurnsWaited = patrolTurnsToWait;
                enemy.CurrentBehavior = AIBehaviors.DefendPosition;
            }            
        }
        else if (enemy.CurrentBehavior == AIBehaviors.DefendPosition)
        {
            /*
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }*/
        }
        else if (enemy.CurrentBehavior == AIBehaviors.Reinforce)
        {
            /*
            if (lockedForCoverCell)
            {
                lockedForCoverCell.LockedForCover = false;
                lockedForCoverCell = null;
            }*/

            enemy.AssignedDefencePositionIndex = enemy.enemyTroop.zoneIndex;
            currentTurnsWaited = patrolTurnsToWait;
            enemy.CurrentBehavior = AIBehaviors.DefendPosition;
        }

        enemy.audioController.StopAudio("Steps");
        animationController.SetRunningAnimation(false);
        enemy.InAction = false;
    }

    private void SetSearchZone(QuadCell searchCell)
    {
        enemy.CurrentBehavior = AIBehaviors.Search;
        //make size of search zone depend on map size
        if (enemy.hasSeenWherePlayerMoved)
        {
            int coordX, coordZ;
            if (enemy.lastSeenMoveDirection == QuadDirection.SouthEast || enemy.lastSeenMoveDirection == QuadDirection.NorthEast ||
                enemy.lastSeenMoveDirection == QuadDirection.East)
            {
                coordX = Math.Max(GameManager.Instance.grid.explorableCountX,
                    searchCell.coordinates.X - searchZoneSize / 6);
            }
            else if (enemy.lastSeenMoveDirection == QuadDirection.North || enemy.lastSeenMoveDirection == QuadDirection.South)
            {
                coordX = Math.Max(GameManager.Instance.grid.explorableCountX,
                    searchCell.coordinates.X - searchZoneSize / 2);
            }
            else
            {
                coordX = Math.Max(GameManager.Instance.grid.explorableCountX,
                    searchCell.coordinates.X - 5 * searchZoneSize / 6);
            }

            if (enemy.lastSeenMoveDirection == QuadDirection.North || enemy.lastSeenMoveDirection == QuadDirection.NorthEast ||
                enemy.lastSeenMoveDirection == QuadDirection.NorthWest)
            {
                coordZ = Math.Max(GameManager.Instance.grid.explorableCountZ,
                    searchCell.coordinates.Z - searchZoneSize / 6);
            }
            else if (enemy.lastSeenMoveDirection == QuadDirection.East || enemy.lastSeenMoveDirection == QuadDirection.West)
            {
                coordZ = Math.Max(GameManager.Instance.grid.explorableCountZ,
                    searchCell.coordinates.Z - searchZoneSize / 2);
            }
            else
            {
                coordZ = Math.Max(GameManager.Instance.grid.explorableCountZ,
                    searchCell.coordinates.Z - 5 * searchZoneSize / 6);
            }

            if (coordX + searchZoneSize - 1 > GameManager.Instance.grid.cellCountX - GameManager.Instance.grid.explorableCountX - 1)
            {
                searchZone.xLength = GameManager.Instance.grid.cellCountX - GameManager.Instance.grid.explorableCountX - coordX;
            }
            else
            {
                searchZone.xLength = searchZoneSize + 1;
            }

            if (coordZ + searchZoneSize - 1 > GameManager.Instance.grid.cellCountZ - GameManager.Instance.grid.explorableCountZ - 1)
            {
                searchZone.zLength = GameManager.Instance.grid.cellCountZ - GameManager.Instance.grid.explorableCountZ - coordZ;
            }
            else
            {
                searchZone.zLength = searchZoneSize + 1;
            }

            searchZone.bottomLeftCoordinates = new QuadCoordinates(coordX, coordZ);
        }
        else
        {
            searchZone.bottomLeftCoordinates = new QuadCoordinates(
                Math.Max(GameManager.Instance.grid.explorableCountX,
                    searchCell.coordinates.X - searchZoneSize / 2),
                Math.Max(GameManager.Instance.grid.explorableCountZ,
                    searchCell.coordinates.Z - searchZoneSize / 2));

            if (searchZone.bottomLeftCoordinates.X + searchZoneSize - 1 > GameManager.Instance.grid.cellCountX - GameManager.Instance.grid.explorableCountX - 1)
            {
                searchZone.xLength = GameManager.Instance.grid.cellCountX - GameManager.Instance.grid.explorableCountX - searchZone.bottomLeftCoordinates.X;
            }
            else
            {
                searchZone.xLength = searchZoneSize + 1;
            }

            if (searchZone.bottomLeftCoordinates.Z + searchZoneSize - 1 > GameManager.Instance.grid.cellCountZ - GameManager.Instance.grid.explorableCountZ - 1)
            {
                searchZone.zLength = GameManager.Instance.grid.cellCountZ - GameManager.Instance.grid.explorableCountZ - searchZone.bottomLeftCoordinates.Z;
            }
            else
            {
                searchZone.zLength = searchZoneSize + 1;
            }
        }

        searchTurnsCount = Random.Range(10, 15);
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
    //change
    public bool IsAttacking()
    {
        return enemy.Location.IsCellNeighbor(Player.Instance.PreviousLocation) || enemy.Location.IsCellNeighbor(Player.Instance.Location);
    }
}
