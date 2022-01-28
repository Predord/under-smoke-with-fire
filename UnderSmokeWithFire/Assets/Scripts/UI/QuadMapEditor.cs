using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using TMPro;

using Random = UnityEngine.Random;

public class QuadMapEditor : MonoBehaviour
{
    public int currentLargeSpecialIndex = 1;

    public Toggle editModeToggle;
    public Toggle showSpecialZonesToggle;
    public Material terrainMaterial;
    public AIControlsMenu aiControlsPanel;
    public ZoneManager zoneManager;

    public TMP_Text currentMinStrengthLevelForCollectIntel;
    public TMP_Text currentMinStrengthLevelForTargetDestroy;

    private bool applySlope;
    private bool applyElevation = true;
    private bool applyWaterLevel = true;
    private bool applyStoneDetailLevel;
    private bool applyPlantDetailLevel;
    private bool applySpecialIndex;
    private bool applyLaddersDirections;
    private bool applyLaddersMode;
    private bool isSpecialWalkable;
    private int activeTerrainTypeIndex;
    private int activeElevation;
    private int activeHazard;
    private int activeWaterLevel;
    private int activeStoneDetailLevel;
    private int activePlantDetailLevel;
    private int activeSpecialWidth = 0;
    private int activeSpecialHeight = 0;
    private int activeSpecialIndex = 0;
    private int[] activeSpecialBlockElevations = { 0 };
    private int[] activeSpecialTargetElevations = { 0 };
    private int activeLaddersDirections = 0;
    private int brushSize;
    private int activeEntityIndex;
    private int activeZoneIndex;
    private QuadDirection activeSpecialDirection;
    private QuadDirection activeSlopeDirection;
    private SpecialZone currentSpecialZone;
    private List<QuadCell> selectedCells = new List<QuadCell>();

    [SerializeField] private InputAction touchCell;
    [SerializeField] private InputAction alternativeAction;
    [SerializeField] private InputAction alternativeAction2;
    [SerializeField] private InputAction alternativeAction3;
    [SerializeField] private InputAction createDestroyUnit;

    private bool isAlternativeAction;
    private bool isAlternativeAction2;
    private bool isAlternativeAction3;

    private void Awake()
    {
        GameManager.Instance.editor = this;
        terrainMaterial.DisableKeyword("GRID_ON");
        SetEditMode(false);
    }

    private void Start()
    {
        touchCell.performed += _ => OnPlayerTouchCell();
        alternativeAction.performed += _ => AlternativeAction();
        alternativeAction2.performed += _ => AlternativeSecondAction();
        alternativeAction3.performed += _ => AlternativeThirdAction();
        createDestroyUnit.performed += _ => CreateDestroyUnit();
    }

    private void OnEnable()
    {
        touchCell.Enable();
        alternativeAction.Enable();
        alternativeAction2.Enable();
        alternativeAction3.Enable();
        createDestroyUnit.Enable();
    }


    private void OnDisable()
    {
        touchCell.Disable();
        alternativeAction.Disable();
        alternativeAction2.Disable();
        alternativeAction3.Disable();
        createDestroyUnit.Disable();

        isAlternativeAction = false;
        isAlternativeAction2 = false;
        isAlternativeAction3 = false;
    }

    private void Update()
    {
        DrawZone();
    }

    private void DrawZone()
    {
        if (!showSpecialZonesToggle.isOn && !EventSystem.current.IsPointerOverGameObject())
        {
            QuadCell currentCell = GetCellUnderCursor();
            if (currentCell && currentCell.Explorable && isAlternativeAction2 && selectedCells.Count == 1)
            {
                currentSpecialZone.ClearZoneHighlights();

                int zMin = Mathf.Min(currentCell.coordinates.Z, selectedCells[0].coordinates.Z);
                int xMin = Mathf.Min(currentCell.coordinates.X, selectedCells[0].coordinates.X);

                int zMax = zMin == currentCell.coordinates.Z ? selectedCells[0].coordinates.Z : currentCell.coordinates.Z;
                int xMax = xMin == currentCell.coordinates.X ? selectedCells[0].coordinates.X : currentCell.coordinates.X;

                currentSpecialZone.bottomLeftCoordinates = new QuadCoordinates(xMin, zMin);
                currentSpecialZone.zLength = zMax - zMin + 1;
                currentSpecialZone.xLength = xMax - xMin + 1;
                currentSpecialZone.zoneType = (SpecialZoneType)activeZoneIndex;

                currentSpecialZone.ShowZoneHighlights(QuadMetrics.GetZoneColor(currentSpecialZone.zoneType));
            }
        }
    }

    private void OnPlayerTouchCell()
    {
        if (!EventSystem.current.IsPointerOverGameObject() && !showSpecialZonesToggle.isOn)
        {
            QuadCell currentCell = GetCellUnderCursor();
            if (currentCell)
            {
                if (isAlternativeAction2)
                {
                    if (currentCell.Explorable)
                    {
                        GameManager.Instance.grid.specialZones.Add(currentSpecialZone);

                        if(currentSpecialZone.zoneType == SpecialZoneType.DefencePosition)
                        {
                            GameManager.Instance.grid.camps.Add(new EnemyCamp(GameManager.Instance.grid.specialZones.IndexOf(currentSpecialZone)));
                        }

                        if(currentSpecialZone.zoneType == SpecialZoneType.Exit)
                        {
                            ObjectiveManager.Instance.SetObjectiveEscape();
                        }
                    }
                }
                else
                {
                    if (isAlternativeAction)
                    {
                        if (brushSize == 0 && currentCell.IsHighlighted())
                        {
                            selectedCells.Remove(currentCell);
                            currentCell.DisableHighlight();
                            if (selectedCells.Count == 1 && selectedCells[0].Unit && selectedCells[0].Unit.index != 0)
                            {
                                aiControlsPanel.ClosePanel.gameObject.SetActive(true);
                                aiControlsPanel.CurrentUnit = (Enemy)selectedCells[0].Unit;
                            }
                            else
                            {
                                aiControlsPanel.CurrentUnit = null;
                                aiControlsPanel.ClosePanel.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            int centerX = currentCell.coordinates.X;
                            int centerZ = currentCell.coordinates.Z;

                            for (int z = centerZ - brushSize; z <= centerZ + brushSize; z++)
                            {
                                for (int x = centerX - brushSize; x <= centerX + brushSize; x++)
                                {
                                    currentCell = GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z));
                                    if (currentCell && !currentCell.IsHighlighted())
                                    {
                                        selectedCells.Add(currentCell);
                                        currentCell.EnableHighlight(Color.gray);
                                    }
                                }
                            }

                            if (selectedCells.Count == 1 && selectedCells[0].Unit && selectedCells[0].Unit.index != 0)
                            {
                                aiControlsPanel.ClosePanel.gameObject.SetActive(true);
                                aiControlsPanel.CurrentUnit = (Enemy)selectedCells[0].Unit;
                            }
                            else
                            {
                                aiControlsPanel.CurrentUnit = null;
                                aiControlsPanel.ClosePanel.gameObject.SetActive(false);
                            }
                        }
                    }
                    else if (isAlternativeAction3)
                    {
                        if(brushSize == 0 && selectedCells.Count == 1)
                        {
                            int zMin = Mathf.Min(currentCell.coordinates.Z, selectedCells[0].coordinates.Z);
                            int xMin = Mathf.Min(currentCell.coordinates.X, selectedCells[0].coordinates.X);

                            int zMax = zMin == currentCell.coordinates.Z ? selectedCells[0].coordinates.Z : currentCell.coordinates.Z;
                            int xMax = xMin == currentCell.coordinates.X ? selectedCells[0].coordinates.X : currentCell.coordinates.X;

                            if (zMax - zMin + 1 > 0 && xMax - xMin + 1 > 0)
                            {
                                for (int z = zMin; z < zMax + 1; z++)
                                {
                                    for (int x = xMin; x < xMax + 1; x++)
                                    {
                                        selectedCells.Add(GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z)));
                                        selectedCells[selectedCells.Count - 1].EnableHighlight(Color.gray);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (currentCell.IsHighlighted() && selectedCells.Count == 1)
                        {
                            EditCell(currentCell);
                        }

                        aiControlsPanel.CurrentUnit = null;
                        aiControlsPanel.ClosePanel.gameObject.SetActive(false);
                        for (int i = 0; i < selectedCells.Count; i++)
                        {
                            selectedCells[i].DisableHighlight();
                        }
                        selectedCells.Clear();

                        int centerX = currentCell.coordinates.X;
                        int centerZ = currentCell.coordinates.Z;

                        for (int z = centerZ - brushSize; z <= centerZ + brushSize; z++)
                        {
                            for (int x = centerX - brushSize; x <= centerX + brushSize; x++)
                            {
                                currentCell = GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z));
                                if (currentCell)
                                {
                                    selectedCells.Add(currentCell);
                                    currentCell.EnableHighlight(Color.gray);
                                }
                            }
                        }

                        if (brushSize == 0 && currentCell.Unit && currentCell.Unit.index != 0)
                        {
                            aiControlsPanel.CurrentUnit = (Enemy)selectedCells[0].Unit;
                            aiControlsPanel.ClosePanel.gameObject.SetActive(true);
                        }
                    }
                }              
            }
        }
    }

    /// temp for choosing searchfromcell

    private void AlternativeAction()
    {
        if (isAlternativeAction2 || isAlternativeAction3)
            return;

        isAlternativeAction = !isAlternativeAction;
    }

    private void AlternativeSecondAction()
    {
        if (isAlternativeAction || isAlternativeAction3 || selectedCells.Count != 1)
            return;

        isAlternativeAction2 = !isAlternativeAction2;

        if (!isAlternativeAction2)
        {
            currentSpecialZone.ClearZoneHighlights();

            currentSpecialZone.zLength = 0;
            currentSpecialZone.xLength = 0;
            selectedCells[0].EnableHighlight(Color.gray);
        }
    }

    private void AlternativeThirdAction()
    {
        if (isAlternativeAction || isAlternativeAction2)
            return;

        isAlternativeAction3 = !isAlternativeAction3;
    }

    /// 

    private void CreateDestroyUnit()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            QuadCell cell = GetCellUnderCursor();
            if (cell)
            {
                if (isAlternativeAction && cell.Unit)
                {
                    GameManager.Instance.grid.RemoveUnit(cell.Unit);
                }
                else if (!cell.Unit)
                {
                    GameManager.Instance.grid.AddUnit(Random.Range(0f, 360f), GameManager.Instance.grid.unitPrefabs[activeEntityIndex], cell);
                    GameManager.Instance.grid.OrderUnits();

                    if (activeEntityIndex == 0)
                    {
                        GameManager.Instance.grid.SetVisibleCells(Player.Instance.Location, Player.Instance.VisionRange);
                    }
                    else if(Player.Instance)
                    {
                        GameManager.Instance.grid.CheckCellVisible(cell, GameManager.Instance.grid.unitPrefabs[activeEntityIndex].Height);
                    }
                }
            }
        }
    }

    private QuadCell GetCellUnderCursor()
    {
        return GameManager.Instance.grid.GetCell(CameraMain.Instance._camera.ScreenPointToRay(GameManager.Instance.mouse.position.ReadValue()));
    }

    private void EditCells()
    {
        if (enabled && !isAlternativeAction2 && !showSpecialZonesToggle.isOn)
        {
            for (int i = 0; i < selectedCells.Count; i++)
            {
                EditCell(selectedCells[i]);
            }
        }
    }

    private void EditCell(QuadCell cell)
    {
        if (cell)
        {
            if (activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }
            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }
            if (applySlope)
            {
                cell.SlopeDirection = activeSlopeDirection;
            }
            if (applyStoneDetailLevel)
            {
                cell.StoneDetailLevel = activeStoneDetailLevel;
            }
            if (applyPlantDetailLevel)
            {
                cell.PlantDetailLevel = activePlantDetailLevel;
            }
            cell.Slope = applySlope;
            if (applyLaddersMode)
            {
                cell.LadderDirections = activeLaddersDirections;
            }
            if (applySpecialIndex)
            {
                if(activeSpecialHeight != 0 || activeSpecialWidth != 0)
                {
                    QuadCoordinates bottomLeftCell = GetLargeSpecialBottomLeftCell(activeSpecialDirection, cell.coordinates);
                    
                    QuadCell currentCell;
                    int[] correctedSpecialBlockElevations = GetSpecialElevationArrayWithDirection(activeSpecialDirection, activeSpecialBlockElevations);
                    int[] correctedSpecialTargetElevations = GetSpecialElevationArrayWithDirection(activeSpecialDirection, activeSpecialTargetElevations);

                    int xMax;
                    int zMax;

                    if(activeSpecialDirection == QuadDirection.North || activeSpecialDirection == QuadDirection.South)
                    {
                        xMax = activeSpecialWidth;
                        zMax = activeSpecialHeight;
                    }
                    else
                    {
                        xMax = activeSpecialHeight;
                        zMax = activeSpecialWidth;
                    }

                    for (int i = 0; i < zMax; i++)
                    {
                        for (int j = 0; j < xMax; j++)
                        {
                            currentCell = GameManager.Instance.grid.GetCell(new QuadCoordinates(bottomLeftCell.X + j, bottomLeftCell.Z + i));

                            currentCell.IsSpecialWalkable = isSpecialWalkable;
                            currentCell.SpecialBlockElevation = correctedSpecialBlockElevations[j + i * xMax];
                            currentCell.SpecialTargetElevation = correctedSpecialTargetElevations[j + i * xMax];
                            currentCell.LargeSpecialIndex = currentLargeSpecialIndex;
                        }
                    }

                    currentLargeSpecialIndex++;
                }
                else
                {
                    cell.IsSpecialWalkable = isSpecialWalkable;
                    cell.SpecialBlockElevation = activeSpecialBlockElevations[0];
                    cell.SpecialTargetElevation = activeSpecialTargetElevations[0];
                    cell.LargeSpecialIndex = 0;
                }

                cell.SpecialIndex = activeSpecialIndex;
                cell.SpecialFeatureDirection = activeSpecialDirection;

            }
            if (applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
            }

            cell.CellHazard = (CellHazards)activeHazard;
        }
    }

    private QuadCoordinates GetLargeSpecialBottomLeftCell(QuadDirection direction, QuadCoordinates origin)
    {
        if(direction == QuadDirection.East)
        {
            return new QuadCoordinates(origin.X - activeSpecialHeight / 2, origin.Z - (activeSpecialWidth - 1) / 2);
        }
        else if(direction == QuadDirection.South)
        {
            return new QuadCoordinates(origin.X - (activeSpecialWidth - 1) / 2, origin.Z - (activeSpecialHeight - 1) / 2);
        }
        else if(direction == QuadDirection.West)
        {
            return new QuadCoordinates(origin.X - (activeSpecialHeight - 1) / 2, origin.Z - activeSpecialWidth / 2);
        }
        else
        {
            return new QuadCoordinates(origin.X - activeSpecialWidth / 2, origin.Z - activeSpecialHeight / 2);
        }
    }

    private int[] GetSpecialElevationArrayWithDirection(QuadDirection direction, int[] specialElevationArray)
    {
        if(direction == QuadDirection.East)
        {
            int[] newSpecialArray = new int[specialElevationArray.Length];

            for (int i = 0; i < activeSpecialWidth; i++)
            {
                for (int j = 0; j < activeSpecialHeight; j++)
                {
                    newSpecialArray[j + i * activeSpecialHeight] = specialElevationArray[activeSpecialWidth - i + j * activeSpecialWidth - 1];
                }
            }

            return newSpecialArray;
        }
        else if(direction == QuadDirection.South)
        {
            Array.Reverse(specialElevationArray);

            return specialElevationArray;
        }
        else if(direction == QuadDirection.West)
        {
            int[] newSpecialArray = new int[specialElevationArray.Length];

            for (int i = 0; i < activeSpecialWidth; i++)
            {
                for (int j = 0; j < activeSpecialHeight; j++)
                {
                    newSpecialArray[j + i * activeSpecialHeight] = specialElevationArray[activeSpecialWidth - i + j * activeSpecialWidth - 1];
                }
            }

            Array.Reverse(newSpecialArray);

            return newSpecialArray;
        }
        else
        {
            return specialElevationArray;
        }
    }

    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
        EditCells();
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
        EditCells();
    }

    public void SetSlopeDirection(int index)
    {
        activeSlopeDirection = (QuadDirection)index;
        EditCells();
    }

    public void SetHazard(int index)
    {
        activeHazard = index;
        EditCells();
    }

    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int)level;
        EditCells();
    }

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    public void SetStoneDetailLevel(float level)
    {
        activeStoneDetailLevel = (int)level;
        EditCells();
    }

    public void SetPlantDetailLevel(float level)
    {
        activePlantDetailLevel = (int)level;
        EditCells();
    }

    public void SetSpecialParams(SpecialParams specialParams)
    {
        isSpecialWalkable = specialParams.isSpecialWalkable;
        activeSpecialWidth = specialParams.cellsInRowCount;
        activeSpecialHeight = specialParams.rowCount;
        activeSpecialTargetElevations = specialParams.cellTargetElevations;
        activeSpecialBlockElevations = specialParams.cellBlockElevations;
    }

    public void SetSpecialDirection(int index)
    {
        activeSpecialDirection = (QuadDirection)index;
    }

    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int)index;
    }

    public void SetObjectivesForEscape(float value)
    {
        ObjectiveManager.Instance.completedObjectivesForEscape = (int)value;
    }

    public void SetLaddersDirections(int value)
    {
        if (applyLaddersDirections)
        {
            activeLaddersDirections += value;
        }
        else
        {
            activeLaddersDirections -= value;
        }
    }

    public void SetEntityIndex(int index)
    {
        activeEntityIndex = index;
    }

    public void SetZoneIndex(int index)
    {
        activeZoneIndex = index;
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    public void SetApplyStoneDetailLevel(bool toggle)
    {
        applyStoneDetailLevel = toggle;
    }

    public void SetApplyPlantDetailLevel(bool toggle)
    {
        applyPlantDetailLevel = toggle;
    }

    public void SetApplySpecialIndex(bool toggle)
    {
        applySpecialIndex = toggle;
    }

    public void SetApplyWallMode(bool toggle)
    {
        applyLaddersMode = toggle;
    }

    public void SetApplyWallDirections(bool toggle)
    {
        applyLaddersDirections = toggle;
    }

    public void SetApplySlope(bool toggle)
    {
        applySlope = toggle;
    }

    public void SetEditMode(bool toggle)
    {
        GameManager.Instance.SetEditMode(toggle);

        if (toggle && !GameManager.Instance.IsEnterEditModePossible())
        {
            editModeToggle.SetIsOnWithoutNotify(false); 
            return;
        }

        enabled = toggle;
        if (toggle)
        {
            for(int i = 0; i < selectedCells.Count; i++)
            {
                selectedCells[i].EnableHighlight(Color.gray);
            }

            if (selectedCells.Count == 1)
            {
                if(selectedCells[0].Unit && selectedCells[0].Unit.index != 0)
                {
                    aiControlsPanel.CurrentUnit = (Enemy)selectedCells[0].Unit;
                    aiControlsPanel.ClosePanel.gameObject.SetActive(true);
                }
            }

            showSpecialZonesToggle.interactable = true;
        }
        else
        {
            showSpecialZonesToggle.isOn = false;
            showSpecialZonesToggle.interactable = false;
            aiControlsPanel.OnEditModeClose();
            zoneManager.Close();
        }
    }

    public void SetEntityToKill()
    {
        if(selectedCells.Count == 1 && selectedCells[0].Unit != null && selectedCells[0].Unit != Player.Instance)
        {
            ObjectiveManager.Instance.SetObjectiveEntityToKill(selectedCells[0].Unit);
        }
    }

    public void SetCellWithIntel()
    {
        if (selectedCells.Count == 1 && selectedCells[0].Explorable)
        {
            ObjectiveManager.Instance.SetObjectiveCollectIntel(selectedCells[0]);
        }
    }

    public void SetCellWithIntelToDestroy()
    {
        if (selectedCells.Count == 1 && selectedCells[0].Explorable)
        {
            ObjectiveManager.Instance.SetObjectiveDestroyTargetCell(selectedCells[0]);
        }
    }

    public void FocusCameraOnEntityToKill()
    {
        CameraMain.Instance.FocusCameraOnCell(ObjectiveManager.Instance.entityToKill.Location);
    }

    public void FocusCameraOnCellWithIntel()
    {
        CameraMain.Instance.FocusCameraOnCell(ObjectiveManager.Instance.cellWithIntel);
    }

    public void FocusCameraOnCellWithIntelToDestroy()
    {
        CameraMain.Instance.FocusCameraOnCell(ObjectiveManager.Instance.cellToDestroy);
    }

    public void SetCollectIntelMinStrength(float value)
    {
        ObjectiveManager.Instance.minStrengthLevelForCollectIntel = (int)value;

        currentMinStrengthLevelForCollectIntel.text = "Min str level to collect: " + ObjectiveManager.Instance.minStrengthLevelForCollectIntel;
    }

    public void SetDestroyTargetMinStrength(float value)
    {
        ObjectiveManager.Instance.minStrengthLevelForTargetDestroy = (int)value;

        currentMinStrengthLevelForTargetDestroy.text = "Min str level for destroy: " + ObjectiveManager.Instance.minStrengthLevelForTargetDestroy;
    }

    public void ShowGrid(bool visible)
    {
        if (visible)
        {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else
        {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    public void ShowHideSpecialZones(bool toggle)
    {
        if (toggle)
        {
            aiControlsPanel.CurrentUnit = null;
            aiControlsPanel.ClosePanel.gameObject.SetActive(false);

            for (int i = 0; i < GameManager.Instance.grid.specialZones.Count; i++)
            {
                GameManager.Instance.grid.specialZones[i].ShowZoneHighlights(QuadMetrics.GetZoneColor(GameManager.Instance.grid.specialZones[i].zoneType));
            }

            zoneManager.Open();
        }
        else
        {
            for (int i = 0; i < GameManager.Instance.grid.specialZones.Count; i++)
            {
                GameManager.Instance.grid.specialZones[i].ClearZoneHighlights();
            }

            zoneManager.Close();
        }
    }

    public void ClearSelectedCells()
    {
        selectedCells.Clear();
    }
}
