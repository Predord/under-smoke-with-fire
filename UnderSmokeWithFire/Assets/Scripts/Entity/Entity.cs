using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class Entity : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Range(0, 5)]
    public int strengthLevelSpawn;

    public bool isMoving;
    public int moveIndex;
    public int index;
    public float rotationSpeed = 360f;
    public EntityAudioController audioController;
    public event Action OnHealthChange;
    public static event Action<Entity> OnSelectedEntityChange;
    public Dictionary<BuffDebuffActionMap, int> characterActionMapBuffsDebuffs = new Dictionary<BuffDebuffActionMap, int>();

    protected bool isCrouching;
    protected bool initializedStats;
    protected bool onCoverLookRight;

    public float Health
    {
        get
        {
            return health;
        }
        set
        {
            health = value;
            health = Mathf.Clamp(health, 0f, GetMaxHealth());
            PlayerInfo.Health = health;

            OnHealthChange?.Invoke();

            if(health == 0)
            {
                Die();
            }
        }
    }

    private float health;

    public virtual bool InAction
    {
        get
        {
            return inAction;
        }
        set
        {
            inAction = value;
        }
    }

    protected bool inAction;

    public virtual bool IsActionPossible
    {
        get
        {
            return isActionPossible;
        }
        set
        {
            isActionPossible = value;
        }
    }

    protected bool isActionPossible;
    
    public virtual float ActionPoints
    {
        get
        {
            return actionPoints;
        }
        set
        {
            actionPoints = value;
            actionPoints = Mathf.Clamp(actionPoints, 0, int.MaxValue);
        }
    }

    protected float actionPoints;

    public virtual float SpentActionPoints
    {
        get
        {
            return spentActionPoints;
        }
        set
        {
            spentActionPoints = value;

            foreach (var buffDebuff in characterActionMapBuffsDebuffs.Keys.ToList())
            {
                characterActionMapBuffsDebuffs[buffDebuff] -= 1;
                OnBuffDebuffTurn(buffDebuff);
            }

            if (Location.CellHazard != CellHazards.None)
            {
                AddBuffDebuff(BuffsDebuffsActionMapDatabase.GetBuffDebuff((int)Location.CellHazard - 1));
            }
        }
    }

    protected float spentActionPoints;

    public virtual QuadCell Location
    {
        get
        {
            return location;
        }
        set
        {
            location = value;
            value.Unit = this;
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

    protected QuadCell location;

    public virtual PoseState ActivePoseState
    {
        get
        {
            return activePoseState;
        }
        set
        {
            activePoseState = value;
        }
    }

    protected PoseState activePoseState;

    public virtual bool InCover
    {
        get
        {
            return inCover;
        }
        set
        {
            if (value == inCover)
                return;

            inCover = value;
            if(!ActivePoseState.IsForcedState())
            {
                if (value)
                {
                    if (location.IsSmallCover((int)coverDirection))
                    {
                        ActivePoseState = PoseState.CrouchInCover;
                    }
                }
                else
                {
                    ActivePoseState = PoseState.Stand;
                }
            }
        }
    }

    protected bool inCover;

    public QuadDirection CoverDirection
    {
        get
        {
            return coverDirection;
        }
        set
        {
            coverDirection = value;
            Orientation = coverDirection.CoverDirectionToRotation();
        }
    }

    protected QuadDirection coverDirection;

    public float Orientation
    {
        get
        {
            return orientation;
        }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    protected float orientation;

    public virtual float VisionRange
    {
        get
        {
            float sight = StatConstants.defaultSightRange;

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

    public float Height
    {
        get
        {
            return ActivePoseState.IsCrouching() ? 1.3f : 1.7f;
        }
    }

    //change
    protected virtual void Awake()
    {
        audioController = GetComponent<EntityAudioController>();
        //InitializeStats();
        health = GetMaxHealth();
        //actionPoints = MaxActionPoints;
    }

    protected virtual void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;
        }
    }

    public virtual void GetCover()
    {       
    }

    public virtual void SetPositionForMove(QuadCell endCell)
    {
        location.Unit = null;
        location = endCell;
        location.Unit = this;
    }

    //add check if unit in cell
    public float GetMovePriority(QuadCell fromCell, QuadCell toCell, QuadDirection direction)
    {
        float movePriotity = fromCell.Distance + 1f;
        if (toCell.IsUnderwater)
        {
            if (toCell.IsUnderwaterWalkable)
            {
                movePriotity += (toCell.WaterLevel - toCell.Elevation) * 1.55f;
            }
            else
            {
                return -1f;
            }
        }

        if (toCell.CellHazard == CellHazards.Fire)
        {
            movePriotity += 15f;
        }
        else if (toCell.CellHazard == CellHazards.Smoke)
        {
            movePriotity += 1f;
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
                return movePriotity + 0.41421356f;
            }
            return -1f;
        }

        int additionalCellElevation = fromCell.Slope && fromCell.SlopeDirection != direction.Opposite() ? 1 : 0;
        int additionalNeighborElevation = toCell.Slope && toCell.SlopeDirection != direction ? 1 : 0;

        //check for better movecosts
        if (fromCell.Elevation + additionalCellElevation < toCell.Elevation + additionalNeighborElevation - 3)
        {
            if (QuadMetrics.CheckLadderDirection(direction, fromCell.LadderDirections))
            {
                return movePriotity += (toCell.Elevation - fromCell.Elevation) * 0.8f;
            }
            else
            {
                return -1f;
            }
        }
        else
        {
            if (QuadMetrics.CheckLadderDirection(direction, fromCell.LadderDirections))
            {
                return movePriotity += 2.4f;
            }
            else
            {
                if (fromCell.Elevation + additionalCellElevation < toCell.Elevation + additionalNeighborElevation)
                {
                    return movePriotity += (toCell.Elevation + additionalNeighborElevation - fromCell.Elevation - additionalCellElevation) * 1.4f;
                }
                else
                {
                    return movePriotity += (fromCell.Elevation + additionalCellElevation - toCell.Elevation - additionalNeighborElevation) * 0.3f;
                }
            }
        }
    }

    protected virtual IEnumerator TravelPath()
    {
        return null;
    }

    public virtual void OnBuffDebuffTurn(BuffDebuffActionMap buffDebuff)
    {
        Health -= buffDebuff.damageOverTime;
        
        if(characterActionMapBuffsDebuffs[buffDebuff] <= 0)
        {
            characterActionMapBuffsDebuffs.Remove(buffDebuff);
        }
    }

    public virtual void AddBuffDebuff(BuffDebuffActionMap buffDebuff)
    {
        if (characterActionMapBuffsDebuffs.ContainsKey(buffDebuff))
        {
            characterActionMapBuffsDebuffs[buffDebuff] = buffDebuff.turnsAmount;
        }
        else
        {
            characterActionMapBuffsDebuffs.Add(buffDebuff, buffDebuff.turnsAmount);
        }
    }

    public virtual void HandlePlayerNewLocation()
    {
    }

    public virtual void HandleEditMode(bool isEnabled)
    {
    }

    public virtual void Die()
    {
    }

    public virtual float GetMaxHealth()
    {
        return StatConstants.normalMaxHealth;
    }

    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public IEnumerator PlayOnEntityAbilityAnimation(GameObject e)
    {
        GameObject effect = Instantiate(e);
        effect.transform.SetParent(transform, false);
        effect.transform.localPosition = effect.transform.localPosition + Vector3.up * Height / 2f;

        ParticleSystem eSystem = effect.GetComponentInChildren<ParticleSystem>();

        yield return new WaitForSeconds(eSystem.main.duration + eSystem.main.startLifetimeMultiplier);

        Destroy(effect.gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)strengthLevelSpawn);
        location.coordinates.Save(writer);
        writer.Write((byte)index);
        writer.Write(orientation);
    }

    public virtual void SpecificsSave(BinaryWriter writer)
    {
    }

    public static bool Load(BinaryReader reader, int header)
    {
        int strengthLevelSpawn = reader.ReadByte();

        QuadCoordinates coordinates = QuadCoordinates.Load(reader);
        int index = reader.ReadByte();
        float orientation = reader.ReadSingle();

        if(strengthLevelSpawn <= GameManager.Instance.CurrentMapStrength || index == 0)
        {
            GameManager.Instance.grid.AddUnit(orientation, GameManager.Instance.grid.unitPrefabs[index], GameManager.Instance.grid.GetCell(coordinates), strengthLevelSpawn);
            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual void SpecificsLoad(BinaryReader reader, int header)
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (location.IsVisible)
        {
            OnSelectedEntityChange?.Invoke(this);
        }       
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnSelectedEntityChange?.Invoke(null);
    }
}
