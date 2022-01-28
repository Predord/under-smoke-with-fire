using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class AIControlsMenu : MonoBehaviour
{
    public int zoneToAssignIndex = -1;
    public RectTransform ClosePanel;
    public TMP_Text assignStrengthLevelSpawnText;
    public TMP_Text currentZoneIndex;
    public TMP_Text assignIndexText;
    public Slider strengthLevelSpawn;
    public QuadMapEditor editor;

    private bool IsAbleToAddCellToRoute, IsRouteVisible;
    private QuadCell currentCell;
    private List<QuadCell> selectedCells = new List<QuadCell>();

    [SerializeField] private InputAction addCell;

    public Enemy CurrentUnit
    {
        get
        {
            return currentUnit;
        }
        set
        {
            currentUnit = value;

            if(currentUnit != null)
            {
                currentZoneIndex.text = "Current zone index: " + CurrentUnit.AssignedDefencePositionIndex;
                assignStrengthLevelSpawnText.text = "Strength Level Spawn: " + CurrentUnit.strengthLevelSpawn;
            }
        }
    }

    public Enemy currentUnit;

    private void Start()
    {
        addCell.performed += _ => AddCell();
    }

    private void OnEnable()
    {
        addCell.Enable();
    }

    private void OnDisable()
    {
        addCell.Disable();
    }
    //add check if reachbale and can be escaped
    private void AddCell()
    {
        if (currentCell)
        {
            selectedCells[selectedCells.Count - 1].EnableHighlight(Color.white);
            selectedCells.Add(currentCell);
            currentCell.EnableHighlight(Color.cyan);
            currentCell = null;
        }
    }

    private void Update()
    {
        SelectCell();
    }

    private void SelectCell()
    {
        if (IsAbleToAddCellToRoute)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                QuadCell cell = GameManager.Instance.grid.GetCell(CameraMain.Instance._camera.ScreenPointToRay(GameManager.Instance.mouse.position.ReadValue()));
                if (cell == currentCell)
                    return;

                if (currentCell)
                {
                    if (currentCell == selectedCells[selectedCells.Count - 1])
                    {
                        currentCell.EnableHighlight(Color.cyan);
                    }
                    else
                    {
                        currentCell.EnableHighlight(Color.white);
                    }
                }

                if (cell && cell != selectedCells[selectedCells.Count - 1] && cell.IsHighlighted())
                {
                    currentCell = cell;
                    currentCell.EnableHighlight(Color.red);
                }
                else
                {
                    currentCell = null;
                }
            }
            else
            {
                if (currentCell)
                {
                    if (currentCell == selectedCells[selectedCells.Count - 1])
                    {
                        currentCell.EnableHighlight(Color.cyan);
                    }
                    else
                    {
                        currentCell.EnableHighlight(Color.white);
                    }
                }

                currentCell = null;
            }
        }
    }
    //add check if reachbale and can be escaped
    public void CreatePatrolRoute()
    {
        GameManager.Instance.grid.ClearHighlights();
        GameManager.Instance.grid.HighlightAll();
        CurrentUnit.Location.EnableHighlight(Color.cyan);
        currentCell = null;
        selectedCells.Clear();
        selectedCells.Add(CurrentUnit.Location);

        IsAbleToAddCellToRoute = true;
        IsRouteVisible = false;
    }

    public void ShowHidePatrolRoute()
    {
        if (IsRouteVisible)
        {
            if (CurrentUnit.patrolCells.Count > 1)
            {
                CurrentUnit.ShowHidePatrolRoute(false);
            }

            CurrentUnit.Location.EnableHighlight(Color.gray);
        }
        else
        {
            if (IsAbleToAddCellToRoute)
            {
                GameManager.Instance.grid.ClearHighlights();
                selectedCells.Clear();
                currentCell = null;
                IsAbleToAddCellToRoute = false;
            }

            if (CurrentUnit.patrolCells.Count > 1)
            {
                CurrentUnit.ShowHidePatrolRoute(true);
            }
            else
            {
                CurrentUnit.Location.EnableHighlight(Color.gray);
            }
        }

        IsRouteVisible = !IsRouteVisible;
    }

    public void SavePatrolRoute()
    {
        if (IsAbleToAddCellToRoute)
        {
            if (selectedCells.Count > 1)
            {
                CurrentUnit.SetNewPatrolRoute(selectedCells);
            }

            selectedCells.Clear();
            GameManager.Instance.grid.ClearHighlights();
            currentCell = null;
            IsAbleToAddCellToRoute = false;
            CurrentUnit.Location.EnableHighlight(Color.gray);
        }
    }

    public void DeletePatrolRoute()
    {
        if (IsAbleToAddCellToRoute)
        {
            selectedCells.Clear();
            GameManager.Instance.grid.ClearHighlights();
            IsAbleToAddCellToRoute = false;
            CurrentUnit.Location.EnableHighlight(Color.gray);
        }

        if (IsRouteVisible)
        {
            ShowHidePatrolRoute();
        }

        CurrentUnit.patrolCells.Clear();
    }

    public void SetStrengthLevelSpawn(float value)
    {
        CurrentUnit.strengthLevelSpawn = (int)value;

        assignStrengthLevelSpawnText.text = "Strength Level Spawn: " + CurrentUnit.strengthLevelSpawn;
    }

    public void AssignZoneIndex()
    {
        CurrentUnit.AssignedDefencePositionIndex = zoneToAssignIndex;

        currentZoneIndex.text = "Current zone index: " + CurrentUnit.AssignedDefencePositionIndex;
    }

    public void Open()
    {
        editor.enabled = false;
        gameObject.SetActive(true);
        ClosePanel.gameObject.SetActive(false);

        assignIndexText.text = "Zone index to assign: " + zoneToAssignIndex.ToString();
    }

    public void Close()
    {
        editor.enabled = true;

        selectedCells.Clear();
        currentCell = null;
        GameManager.Instance.grid.ClearHighlights();
        IsAbleToAddCellToRoute = false;

        ClosePanel.gameObject.SetActive(true);
        gameObject.SetActive(false);
        if (IsRouteVisible)
        {
            ShowHidePatrolRoute();
        }
    }

    public void OnEditModeClose()
    {
        CurrentUnit = null;
        IsAbleToAddCellToRoute = false;
        IsRouteVisible = false;
        currentCell = null;
        selectedCells.Clear();

        ClosePanel.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}
