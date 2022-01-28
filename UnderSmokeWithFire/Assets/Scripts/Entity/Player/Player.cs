using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : Entity
{
    public static object _lock = new object();
    public int dodge;
    public int currentSpentStatPoints;
    public event Action OnFatigueChange;
    public event Action OnSpentActionPoint;
    public event Action OnLookAroundCorner;
    public PlayerInput playerInput;
    public Dictionary<Ability, float> activeAbilitiesCooldowns = new Dictionary<Ability, float>();

    [SerializeField]
    private FloatingTextHandler floatingTextPrefab;

    private bool isDead;
    private const int turnsForDodgeRecover = 5;
    private const int turnsForFatigue = 10;

    private static Player instance;

    private PlayerAnimationController animationController;

    private List<QuadCell> ignoreBlockCells = new List<QuadCell>();
    private List<CellTrap> traps = new List<CellTrap>();

    public int EnemyDetectionCount { get; set; }

    public static Player Instance
    {
        get
        {
            object @lock = _lock;
            Player instance;
            lock (@lock)
            {
                instance = Player.instance;
            }
            return instance;
        }
    }

    public override QuadCell Location
    {
        get
        {
            return location;
        }
        set
        {
            if (location)
            {
                location.Unit = null;
            }
            location = value;
            value.Unit = this;

            if (location == ObjectiveManager.Instance.cellWithIntel && !ObjectiveManager.Instance.IsIntelCollected)
                ActionUI.Instance.pickUpIntelObjective.gameObject.SetActive(true);

            if(location.IsCellNeighbor(ObjectiveManager.Instance.cellToDestroy) && !ObjectiveManager.Instance.IsTargetCellDestroyed)
                ActionUI.Instance.burnIntelObjective.gameObject.SetActive(true);

            if (location.Slope)
            {
                transform.localPosition = value.Position + Vector3.up * QuadMetrics.elevationStep / 2f;
            }
            else
            {
                transform.localPosition = value.Position;
            }
        }
    }

    public QuadCell PreviousLocation
    {
        get
        {
            if (previousLocation)
                return previousLocation;

            return location;
        }
        set
        {
            previousLocation = value;
        }
    }

    private QuadCell previousLocation;

    public bool IsDetected
    {
        get
        {
            return isDetected;
        }
        set
        {
            if (isDetected == value || (!value && EnemyDetectionCount != 0))
                return;

            isDetected = value;

            animationController.SetDetection(isDetected);

            if (isDetected)
            {
                AudioManager.Instance.StopAudio(true, "UndetectedTheme", "BattleTheme");
                ActivePoseState = PoseState.Stand;
            }
            else
            {
                AudioManager.Instance.StopAudio(true, "BattleTheme", "UndetectedTheme");
                ActivePoseState = PoseState.ForcedCrouch;         
            }
        }
    }

    private bool isDetected;

    public override bool InAction
    {
        get
        {
            return base.InAction;
        }
        set
        {
            base.InAction = value;
            if (value)
            {
                IsActionPossible = false;
                GameManager.Instance.IsPlayerInAction = true;
            }
            else
            {
                ActionPoints = 0f;
                GameManager.Instance.CheckForActionsFinished();
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
                if (Instance != null && Location != null)
                {
                    spentActionPoints = 0f;
                    GameManager.Instance.stopCurrentAction = false;

                    if (!isDead)
                    {
                        GameManager.Instance.grid.FindPlayerDistance();
                    }                 
                }
                else
                {
                    return;
                }
            }
        }
    }

    public override float VisionRange
    {
        get
        {
            float sight = PlayerInfo.GetSight();

            foreach (var buffDebuff in characterActionMapBuffsDebuffs.Keys.ToList())
            {
                if (buffDebuff.sightRaw != -1)
                {
                    return buffDebuff.sightRaw;
                }

                sight += buffDebuff.sightModifier;
            }

            return sight;
        }
    }

    public float Fatigue
    {
        get
        {
            return fatigue;
        }
        set
        {
            if (value == fatigue)
                return;

            value = Mathf.Clamp(value, 0f, StatConstants.normalMaxFatigue);
            fatigue = value;

            if (fatigue == StatConstants.normalMaxFatigue)
                Health--;

            int originalSight = PlayerInfo.GetSight();
            PlayerInfo.Fatigue = fatigue;
            if(originalSight != PlayerInfo.GetSight())
            {
                GameManager.Instance.grid.ClearVision();
                GameManager.Instance.grid.SetVisibleCells(Location, VisionRange);

                if (InCover)
                {
                    LookAroundCorner(onCoverLookRight, false);
                    ClearCornerVision(!onCoverLookRight);
                }          
            }

            OnFatigueChange?.Invoke();
        }
    }

    private float fatigue;

    private int FatigueTurns
    {
        get
        {
            return fatigueTurns;
        }
        set
        {
            fatigueTurns = value;

            if(fatigueTurns >= turnsForFatigue)
            {
                fatigueTurns = 0;
                Fatigue += 1;
            }
        }
    }

    private int fatigueTurns;

    public int DodgeRecover
    {
        get
        {
            return dodgeRecover;
        }
        set
        {
            if(value >= turnsForDodgeRecover)
            {
                dodgeRecover = 0;
                dodge++;
            }
            else
            {
                if(dodge >= PlayerInfo.GetDodge())
                {
                    dodgeRecover = 0;
                }
                else
                {
                    dodgeRecover = value;
                }
            }
        }
    }

    private int dodgeRecover;

    public override float SpentActionPoints
    {
        get => base.SpentActionPoints;
        set
        {
            base.SpentActionPoints = value;

            GameManager.Instance.grid.RemoveTurnFromCellsWithHazards();

            for(int i = 0; i < traps.Count; i++)
            {
                if (traps[i].CheckForEntity())
                {
                    if(traps[i].explosion != null)
                    {
                        StartCoroutine(PlayTrap(i));
                    }
                    else
                    {
                        traps.Remove(traps[i]);
                    }                                    
                }
            }

            foreach (var ability in activeAbilitiesCooldowns.Keys.ToList())
            {
                activeAbilitiesCooldowns[ability] = Mathf.Max(0f, activeAbilitiesCooldowns[ability] - 1f);
                if (GameUI.Instance)
                {
                    GameUI.Instance.SetCoolDownToAbility(activeAbilitiesCooldowns[ability] / ability.GetStatValue(AbilityStats.Cooldown), ability);
                }
            }

            FatigueTurns++;
            DodgeRecover++;

            OnSpentActionPoint?.Invoke();
        }
    }

    public override PoseState ActivePoseState
    {
        get => base.ActivePoseState;
        set
        {
            if (value == activePoseState && InAction)
                return;

            base.ActivePoseState = value;
            //GameManager.Instance.grid.SetVisibleCells(location, VisionRange);
        }
    }

    public override bool InCover 
    { 
        get => base.InCover;
        set 
        {

            if (!value && value == inCover)
                return;

            base.InCover = value;

            if (value)
            {
                ActionUI.Instance.lookAroundCornerLeft.interactable = true;
                ActionUI.Instance.lookAroundCornerRight.interactable = true;

                QuadCell diagonalCell = location.GetNeighbor(coverDirection.Next());

                if (diagonalCell.Elevation + (diagonalCell.IsSpecial ? diagonalCell.SpecialBlockElevation : 0) - location.Elevation >= 3)
                {
                    onCoverLookRight = true;
                    LookAroundCorner(false);
                    ClearCornerVision(true);
                }
                else
                {
                    onCoverLookRight = false;
                    LookAroundCorner(true);
                    ClearCornerVision(false);
                }

                animationController.SetCoverAnimation(activePoseState == PoseState.CrouchInCover ? 2 : 1);
            }
            else
            {
                ActionUI.Instance.lookAroundCornerLeft.interactable = false;
                ActionUI.Instance.lookAroundCornerRight.interactable = false;

                foreach (var cell in ignoreBlockCells)
                {
                    cell.isBlockingViewIgnored = false;
                }
                ignoreBlockCells.Clear();
                //GameManager.Instance.grid.SetVisibleCells(location, VisionRange);

                animationController.SetCoverAnimation(0);
            }
        }
    }

    public void RegisterMe()
    {
        if (Instance == null)
        {
            instance = GetComponent<Player>();
            playerInput = GetComponent<PlayerInput>();
        }
    }

    protected override void Awake()
    {
        RegisterMe();
        base.Awake();
        GameManager.Instance.IsPlayerInAction = false;
       
        PlayerInfo.GetPlayersInfo();
        AssignUIElements();
    }

    private void Start()
    {
        animationController = GetComponent<PlayerAnimationController>();
        ActionUI.Instance.BindPlayer();
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        //change to enemy
        foreach(Entity entity in GameManager.Instance.grid.units.Where(x => x.index != 0))
        {
            playerInput.OnLocationChange += entity.HandlePlayerNewLocation;
            OnLookAroundCorner += entity.HandlePlayerNewLocation;
        }
    }

    private void OnDisable()
    {
        foreach (Entity entity in GameManager.Instance.grid.units.Where(x => x.index != 0))
        {
            playerInput.OnLocationChange -= entity.HandlePlayerNewLocation;
            OnLookAroundCorner -= entity.HandlePlayerNewLocation;
        }
    }

    private void OnDestroy()
    {
        PlayerInfo.Health = Health;
        PlayerInfo.Fatigue = Fatigue;
    }

    public override float GetMaxHealth()
    {
        return PlayerInfo.GetMaxHealth();
    }

    public override void SetPositionForMove(QuadCell endCell)
    {
        endCell.LockedForTravel = false;
        previousLocation = location;
        base.SetPositionForMove(endCell);
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
                        CoverDirection = direction;
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

                if (GameManager.Instance.grid.units.Count > 1)
                {
                    foreach (Entity enemy in GameManager.Instance.grid.units.Where(x => x.index != 0 && x.Location.IsVisible))
                    {
                        QuadDirection direction = location.coordinates.GetRelativeDirection(enemy.Location.coordinates);
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
                                        enemy.Location.Position.x - location.Position.x, 0f,
                                        enemy.Location.Position.z - location.Position.z));

                                angleCoefficient = angleCoefficient < 45 ? 1f : 45f / angleCoefficient;
                                directions[direction.Previous()] += (int)(25f * angleCoefficient);
                            }
                            if (directions.ContainsKey(direction.Next()))
                            {
                                float angleCoefficient = Vector3.Angle(
                                    QuadMetrics.GetFirstCorner(direction.Next()) / QuadMetrics.radius,
                                    new Vector3(
                                        enemy.Location.Position.x - location.Position.x, 0f,
                                        enemy.Location.Position.z - location.Position.z));

                                angleCoefficient = angleCoefficient < 45 ? 1f : 45f / angleCoefficient;
                                directions[direction.Next()] += (int)(25f * angleCoefficient);
                            }
                        }
                    }
                }

                k = directions.First().Value;
                foreach (int value in directions.Values)
                {
                    if (k > value)
                        k = value;
                }

                CoverDirection = directions.FirstOrDefault(x => x.Value == k).Key;
            }

            animationController.SetCoverAnimation(activePoseState == PoseState.CrouchInCover ? 2 : 1);
            InCover = true;
        }
    }

    public override void OnBuffDebuffTurn(BuffDebuffActionMap buffDebuff)
    {
        ActionUI.Instance.actionMapBuffsDebuffs.OnBuffDebuffChange(characterActionMapBuffsDebuffs[buffDebuff], buffDebuff);

        Health -= buffDebuff.damageOverTime * (1f - PlayerInfo.GetResists()[buffDebuff.damageType]);

        if (characterActionMapBuffsDebuffs[buffDebuff] <= 0)
        {
            characterActionMapBuffsDebuffs.Remove(buffDebuff);
        }
    }

    public override void AddBuffDebuff(BuffDebuffActionMap buffDebuff)
    {
        base.AddBuffDebuff(buffDebuff);

        ActionUI.Instance.actionMapBuffsDebuffs.OnBuffDebuffChange(buffDebuff.turnsAmount, buffDebuff);
    }

    public void AddTrap(int duration, float trapDamage, string title, CellHazards hazard, QuadCell mainCell, List<QuadCell> targetedCells)
    {
        traps.Add(new CellTrap(duration, trapDamage, title, hazard, mainCell, targetedCells));
    }

    private IEnumerator PlayTrap(int index)
    {
        GameObject e = Instantiate(traps[index].explosion);
        e.transform.SetParent(traps[index].mainCell.transform, false);

        ParticleSystem eSystem = e.GetComponentInChildren<ParticleSystem>();

        yield return new WaitForSeconds(eSystem.main.duration + eSystem.main.startLifetimeMultiplier);

        traps.RemoveAt(index);
        Destroy(e.gameObject);
    }

    private void AssignUIElements()
    {
        if (ActionUI.Instance)
        {
            ActionUI.Instance.lookAroundCornerLeft.onClick.AddListener(() => TriggerCoverTurn(false));
            ActionUI.Instance.lookAroundCornerLeft.onClick.AddListener(() => LookAroundCorner(false));
            ActionUI.Instance.lookAroundCornerLeft.onClick.AddListener(() => ClearCornerVision(true));
            ActionUI.Instance.lookAroundCornerRight.onClick.AddListener(() => TriggerCoverTurn(true));
            ActionUI.Instance.lookAroundCornerRight.onClick.AddListener(() => LookAroundCorner(true));
            ActionUI.Instance.lookAroundCornerRight.onClick.AddListener(() => ClearCornerVision(false));
            ActionUI.Instance.waitTurn.onClick.AddListener(() => playerInput.WaitTurn());
            ActionUI.Instance.pickUpIntelObjective.onClick.AddListener(() => PickupObjective());
            ActionUI.Instance.burnIntelObjective.onClick.AddListener(() => BurnIntel());
        }

        if (GameUI.Instance)
        {
            GameUI.Instance.InitializePlayerUI();
        }
    }

    public bool IsNearCellReachable(QuadCell fromCell, QuadCell toCell, QuadDirection direction)
    {
        if (toCell == null || (toCell.IsSpecial && !toCell.IsSpecialWalkable) || toCell.Unit || !toCell.Explorable || (!toCell.IsUnderwaterWalkable && toCell.IsUnderwater) ||
            (!QuadMetrics.CheckLadderDirection(direction, fromCell.LadderDirections) && fromCell.Elevation + (fromCell.Slope && fromCell.SlopeDirection != direction.Opposite() ? 1 : 0)
                < toCell.Elevation + (toCell.Slope && toCell.SlopeDirection != direction ? 1 : 0) - 3))
        {
            return false;
        }

        if (((int)direction & 1) != 0)
        {
            int additionalElevation =
                fromCell.Slope && (fromCell.SlopeDirection == direction.Previous() || fromCell.SlopeDirection == direction.Next()) ? 1 : 0;
            int additionalPrevNElevation = fromCell.GetNeighbor(direction.Previous()).GetAdditionalPreviousDiagonalElevation(direction.Previous());
            int additionalNElevation =
                toCell.Slope && (toCell.SlopeDirection != direction.Previous() || toCell.SlopeDirection != direction.Next()) ? 1 : 0;
            int additionalNextNElevation = fromCell.GetNeighbor(direction.Next()).GetAdditionalNextDiagonalElevation(direction.Next());

            if (fromCell.Elevation + additionalElevation == toCell.Elevation + additionalNElevation &&
                fromCell.Elevation + additionalElevation == fromCell.GetNeighbor(direction.Previous()).Elevation + additionalPrevNElevation &&
                fromCell.Elevation + additionalElevation == fromCell.GetNeighbor(direction.Next()).Elevation + additionalNextNElevation)
            {
                return true;
            }
            return false;
        }

        return true;
    }

    public void TriggerCoverTurn(bool rightSide)
    {
        if(rightSide != onCoverLookRight)
        {
            animationController.TriggerCoverTurnAnimation();
        }       
    }

    public void LookAroundCorner(bool rightSide, bool visionChanged = false)
    {
        if (InCover && (rightSide != onCoverLookRight || visionChanged))
        {
            onCoverLookRight = rightSide;

            if(!visionChanged)
                animationController.SetCoverTurnDirectionAnimation(onCoverLookRight);

            foreach (var cell in ignoreBlockCells)
            {
                cell.isBlockingViewIgnored = false;
            }
            ignoreBlockCells.Clear();

            float distance = 2f;
            QuadCell diagonalCell = location.GetNeighbor(coverDirection);

            while (distance < VisionRange && diagonalCell != null)
            {
                ignoreBlockCells.Add(diagonalCell);
                diagonalCell.isBlockingViewIgnored = true;
                diagonalCell.GetNeighbor(coverDirection);
                distance++;
            }

            distance = 2.41421356f;
            float diagonalDistance = 1.41421356f;
            
            diagonalCell = rightSide ? location.GetNeighbor(coverDirection.Next()) : location.GetNeighbor(coverDirection.Previous());
            diagonalCell.IsVisible = true;

            if (diagonalCell.Elevation + (diagonalCell.IsSpecial ? diagonalCell.SpecialBlockElevation : 0) - location.Elevation >= 3)
                return;

            float cellViewElevation = location.Position.y + Height + (location.Slope ? 1f : 0f);
            QuadDirection direction1, direction2;

            if (rightSide)
            {
                direction1 = coverDirection.Next();
                direction2 = coverDirection.Next2();
            }
            else
            {
                direction1 = coverDirection.Previous();
                direction2 = coverDirection.Previous2();
            }

            Vector3 diagonalBlock1, diagonalBlock2;
            if (diagonalCell.BlockViewElevation >= diagonalCell.GetNeighbor(direction2.Opposite()).BlockViewElevation ||
                diagonalCell.BlockViewElevation >= diagonalCell.GetNeighbor(coverDirection.Opposite()).BlockViewElevation)
            {
                if (diagonalCell.BlockViewElevation > cellViewElevation)
                {
                    diagonalBlock1 = diagonalCell.Position + QuadMetrics.GetFirstCorner(direction1.Opposite());
                    diagonalBlock1 = diagonalBlock2 = new Vector3(diagonalBlock1.x, diagonalCell.BlockViewElevation, diagonalBlock1.z);
                }
                else
                {
                    diagonalBlock1 = diagonalCell.Position + QuadMetrics.GetFirstCorner(direction1);
                    diagonalBlock1 = diagonalBlock2 = new Vector3(diagonalBlock1.x, diagonalCell.BlockViewElevation, diagonalBlock1.z);
                }
            }
            else
            {
                diagonalBlock1 = diagonalCell.GetNeighbor(direction2.Opposite()).Position + QuadMetrics.GetFirstCorner(direction1.Opposite());
                diagonalBlock1 = new Vector3(diagonalBlock1.x, diagonalCell.GetNeighbor(direction2.Opposite()).BlockViewElevation, diagonalBlock1.z);
                diagonalBlock2 = diagonalCell.GetNeighbor(coverDirection.Opposite()).Position + QuadMetrics.GetFirstCorner(direction1.Opposite());
                diagonalBlock2 = new Vector3(diagonalBlock2.x, diagonalCell.GetNeighbor(coverDirection.Opposite()).BlockViewElevation, diagonalBlock2.z);
            }

            while (diagonalCell != null)
            {
                QuadCell current = diagonalCell.GetNeighbor(coverDirection);

                GameManager.Instance.grid.GetDiagonalTriangleVision(location, current, coverDirection, distance, VisionRange);

                diagonalCell = rightSide ? diagonalCell.GetNeighbor(coverDirection.Next()) : diagonalCell.GetNeighbor(coverDirection.Previous());
                diagonalDistance += 1.41421356f;
                distance = diagonalDistance + 1f;

                if (diagonalDistance > VisionRange)
                    break;

                GameManager.Instance.grid.GetDiagonalCellVision(cellViewElevation, coverDirection, direction1, direction2, ref diagonalBlock1, ref diagonalBlock2, location, diagonalCell);
            }

            foreach (var cell in ignoreBlockCells)
            {
                cell.isBlockingViewIgnored = false;
            }
            ignoreBlockCells.Clear();
        }
    }

    public void ClearCornerVision(bool rightSide)
    {
        if (InCover && rightSide != onCoverLookRight)
        {
            float diagonalDistance = 2.41421356f;
            float distance = 3.41421356f;
            float range = Mathf.FloorToInt(VisionRange + 1f);

            QuadCell diagonalCell = rightSide ? location.GetNeighbor(coverDirection.Next()) :
                location.GetNeighbor(coverDirection.Previous());

            while (diagonalDistance <= range && diagonalCell != null)
            {               
                QuadCell cell = diagonalCell.GetNeighbor(coverDirection);
                while (distance <= range && cell != null)
                {
                    cell.IsVisible = false;
                    cell = cell.GetNeighbor(coverDirection);
                    distance++;
                }

                diagonalCell = rightSide ? diagonalCell.GetNeighbor(coverDirection.Next()) : diagonalCell.GetNeighbor(coverDirection.Previous());
                diagonalDistance += 1.41421356f;
                distance = diagonalDistance + 1f;

                diagonalCell.IsVisible = false;
            }

            OnLookAroundCorner?.Invoke();
        }
    }

    public void PickupObjective()
    {
        if(location == ObjectiveManager.Instance.cellWithIntel && !ObjectiveManager.Instance.IsIntelCollected)
        {
            ObjectiveManager.Instance.IsIntelCollected = true;
            ActionUI.Instance.pickUpIntelObjective.gameObject.SetActive(false);
        }
    }

    public void BurnIntel()
    {
        if (location.IsCellNeighbor(ObjectiveManager.Instance.cellToDestroy) && !ObjectiveManager.Instance.IsTargetCellDestroyed)
        {
            ObjectiveManager.Instance.cellToDestroy.CellHazard = CellHazards.Fire;
            ObjectiveManager.Instance.IsTargetCellDestroyed = true;
            ActionUI.Instance.burnIntelObjective.gameObject.SetActive(false);
        }
    }

    public void GetHit(float damage)
    {
        if(dodge != 0)
        {
            dodge--;

            FloatingTextHandler instance = Instantiate
                (floatingTextPrefab, transform.position, Quaternion.LookRotation(CameraMain.Instance._camera.transform.forward));

            instance.text.text = "Dodged";
        }
        else
        {
            Health -= damage;
            FloatingTextHandler instance = Instantiate
                (floatingTextPrefab, transform.position, Quaternion.LookRotation(CameraMain.Instance._camera.transform.forward));

            instance.text.text = damage.ToString();
        }
    }

    public override void Die()
    {
        GameUI.Instance.CloseAll();

        if (GameManager.Instance.enabled)
        {
            isDead = true;
            GameManager.Instance.stopCurrentAction = true;
            GetComponent<PlayerAnimationController>().SetDeathAnimation();
            GameManager.Instance.GameOverActionMap();
            GameManager.Instance.grid.ClearHighlightsAroundPlayer();
        }
        else
        {
            if (location)
            {
                GameManager.Instance.grid.ClearVision();
            }
            location.Unit = null;

            ActionUI.Instance.UnbindPlayer();
            instance = null;
            Destroy(gameObject);
        }        
    }
}
