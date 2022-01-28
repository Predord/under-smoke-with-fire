using System.IO;
using System.Linq;

public class ObjectiveManager : Singleton<ObjectiveManager>
{
    public int minStrengthLevelForCollectIntel;
    public int minStrengthLevelForTargetDestroy;
    public int completedObjectivesForEscape;
    public Entity entityToKill;
    public QuadCell cellWithIntel;
    public QuadCell cellToDestroy;

    private bool isEscapeObjectiveActive;
    private bool isKillTargetObjectiveActive;
    private bool isCollectIntelObjectiveActive;
    private bool isDestroyTargetObjectiveActive;

    public bool IsEntityToKillDead
    {
        get
        {
            return isEntityToKillDead;
        }
        set
        {
            if (isEntityToKillDead == value)
                return;

            isEntityToKillDead = value;
            if (entityToKill)
            {
                GameUI.Instance.UpdateObjectiveItem(isKillTargetObjectiveActive, isEntityToKillDead, 1, entityToKill.Location);
            }
            else
            {
                GameUI.Instance.UpdateObjectiveItem(isKillTargetObjectiveActive, isEntityToKillDead, 1, null);
            }           

            if (isEntityToKillDead)
            {
                completedObjectivesForEscape--;
                PlayerInfo.LevelUp();
                SetObjectiveEscape();
            }
        }
    }

    private bool isEntityToKillDead;

    public bool IsIntelCollected
    {
        get
        {
            return isIntelCollected;
        }
        set
        {
            if (isIntelCollected == value)
                return;

            isIntelCollected = value;
            GameUI.Instance.UpdateObjectiveItem(isCollectIntelObjectiveActive, isIntelCollected, 2, cellWithIntel);

            if (isIntelCollected)
            {
                completedObjectivesForEscape--;
                PlayerInfo.LevelUp();
                SetObjectiveEscape();
            }
        }
    }

    private bool isIntelCollected;

    public bool IsTargetCellDestroyed
    {
        get
        {
            return isTargetCellDestroyed;
        }
        set
        {
            if (isTargetCellDestroyed == value)
                return;

            isTargetCellDestroyed = value;
            GameUI.Instance.UpdateObjectiveItem(isDestroyTargetObjectiveActive, isTargetCellDestroyed, 3, cellToDestroy);

            if (isTargetCellDestroyed)
            {
                completedObjectivesForEscape--;
                PlayerInfo.LevelUp();
                SetObjectiveEscape();
            }
        }
    }

    private bool isTargetCellDestroyed;

    private void Awake()
    {
        if (!RegisterMe())
        {
            return;
        }
    }

    public void SetObjectiveEscape()
    {
        if (GameManager.Instance.grid.specialZones.Where(zone => zone.zoneType == SpecialZoneType.Exit) != null)
        {
            if (completedObjectivesForEscape <= 0)
            {
                GameUI.Instance.UpdateObjectiveItem
                    (isEscapeObjectiveActive, false, 0, GameManager.Instance.grid.GetCell(GameManager.Instance.grid.specialZones.Find(zone => zone.zoneType == SpecialZoneType.Exit).bottomLeftCoordinates));
                isEscapeObjectiveActive = true;
            }
        }
    }

    public void SetObjectiveEntityToKill(Entity entityToKill, bool isLoading = false)
    {
        if(entityToKill != null || isLoading)
        {
            this.entityToKill = entityToKill;

            if(entityToKill != null)
            {
                GameUI.Instance.UpdateObjectiveItem(isKillTargetObjectiveActive, isEntityToKillDead, 1, entityToKill.Location);
            }
            else
            {
                GameUI.Instance.UpdateObjectiveItem(isKillTargetObjectiveActive, isEntityToKillDead, 1, null);
            }
            
            isKillTargetObjectiveActive = true;
        }
    }

    public void SetObjectiveCollectIntel(QuadCell cellWithIntel, bool isLoading = false)
    {
        if (cellWithIntel == cellToDestroy)
            return;

        if(cellWithIntel != null || isLoading)
        {
            this.cellWithIntel = cellWithIntel;
            GameUI.Instance.UpdateObjectiveItem(isCollectIntelObjectiveActive, isIntelCollected, 2, cellWithIntel);
            isCollectIntelObjectiveActive = true;
        }
    }

    public void SetObjectiveDestroyTargetCell(QuadCell cellToDestroy, bool isLoading = false)
    {
        if (cellWithIntel == cellToDestroy)
            return;

        if (cellToDestroy != null || isLoading)
        {
            this.cellToDestroy = cellToDestroy;
            GameUI.Instance.UpdateObjectiveItem(isDestroyTargetObjectiveActive, isTargetCellDestroyed, 3, cellToDestroy);
            isDestroyTargetObjectiveActive = true;
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(isKillTargetObjectiveActive);

        if(isKillTargetObjectiveActive)
        {
            writer.Write(IsEntityToKillDead);

            if (!isEntityToKillDead)
            {
                entityToKill.Location.coordinates.Save(writer);
            }         
        }

        writer.Write(isCollectIntelObjectiveActive);

        if (isCollectIntelObjectiveActive)
        {
            writer.Write(isIntelCollected);
            writer.Write((byte)minStrengthLevelForCollectIntel);

            if (!isIntelCollected)
            {
                cellWithIntel.coordinates.Save(writer);
            }           
        }

        writer.Write(isDestroyTargetObjectiveActive);

        if (isDestroyTargetObjectiveActive)
        {
            writer.Write(isTargetCellDestroyed);
            writer.Write((byte)minStrengthLevelForTargetDestroy);

            if (!isTargetCellDestroyed)
            {
                cellToDestroy.coordinates.Save(writer);
            }
        }

        writer.Write((byte)completedObjectivesForEscape);
    }

    public void Load(BinaryReader reader, int header)
    {
        int missingObjectivesModifier = 0;

        isKillTargetObjectiveActive = reader.ReadBoolean();

        if (isKillTargetObjectiveActive)
        {
            isKillTargetObjectiveActive = false;
            isEntityToKillDead = reader.ReadBoolean();

            if (!isEntityToKillDead)
            {
                QuadCell targetCell = GameManager.Instance.grid.GetCell(QuadCoordinates.Load(reader));

                if (targetCell.Unit)
                {
                    SetObjectiveEntityToKill(targetCell.Unit, true);
                }
                else
                {
                    missingObjectivesModifier++;
                }
            }
            else
            {
                SetObjectiveEntityToKill(null, true);
            }                     
        }

        isCollectIntelObjectiveActive = reader.ReadBoolean();

        if (isCollectIntelObjectiveActive)
        {
            isCollectIntelObjectiveActive = false;
            isIntelCollected = reader.ReadBoolean();
            minStrengthLevelForCollectIntel = reader.ReadByte();

            if (!isIntelCollected)
            {
                if(minStrengthLevelForCollectIntel <= GameManager.Instance.CurrentMapStrength)
                {
                    SetObjectiveCollectIntel(GameManager.Instance.grid.GetCell(QuadCoordinates.Load(reader)), true);
                }
                else
                {
                    QuadCoordinates.Load(reader);
                    missingObjectivesModifier++;
                }
            }
            else
            {
                SetObjectiveCollectIntel(null, true);
            }
        }

        isDestroyTargetObjectiveActive = reader.ReadBoolean();

        if (isDestroyTargetObjectiveActive)
        {
            isDestroyTargetObjectiveActive = false;
            isTargetCellDestroyed = reader.ReadBoolean();
            minStrengthLevelForTargetDestroy = reader.ReadByte();

            if (!isTargetCellDestroyed)
            {
                if(minStrengthLevelForTargetDestroy <= GameManager.Instance.CurrentMapStrength)
                {
                    SetObjectiveDestroyTargetCell(GameManager.Instance.grid.GetCell(QuadCoordinates.Load(reader)), true);
                }
                else
                {
                    QuadCoordinates.Load(reader);
                    missingObjectivesModifier++;
                }
            }
            else
            {
                SetObjectiveDestroyTargetCell(null, true);
            }
        }

        completedObjectivesForEscape = reader.ReadByte() - missingObjectivesModifier;
        SetObjectiveEscape();
    }
}
