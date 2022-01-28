using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Enemy : Entity
{
    public float maxHealth = 150f; 

    public bool hasSeenWherePlayerMoved;  
    public Transform body;
    public EntityFOV fov;
    public EnemyTroop enemyTroop;

    public AIBehaviors priorityBehaviour;
    public QuadDirection lastSeenMoveDirection;
    public QuadCell lastSeenPlayerLocation;
    public QuadCell lockedCell;
    public List<QuadCell> patrolCells = new List<QuadCell>();

    public AIControls controls;

    [SerializeField]
    private float normalDamage;

    private EnemyAnimationController animationController;

    protected override void Awake()
    {
        base.Awake();
        //temp
        CurrentBehavior = AIBehaviors.Patrol;
    }

    private void Start()
    {
        animationController = GetComponent<EnemyAnimationController>();

        body.gameObject.SetActive(!GameManager.Instance.enabled || location.IsVisible);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if(Player.Instance != null)
        {
            Player.Instance.playerInput.OnLocationChange += HandlePlayerNewLocation;
            Player.Instance.OnLookAroundCorner += HandlePlayerNewLocation;
        }
    }

    private void OnDisable()
    {
        if (Player.Instance != null)
        {
            Player.Instance.playerInput.OnLocationChange -= HandlePlayerNewLocation;
            Player.Instance.OnLookAroundCorner -= HandlePlayerNewLocation;
        }
    }

    public override float ActionPoints
    {
        get
        {
            return base.ActionPoints;
        }
        set
        {
            base.ActionPoints = value;
            if(IsEndedTurn)
            {
                audioController.PauseAllAudio();
                animationController.PauseAnimations();
                IsActionPossible = false;
            }
        }
    }

    public override float SpentActionPoints
    {
        get
        {
            return base.SpentActionPoints;
        }
        set
        {
            base.SpentActionPoints = value;

            if (fov.CanSeePlayer && (enemyTroop == null || !enemyTroop.IsPlayerDetected))
            {
                fov.CurrentDetectionTurns++;
            }
        }
    }

    public override bool InAction
    {
        get
        {
            return base.InAction;
        }
        set
        {
            base.InAction = value;
            if(!value && !IsEndedTurn)
            {
                controls.SetActiveAction();
                if (GameManager.Instance.enabled && controls.activeCommand != null)
                {
                    controls.activeCommand?.Execute();
                }
            }
        }
    }

    public override bool IsActionPossible
    {
        get
        {
            return base.IsActionPossible;
        }
        set
        {
            base.IsActionPossible = value;
            if (value)
            {
                audioController.UnPauseAllAudio();
                animationController.ResumeAnimations();
                StartLookingForPlayer();
                //InAction = false;
            }
            else
            {
                spentActionPoints = 0f;
                GameManager.Instance.CheckForActionsFinished();
            }
        }
    }

    public bool IsEndedTurn
    {
        get
        {
            return actionPoints <= 0f;
        }
    }

    public int AssignedDefencePositionIndex
    {
        get
        {
            return assignedDefencePositionIndex;
        }
        set
        {
            if (assignedDefencePositionIndex == value)
                return;

            if (assignedDefencePositionIndex != -1 && value != -1)
            {
                GameManager.Instance.grid.camps.Find(x => x.zoneIndex == assignedDefencePositionIndex).campTroop.units.Remove(this);
            }

            assignedDefencePositionIndex = value;

            if(assignedDefencePositionIndex != -1)
            {
                GameManager.Instance.grid.camps.Find(x => x.zoneIndex == assignedDefencePositionIndex).campTroop.units.Add(this);
                enemyTroop = GameManager.Instance.grid.camps.Find(x => x.zoneIndex == assignedDefencePositionIndex).campTroop;
            }
            else
            {
                enemyTroop = null;
            }
        }
    }

    private int assignedDefencePositionIndex = -1;

    public float Damage
    {
        get
        {
            return normalDamage;
        }
    }

    public AIBehaviors CurrentBehavior
    {
        get
        {
            return currentBehavior;
        }
        set
        {
            if (currentBehavior == value)
                return;

            ChangeDetectionCount(currentBehavior, value);

            if (value == AIBehaviors.DefendPosition || value == AIBehaviors.Reinforce)
            {
                fov.CurrentDetectionTurns = 0;
                Player.Instance.IsDetected = false;
            }
           
            if(enemyTroop != null)
            {
                if(value == AIBehaviors.Attack)
                {
                    currentBehavior = value;
                    enemyTroop.IsPlayerDetected = true;
                    return;
                }       
            }

            currentBehavior = value;
        }
    }

    private AIBehaviors currentBehavior;

    private void ChangeDetectionCount(AIBehaviors prevBehaviour, AIBehaviors newBehaviour)
    {
        if((prevBehaviour == AIBehaviors.Attack || prevBehaviour == AIBehaviors.Pursue || prevBehaviour == AIBehaviors.Search) &&
            newBehaviour != AIBehaviors.Attack && newBehaviour != AIBehaviors.Pursue)
        {
            Player.Instance.EnemyDetectionCount--;
            return;
        }

        if ((newBehaviour == AIBehaviors.Attack || newBehaviour == AIBehaviors.Pursue) &&
            prevBehaviour != AIBehaviors.Attack && prevBehaviour != AIBehaviors.Pursue && prevBehaviour != AIBehaviors.Search)
        {
            Player.Instance.EnemyDetectionCount++;
            return;
        }
    }

    public void StartLookingForPlayer()
    {
        fov.StartLooking();
    }

    public override float GetMaxHealth()
    {
        return maxHealth;
    }

    public override void HandlePlayerNewLocation()
    {
        body.gameObject.SetActive(location.IsVisible);
    }

    public override void HandleEditMode(bool isEnabled)
    {
        if (isEnabled)
        {
            body.gameObject.SetActive(true);
        }
        else
        {
            body.gameObject.SetActive(location.IsVisible);
        }
    }

    public override void SetPositionForMove(QuadCell endCell)
    {
        if (lockedCell)
        {
            lockedCell.LockedForTravel = false;
            lockedCell = null;
        }
        base.SetPositionForMove(endCell);
    }

    public void SetNewAction()
    {
        if (!InAction)
        {
            controls.SetActiveAction();
            if (GameManager.Instance.enabled && controls.activeCommand != null)
            {
                controls.activeCommand?.Execute();
            }
        }
    }

    public override void GetCover()
    {
        if (location.HasCover())
        {
            if (location.HasOnlyOneCover(out int covers))
            {
                QuadDirection direction = QuadDirection.North;
                for (int i = 0; i < 4; i++, direction = direction.Next2())
                {
                    if ((covers & (1 << i)) != 0)
                    {
                        coverDirection = direction;
                        break;
                    }
                }
            }
            else
            {
                int k = 0;
                Dictionary<QuadDirection, int> directions = new Dictionary<QuadDirection, int>();

                for (QuadDirection i = QuadDirection.North; k < 4; k++, i = i.Next2())
                {
                    if ((covers & (1 << k)) == 0)
                        continue;

                    directions.Add(i, 0);

                    if (location.IsSmallCover(k) ||
                       (location.GetNeighbor(i.Previous2()).Elevation < location.Elevation + 2 &&
                        location.GetNeighbor(i.Previous()).Elevation < location.Elevation + 2))
                    {
                        directions[i] += 10;
                    }

                    if (i == QuadDirection.West)
                        break;
                }

                QuadDirection direction = location.coordinates.GetRelativeDirection(Player.Instance.Location.coordinates);
                if ((int)direction % 2 == 0)
                {
                    if (directions.ContainsKey(direction))
                    {
                        directions[direction] += 25;
                    }
                }
                else
                {
                    if (directions.ContainsKey(direction.Previous()))
                    {
                        float angleCoefficient = Vector3.Angle(
                            QuadMetrics.GetFirstCorner(direction.Previous()) / QuadMetrics.radius,
                            new Vector3(
                                Player.Instance.Location.Position.x - location.Position.x, 0f,
                                Player.Instance.Location.Position.z - location.Position.z));

                        angleCoefficient = angleCoefficient < 45 ? 1f : 45f / angleCoefficient;
                        directions[direction.Previous()] += (int)(25f * angleCoefficient);
                    }
                    if (directions.ContainsKey(direction.Next()))
                    {
                        float angleCoefficient = Vector3.Angle(
                            QuadMetrics.GetFirstCorner(direction.Next()) / QuadMetrics.radius,
                            new Vector3(
                                Player.Instance.Location.Position.x - location.Position.x, 0f,
                                Player.Instance.Location.Position.z - location.Position.z));

                        angleCoefficient = angleCoefficient < 45 ? 1f : 45f / angleCoefficient;
                        directions[direction.Next()] += (int)(25f * angleCoefficient);
                    }
                }

                k = directions.First().Value;
                foreach (int value in directions.Values)
                {
                    if (k > value)
                        k = value;
                }

                coverDirection = directions.FirstOrDefault(x => x.Value == k).Key;
            }

            InCover = true;
        }
    }

    public void SetNewPatrolRoute(List<QuadCell> cells)
    {
        patrolCells.Clear();
        patrolCells.AddRange(cells);      
    }

    public void ShowHidePatrolRoute(bool showRoute)
    {
        //draw line
        for (int i = 0; i < patrolCells.Count - 1; i++)
        {
            List<QuadCell> path;
            if (GameManager.Instance.grid.FindDistanceHeuristic(this, patrolCells[i], patrolCells[i + 1]))
            {
                path = GameManager.Instance.grid.FindPathClear(patrolCells[i], patrolCells[i + 1]);
                for (int j = 0; j < path.Count; j++)
                {
                    if (showRoute)
                    {
                        path[j].EnableHighlight(Color.Lerp(Color.white, Color.black, j / (path.Count - 1f)));
                    }
                    else
                    {
                        path[j].DisableHighlight();
                    }
                }
            }
            else
            {
                Debug.LogWarning("Impossible to reach location");
                return;
            }
        }
    }

    public override void SpecificsSave(BinaryWriter writer)
    {
        writer.Write((byte)patrolCells.Count);
        for(int i = 0; i < patrolCells.Count; i++)
        {
            patrolCells[i].coordinates.Save(writer);
        }

        writer.Write((byte)priorityBehaviour);
        writer.Write((byte)assignedDefencePositionIndex);
    }

    public override void SpecificsLoad(BinaryReader reader, int header)
    {
        int patrolCellsCount = reader.ReadByte();

        for (int i = 0; i < patrolCellsCount; i++)
        {
            patrolCells.Add(GameManager.Instance.grid.GetCell(QuadCoordinates.Load(reader)));
        }

        priorityBehaviour = (AIBehaviors)reader.ReadByte();

        int zoneIndex = reader.ReadByte();
        if(zoneIndex != 255)
        {
            AssignedDefencePositionIndex = zoneIndex;
        }
    }

    public static void SpecificsLoadEmpty(BinaryReader reader, int header)
    {
        int patrolCellsCount = reader.ReadByte();

        for (int i = 0; i < patrolCellsCount; i++)
        {
            QuadCoordinates.Load(reader);
        }

        reader.ReadByte();
        reader.ReadByte();
    }

    public override void Die()
    {
        if (this == ObjectiveManager.Instance.entityToKill)
        {
            ObjectiveManager.Instance.IsEntityToKillDead = true;
        }

        if (lockedCell)
            lockedCell.LockedForTravel = false;

        location.Unit = null;

        if (GameManager.Instance.enabled)
        {
            if (enemyTroop != null)
            {
                if (enemyTroop.IsPlayerDetected)
                {
                    ChangeDetectionCount(currentBehavior, AIBehaviors.Wait);
                    Player.Instance.IsDetected = false;
                }

                if (!enemyTroop.IsReinforcements)
                {
                    EnemyCamp camp = GameManager.Instance.grid.camps.Find(x => x.zoneIndex == assignedDefencePositionIndex);
                    camp.UnderAttack = true;
                    camp.TurnsForAlert += 5;
                }
            }

            enemyTroop.units.Remove(this);
            Destroy(fov);
            enemyTroop = null;
            Destroy(controls);

            GameManager.Instance.grid.units.Remove(this);
            GameManager.Instance.CheckForActionsFinished();
            
            Destroy(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
