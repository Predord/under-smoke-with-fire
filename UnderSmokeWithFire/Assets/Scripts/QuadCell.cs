using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class QuadCell : MonoBehaviour
{
    public bool isBlockingViewIgnored;
    public bool isPartiallyVisible;   
    public RectTransform uiRect;
    public QuadCoordinates coordinates;
    public QuadGridChunk chunk;

    public bool LockedForCover { get; set; }
    public bool LockedForTravel { get; set; }
    public bool Explorable { get; set; }
    public bool IsSpecialWalkable { get; set; }
    public int SearchDistancePhase { get; set; }
    public int Index { get; set; }
    public int SpecialTargetElevation { get; set; }
    public int SpecialBlockElevation { get; set; }
    public QuadDirection SpecialFeatureDirection { get; set; }
    public float SearchHeuristic { get; set; }
    public float Distance { get; set; }
    public QuadCell PathFrom { get; set; }
    public QuadCell NextWithSamePriority { get; set; }

#pragma warning disable 0649
    [SerializeField] private QuadCell[] neighbors;
#pragma warning restore 0649

    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            if (elevation == value)
                return;

            float originalViewElevation = TargetViewElevation;
            elevation = value;
            SetTargetViewElevation();
            if (TargetViewElevation != originalViewElevation)
            {
                GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
            }
            RefreshPosition();
            RefreshCover();
            Refresh();
        }
    }

    private int elevation = int.MinValue;

    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public bool Slope
    {
        get
        {
            return isSlope;
        }
        set
        {
            if (isSlope == value)
                return;

            float originalViewElevation = TargetViewElevation;
            isSlope = value;
            SetTargetViewElevation();
            if (!isSlope)
            {
                RectTransform highlight = uiRect.GetChild(0).GetComponent<RectTransform>();
                highlight.localRotation = Quaternion.identity;
                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -transform.position.y;
                uiRect.localPosition = uiPosition;
            }
            if (TargetViewElevation != originalViewElevation)
            {
                GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
            }
            Refresh();
        }
    }

    private bool isSlope;

    public Entity Unit
    {
        get
        {
            return unit;
        }
        set
        {
            unit = value;
            SetTargetViewElevation();
        }
    }

    private Entity unit;

    //add slope
    public int Covers
    {
        get
        {
            int covers = 0;
            for(int i = 0; i < 4; i++)
            {
                if(neighbors[i * 2] && (neighbors[i * 2].Elevation + (neighbors[i * 2].IsSpecial ? neighbors[i * 2].SpecialTargetElevation : 0) > elevation + 1))
                {
                    covers += 1 << i;

                    if(neighbors[i * 2].Elevation + (neighbors[i * 2].IsSpecial ? neighbors[i * 2].SpecialTargetElevation : 0) - elevation > 2)
                    {
                        covers += 1 << (i + 4);
                    }
                }
            }

            return covers;
        }
    }

    public float TargetViewElevation { get; private set; }

    public float BlockViewElevation
    {
        get
        {
            return (elevation + (IsSpecial ? SpecialBlockElevation : 0) 
                + (cellHazard == CellHazards.Smoke ? 3 : 0)) * QuadMetrics.elevationStep;
        }
    }

    public QuadDirection SlopeDirection
    {
        get
        {
            return slopeDirection;
        }
        set
        {
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -transform.position.y;
            uiPosition.z -= QuadMetrics.elevationStep / 2f;

            if (isSlope && slopeDirection == value)
            {
                uiRect.localPosition = uiPosition;
                return;
            }

            slopeDirection = value;

            RectTransform highlight = uiRect.GetChild(0).GetComponent<RectTransform>();
            highlight.localRotation = Quaternion.identity;
            Vector2 rotation = QuadMetrics.GetSlopeAngle(slopeDirection.Next2());
            highlight.localRotation = Quaternion.Euler(rotation.x, rotation.y, 0f);
            uiRect.localPosition = uiPosition;
            Refresh();
        }
    }

    private QuadDirection slopeDirection;

    public int TerrainTypeIndex
    {
        get
        {
            return terrainTypeIndex;
        }
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                GameManager.Instance.grid.cellShaderData.RefreshTerrain(this);
            }
        }
    }

    private int terrainTypeIndex;

    public CellHazards CellHazard
    {
        get
        {
            return cellHazard;
        }
        set
        {
            if (cellHazard == value || ((value == CellHazards.Fire || value == CellHazards.Smoke) && IsUnderwater))
                return;

            
            if (value == CellHazards.None)
            {
                if (cellHazard == CellHazards.Fire)
                {
                    cellHazard = value;
                    SetTargetViewElevation();
                    GameManager.Instance.grid.SetEffectsClear(this);
                    GameManager.Instance.grid.CheckCellVisible(this, 0);
                    GameManager.Instance.grid.RemoveCellWithHazard(this);
                }
                else if (cellHazard == CellHazards.Smoke)
                {
                    cellHazard = value;
                    SetTargetViewElevation();
                    GameManager.Instance.grid.SetEffectsClear(this);
                    GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
                    GameManager.Instance.grid.RemoveCellWithHazard(this);
                }
                else
                {
                    cellHazard = value;
                }
            }
            else if(value == CellHazards.Fire)
            {                
                if(cellHazard == CellHazards.None)
                {
                    cellHazard = value;
                    SetTargetViewElevation();
                    GameManager.Instance.grid.SetCellOnFire(this);
                    GameManager.Instance.grid.CheckCellVisible(this, 0);
                }
                else if(cellHazard == CellHazards.Smoke)
                {
                    cellHazard = value;
                    SetTargetViewElevation();
                    GameManager.Instance.grid.SetCellOnFire(this);                   
                    GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
                }
                else
                {
                    cellHazard = value;
                }

                GameManager.Instance.grid.AddCellWithHazard(this);

                if(this == ObjectiveManager.Instance.cellToDestroy && !ObjectiveManager.Instance.IsTargetCellDestroyed)
                {
                    ObjectiveManager.Instance.IsTargetCellDestroyed = true;
                }
            }
            else if(value == CellHazards.Smoke)
            {
                cellHazard = value;
                SetTargetViewElevation();
                GameManager.Instance.grid.SetCellSmoke(this);                
                GameManager.Instance.grid.cellShaderData.ViewElevationChanged();

                GameManager.Instance.grid.AddCellWithHazard(this);
            }
        }
    }

    private CellHazards cellHazard;

    public int WaterLevel
    {
        get
        {
            return waterLevel;
        }
        set
        {
            if (waterLevel == value)
                return;

            float originalViewElevation = TargetViewElevation;
            waterLevel = value;
            SetTargetViewElevation();
            if (TargetViewElevation != originalViewElevation)
            {
                GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
            }
            Refresh();
        }
    }

    private int waterLevel;

    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    public bool IsUnderwaterWalkable
    {
        get
        {
            return waterLevel <= elevation + 3;
        }
    }

    public int StoneDetailLevel
    {
        get
        {
            return stoneDetailLevel;
        }
        set
        {
            if (stoneDetailLevel != value)
            {
                stoneDetailLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    private int stoneDetailLevel;

    public int PlantDetailLevel
    {
        get
        {
            return plantDetailLevel;
        }
        set
        {
            if (plantDetailLevel != value)
            {
                plantDetailLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    private int plantDetailLevel;

    public int LadderDirections
    {
        get
        {
            return ladderDirections;
        }
        set
        {
            if (ladderDirections != value)
            {
                ladderDirections = value;
                RefreshSelfOnly();
            }
        }
    }

    private int ladderDirections;

    public int SpecialIndex
    {
        get
        {
            return specialIndex;
        }
        set
        {
            if (specialIndex != value)
            {
                float originalViewElevation = TargetViewElevation;
                specialIndex = value;
                SetTargetViewElevation();
                if (TargetViewElevation != originalViewElevation)
                {
                    GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
                }
                RefreshSelfOnly();
                RefreshCover();
            }
        }
    }

    private int specialIndex;

    public int LargeSpecialIndex
    {
        get
        {
            return largeSpecialIndex;
        }
        set
        {
            if (value == largeSpecialIndex)
                return;

            if(largeSpecialIndex != 0)
            {
                specialIndex = 0;
                GameManager.Instance.grid.RemoveLargeSpecialCell(this);
            }
            
            float originalViewElevation = TargetViewElevation;            
            largeSpecialIndex = value;

            if(largeSpecialIndex != 0)
            {
                GameManager.Instance.grid.AddLargeSpecialCell(this);
            }
            
            SetTargetViewElevation();
            if (TargetViewElevation != originalViewElevation)
            {
                GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
            }
            RefreshSelfOnly();
            RefreshCover();
        }
    }

    private int largeSpecialIndex;

    public bool IsSpecial
    {
        get
        {
            return specialIndex > 0 || largeSpecialIndex > 0;
        }
    }

    public float SearchPriority
    {
        get
        {
            return Distance + SearchHeuristic;
        }
    }

    public bool IsVisible
    {
        get
        {
            return isVisible && Explorable;
        }
        set
        {
            isPartiallyVisible = value;

            if (isVisible != value)
            {
                SetVisibility(value);

                if (largeSpecialIndex != 0)
                    GameManager.Instance.grid.cellShaderData.AddResetSpecialCellsVisibilityIndices(largeSpecialIndex);
            }
        }
    }

    private bool isVisible;

    public bool IsExplored { get; private set; }

    public bool IsTargeted
    {
        get
        {
            return isTargeted;
        }
        set
        {
            if(isTargeted != value)
            {
                isTargeted = value;
                GameManager.Instance.grid.cellShaderData.RefreshTarget(this);
            }
        }
    }

    private bool isTargeted;

    public QuadCell GetNeighbor(QuadDirection direction)
    {
        return neighbors[(int)direction];
    }

    public QuadCell GetNeighbor(int direction)
    {
        return neighbors[direction];
    }

    public void SetNeighbor(QuadDirection direction, QuadCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public QuadType GetEdgeType(QuadDirection direction)
    {
        if (!isSlope && !neighbors[(int)direction].isSlope)
        {
            if (elevation == neighbors[(int)direction].elevation)
                return QuadType.Flat;

            return QuadType.Cliff;
        }

        if (isSlope ^ neighbors[(int)direction].isSlope)
            return QuadType.SlopeCliff;

        if (elevation == neighbors[(int)direction].elevation && (slopeDirection == direction.Next2() || slopeDirection == direction.Previous2()))
        {
            if (neighbors[(int)direction].slopeDirection == slopeDirection)
                return QuadType.SlopeFlat;
            if (neighbors[(int)direction].slopeDirection == slopeDirection.Opposite())
                return QuadType.SlopeIntersection;
        }

        return QuadType.SlopeSlope;
    }

    public int GetCellNeighborConnectionElevationDifference(QuadType type, QuadDirection direction)
    {
        if (type == QuadType.Flat)
            return 0;
        if (type == QuadType.Cliff)
        {
            return QuadMetrics.GetElevationDifference(elevation, neighbors[(int)direction].elevation);
        }
        if (type == QuadType.SlopeFlat)
            return 0;
        if (type == QuadType.SlopeCliff)
        {
            int additionalElevation = 0;
            if (!isSlope)
            {
                if (neighbors[(int)direction].slopeDirection == direction.Opposite() ||
                    (elevation == neighbors[(int)direction].elevation && (direction.Next2() == neighbors[(int)direction].slopeDirection ||
                    direction.Previous2() == neighbors[(int)direction].slopeDirection)))
                    additionalElevation = 1;
                return QuadMetrics.GetElevationDifference(elevation, neighbors[(int)direction].elevation + additionalElevation);
            }
            else
            {
                if (slopeDirection == direction ||
                    (elevation == neighbors[(int)direction].elevation && (direction.Next2() == slopeDirection || direction.Previous2() == slopeDirection)))
                    additionalElevation = 1;
                return QuadMetrics.GetElevationDifference(elevation + additionalElevation, neighbors[(int)direction].elevation);
            }
        }
        if (type == QuadType.SlopeSlope)
        {
            if (direction == slopeDirection)
            {
                return 1;
            }
            if (direction.Opposite() == slopeDirection)
            {
                return -1;
            }
            int elevationDifference = QuadMetrics.GetElevationDifference(elevation, neighbors[(int)direction].elevation);
            if (elevationDifference == 0 && (direction == neighbors[(int)direction].slopeDirection || direction == neighbors[(int)direction].slopeDirection.Opposite()))
                return direction == neighbors[(int)direction].slopeDirection ? -1 : 1;
            return elevationDifference;
        }
        if (direction.Next2() == slopeDirection)
            return -1;
        return 1;
    }

    public int GetCellNeighborSlopeCornerElevationDifference(QuadDirection direction, bool isThirdVertix)
    {
        QuadType type = GetEdgeType(direction);
        if (type == QuadType.Flat)
            return 0;
        if (type == QuadType.Cliff)
        {
            return QuadMetrics.GetElevationDifference(elevation, neighbors[(int)direction].elevation);
        }
        if (type == QuadType.SlopeFlat)
            return 0;
        if (type == QuadType.SlopeCliff || type == QuadType.SlopeSlope)
        {
            int additionalElevation = 0;
            if (!isSlope)
            {
                if (neighbors[(int)direction].slopeDirection == direction.Opposite() || (!isThirdVertix && direction.Next2() == neighbors[(int)direction].slopeDirection) ||
                    (isThirdVertix && direction.Previous2() == neighbors[(int)direction].slopeDirection))
                    additionalElevation = 1;
                return QuadMetrics.GetElevationDifference(elevation, neighbors[(int)direction].elevation + additionalElevation);
            }
            else
            {
                if (slopeDirection == direction || (!isThirdVertix && direction.Next2() == slopeDirection) || (isThirdVertix && direction.Previous2() == slopeDirection))
                    additionalElevation += 1;
                if (neighbors[(int)direction].isSlope && (direction.Opposite() == neighbors[(int)direction].slopeDirection ||
                    (!isThirdVertix && direction.Next2() == neighbors[(int)direction].slopeDirection) ||
                    (isThirdVertix && direction.Previous2() == neighbors[(int)direction].slopeDirection)))
                    additionalElevation -= 1;
                int elevationDifference = QuadMetrics.GetElevationDifference(elevation + additionalElevation, neighbors[(int)direction].elevation);
                return elevationDifference;
            }
        }
        if (isThirdVertix)
        {
            if (direction.Next2() == slopeDirection)
                return 1;
            return -1;
        }
        if (direction.Next2() == slopeDirection)
            return -1;
        return 1;
    }

    public int GetCellSecondNeighborElevationDifference(QuadDirection direction)
    {
        QuadCell secondNeighbor = neighbors[(int)direction.Next()];
        int additionalElevation = 0;
        if (isSlope && (direction == slopeDirection || direction.Next2() == slopeDirection))
            additionalElevation++;
        if (secondNeighbor.isSlope && (direction == secondNeighbor.slopeDirection.Opposite() || direction.Previous2() == secondNeighbor.slopeDirection))
            additionalElevation--;

        return QuadMetrics.GetElevationDifference(elevation + additionalElevation, secondNeighbor.elevation);
    }

    public float GetCentralPointPosition(
        int elevationDifference, int elevationSecondDifference, int elevationThirdDifference,
        int additionalSlopeElevation, QuadDirection direction
    )
    {
        int flatCount = 1;
        float elevation = transform.localPosition.y + additionalSlopeElevation * QuadMetrics.elevationStep;
        if (elevationDifference == 0)
        {
            flatCount++;
            QuadCell neighbor = neighbors[(int)direction];
            elevation += neighbor.Position.y + (this.elevation + additionalSlopeElevation - neighbor.Elevation) * QuadMetrics.elevationStep;
        }
        if (elevationSecondDifference == 0)
        {
            flatCount++;
            QuadCell secondNeighbor = neighbors[(int)direction.Next()];
            elevation += secondNeighbor.Position.y + (this.elevation + additionalSlopeElevation - secondNeighbor.Elevation) * QuadMetrics.elevationStep;
        }
        if (elevationThirdDifference == 0)
        {
            flatCount++;
            QuadCell thirdNeighbor = neighbors[(int)direction.Next2()];
            elevation += thirdNeighbor.Position.y + (this.elevation + additionalSlopeElevation - thirdNeighbor.Elevation) * QuadMetrics.elevationStep;
        }
        elevation = elevation / flatCount;
        return elevation;
    }

    public float WaterSurfaceY
    {
        get
        {
            return (waterLevel + QuadMetrics.waterElevationOffset) * QuadMetrics.elevationStep;
        }
    }

    public int GetAdditionalPreviousDiagonalElevation(QuadDirection direction)
    {
        return isSlope &&
            (slopeDirection == direction.Opposite() ||
            slopeDirection == direction.Next2()) ? 1 : 0;
    }

    public int GetAdditionalNextDiagonalElevation(QuadDirection direction)
    {
        return isSlope &&
            (slopeDirection == direction.Opposite() ||
            slopeDirection == direction.Previous2()) ? 1 : 0;
    }

    public bool IsCellVisible(Vector3 blockPosition, Vector3 targetPosition, bool blockUp = false)
    {
        float visibleHeight = (new Vector3(blockPosition.x, targetPosition.y, blockPosition.z) - targetPosition).sqrMagnitude
            / (new Vector3(Position.x, targetPosition.y, Position.z) - targetPosition).sqrMagnitude;

        if (blockUp)
        {
            visibleHeight = Mathf.Sqrt(visibleHeight) * (targetPosition.y - Position.y - Player.Instance.Height);
            return blockPosition.y < targetPosition.y - visibleHeight;
        }
        else
        {
            visibleHeight = Mathf.Sqrt(visibleHeight) * (Position.y + Player.Instance.Height - targetPosition.y);
            return blockPosition.y < visibleHeight + targetPosition.y;
        }
    }

    public bool HasCoverInDirection(int direction)
    {
        if ((Covers & (1 << direction / 2)) != 0)
        {
            return true;
        }

        return false;
    }

    public bool HasCover()
    {
        int covers = Covers & 0x0F;
        return covers != 0;
    }

    public bool IsSmallCover(int direction)
    {
        return (Covers & (1 << direction / 2 + 4)) == 0;
    }

    public bool HasOnlyOneCover(out int covers)
    {
        covers = Covers & 0x0F;
        return (covers != 0) && ((covers & (covers - 1)) == 0);
    }

    public void ResetVisibility()
    {
        if (isVisible)
        {
            isVisible = false;
            GameManager.Instance.grid.cellShaderData.RefreshVisibility(this);
        }
    }

    public void RemoveLargeSpecialFeature()
    {
        largeSpecialIndex = 0;
        specialIndex = 0;
        SpecialBlockElevation = 0;
        SpecialTargetElevation = 0;

        SetTargetViewElevation();
        GameManager.Instance.grid.cellShaderData.ViewElevationChanged();
        RefreshSelfOnly();
        RefreshCover();
    }

    public void DisableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    public bool IsHighlighted()
    {
        return uiRect.GetChild(0).GetComponent<Image>().enabled;
    }

    public void DisableCoverHighlight()
    {
        for(int i = 1; i < uiRect.childCount; i++)
        {
            Image coverHighlight = uiRect.GetChild(i).GetComponent<Image>();
            coverHighlight.enabled = false;
        }
    }

    public void EnableCoverHighlight(Color color)
    {
        for (int i = 1; i < uiRect.childCount; i++)
        {
            Image coverHighlight = uiRect.GetChild(i).GetComponent<Image>();
            coverHighlight.color = color;
            coverHighlight.enabled = true;
        }
    }

    private void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * QuadMetrics.elevationStep;
        position.y += (QuadMetrics.SampleNoise(position).y * 2f - 1f) * QuadMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    private void RefreshUIPosition()
    {
        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -transform.position.y;
        uiPosition.z -= QuadMetrics.elevationStep / 2f;

        RectTransform highlight = uiRect.GetChild(0).GetComponent<RectTransform>();
        highlight.localRotation = Quaternion.identity;
        Vector2 rotation = QuadMetrics.GetSlopeAngle(slopeDirection.Next2());
        highlight.localRotation = Quaternion.Euler(rotation.x, rotation.y, 0f);

        uiRect.localPosition = uiPosition;
    }

    public void RefreshCover()
    {
        for (int i = 1; i < uiRect.childCount; i++)
        {
            Destroy(uiRect.GetChild(i).gameObject);
        }

        for (int i = 0; i < 4; i++)
        {
            if(neighbors[i * 2])
            {
                for (int j = 1; j < neighbors[i * 2].uiRect.childCount; j++)
                {
                    Destroy(neighbors[i * 2].uiRect.GetChild(j).gameObject);
                }
            }
        }
    }

    public void SetVisibility(bool value)
    {
        isVisible = value;
        IsExplored = true;
        GameManager.Instance.grid.cellShaderData.RefreshVisibility(this);

        if (cellHazard == CellHazards.Fire && IsExplored)
        {
            GameManager.Instance.grid.SetFireColor(this);
        }
    }

    public void SetTargetViewElevation()
    {
        if(IsSpecial)
        {
            TargetViewElevation = elevation * QuadMetrics.elevationStep + SpecialTargetElevation;
        }
        else if (Unit)
        {
            TargetViewElevation = elevation * QuadMetrics.elevationStep + Unit.Height + (isSlope ? 1f : 0f);
        }
        else if (elevation >= waterLevel)
        {
            TargetViewElevation = elevation * QuadMetrics.elevationStep + (isSlope ? 1f : 0f);
        }
        else
        {
            TargetViewElevation = waterLevel * QuadMetrics.elevationStep;
        }

        if(cellHazard == CellHazards.Fire || cellHazard == CellHazards.Smoke)
        {
            TargetViewElevation += 3f;
        }       
    }

    public bool IsCellNeighbor(QuadCell otherCell)
    {
        for(int direction = 0; direction < 8; direction++)
        {
            if (GetNeighbor((QuadDirection)direction) == otherCell)
                return true;
        }

        return false;
    }

    private void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                QuadCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }

    private void RefreshSelfOnly()
    {
        chunk.Refresh();
        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }

    private void RefreshHazards()
    {
        if (cellHazard == CellHazards.Fire && !IsUnderwater)
        {
            GameManager.Instance.grid.SetCellOnFire(this);
        }
        else if(cellHazard == CellHazards.Smoke)
        {
            GameManager.Instance.grid.SetCellSmoke(this);
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)stoneDetailLevel);
        writer.Write((byte)plantDetailLevel);
        writer.Write((byte)ladderDirections);
        writer.Write((byte)specialIndex);
        writer.Write(largeSpecialIndex);

        if(largeSpecialIndex != 0 || specialIndex != 0)
        {
            writer.Write(IsSpecialWalkable);
            writer.Write((byte)SpecialTargetElevation);
            writer.Write((byte)SpecialBlockElevation);

            if(specialIndex != 0)
            {
                writer.Write((byte)SpecialFeatureDirection);
            }
        }

        if (isSlope)
        {
            writer.Write((byte)(slopeDirection + 128));
        }
        else
        {
            writer.Write((byte)0);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        terrainTypeIndex = reader.ReadByte();
        GameManager.Instance.grid.cellShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        RefreshPosition();

        waterLevel = reader.ReadByte();
        stoneDetailLevel = reader.ReadByte();
        plantDetailLevel = reader.ReadByte();
        ladderDirections = reader.ReadByte();
        specialIndex = reader.ReadByte();
        largeSpecialIndex = reader.ReadInt32();

        if (largeSpecialIndex != 0 || specialIndex != 0)
        {
            IsSpecialWalkable = reader.ReadBoolean();
            SpecialTargetElevation = reader.ReadByte();
            SpecialBlockElevation = reader.ReadByte();

            if(specialIndex != 0)
            {
                SpecialFeatureDirection = (QuadDirection)reader.ReadByte();
            }

            GameManager.Instance.grid.AddLargeSpecialCell(this);

            GameManager.Instance.editor.currentLargeSpecialIndex = 
                Mathf.Max(GameManager.Instance.editor.currentLargeSpecialIndex, largeSpecialIndex + 1);
        }

        byte slopeData = reader.ReadByte();
        if (slopeData >= 128)
        {
            isSlope = true;
            slopeDirection = (QuadDirection)(slopeData - 128);
            RefreshUIPosition();
        }
        else
        {
            isSlope = false;
        }

        SetTargetViewElevation();
        GameManager.Instance.grid.cellShaderData.RefreshVisibility(this);
    }
}
