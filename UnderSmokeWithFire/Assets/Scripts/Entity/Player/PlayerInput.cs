using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

using Random = UnityEngine.Random;

public class PlayerInput : MonoBehaviour, IMoveInput, IAttackModeInput, IAttackAreaInput
{
    public Transform target;
    public event Action OnLocationChange;
    public bool Casting { get; private set; }

    public Command moveCommand;    
    public Command attackModeCommand;
    public Command attackAreaCommand;
    public Command waitCommand;

    [HideInInspector]
    public float currentAbilityCurveMiddlePoint;

    private const int gridLayerMask = 8;

    private bool stopAction;
    private bool alternativeAction;
    private int projectilesFired;
    private int currentPathIndex;
    private int layerMask = 1 << gridLayerMask;
    private float minAbilityCurveMiddlePoint = 2f;
    private float maxAbilityCurveMiddlePoint = 8f;   
    private float currentAbilityAOERadius;
    private float currentAbilityMaxDistance;
    private float partialCurveLength;
    private float fullCurveLength;
    private TrajectoryTypes abilityTrajectoryType;
    private Vector3 previousTargetPosition;
    private QuadCell currentCell;
    private Ability selectedAbility;
    private PlayerAnimationController animationController;
    private List<QuadCell> path = new List<QuadCell>();
    private List<QuadCell> trapCells = new List<QuadCell>();
    private List<Entity> affectedEntities = new List<Entity>();

    public bool AttackMode
    {
        get
        {
            return attackMode;
        }
        private set
        {
            if (!value && attackMode == value)
                return;

            attackMode = value;       
            if (value)
            {
                trapCells.Clear();
                affectedEntities.Clear();

                layerMask = selectedAbility.isOnUnitUse ? selectedAbility.targetedEntitiesMask == -1 ? (1 << 10) | (1 << 11) :
                    1 << selectedAbility.targetedEntitiesMask : 1 << gridLayerMask; 
                abilityTrajectoryType = (TrajectoryTypes)selectedAbility.trajectoryType;

                if (abilityTrajectoryType == TrajectoryTypes.Curve)
                {
                    currentAbilityCurveMiddlePoint = minAbilityCurveMiddlePoint;
                    Vector3 origin = Player.Instance.transform.position + Vector3.up * Player.Instance.Height / 2f;
                    //Vector3(0.7f, 0.7f, 0f) depends on orientation
                    Vector3 fullLengthDestination = origin + new Vector3(0.7f, 0.7f, 0f) * 2f * QuadMetrics.radius * selectedAbility.GetStatValue(AbilityStats.MaxDistance);
                    fullCurveLength = Bezier.GetLength(origin,
                        new Vector3(origin.x + (fullLengthDestination.x - origin.x) * 0.5f, fullLengthDestination.y + maxAbilityCurveMiddlePoint, origin.z + (fullLengthDestination.z - origin.z) * 0.5f),
                        fullLengthDestination, 1f);
                }

                CameraMain.Instance.abilityLines.SetDrawFunction(abilityTrajectoryType);
                if (attackModeCommand != null)
                {
                    attackModeCommand.Execute();
                }
            }
            else
            {
                IsValidAttackPoint = false;
                selectedAbility = null;
            }
        }
    }

    private bool attackMode;

    public bool IsValidAttackPoint
    {
        get
        {
            return isValidAttackPoint;
        }
        set
        {
            if (value == isValidAttackPoint)
                return;

            isValidAttackPoint = value;
            if (value)
            {
                ActionUI.Instance.actionPointsPopup.gameObject.SetActive(true);

                target.GetChild(0).gameObject.SetActive(true);
                if (selectedAbility.GetStatValue(AbilityStats.AOERadius) > 0f)
                {
                    currentAbilityAOERadius = 2f * QuadMetrics.radius * selectedAbility.GetStatValue(AbilityStats.AOERadius);
                    Transform targetSphere = target.GetChild(1);
                    targetSphere.gameObject.SetActive(true);
                    targetSphere.localScale = Vector3.one * 2f * currentAbilityAOERadius;
                }
                else
                {
                    currentAbilityAOERadius = 0f;
                }
            }
            else
            {
                ActionUI.Instance.actionPointsPopup.gameObject.SetActive(false);

                TargetCell = null;
                target.GetChild(0).gameObject.SetActive(false);
                target.GetChild(1).gameObject.SetActive(false);
            }
        }
    }

    private bool isValidAttackPoint;

    public QuadCell TargetCell
    {
        get
        {
            return targetCell;
        }
        set
        {
            if (targetCell)
            {
                targetCell.IsTargeted = false;
                if (selectedAbility.isTrap || selectedAbility.isOnUnitUse)
                {
                    SetTargetedCellsCenterLocked(false);
                }
                else
                {
                    SetTargetedCells(false);
                }                
            }

            targetCell = value;
            if (value)
            {
                if (selectedAbility.isTrap || selectedAbility.isOnUnitUse)
                {
                    SetTargetedCellsCenterLocked(true);
                }
                else
                {
                    SetTargetedCells(true);
                    targetCell.IsTargeted = target.position.y + 1.5f * QuadMetrics.elevationStep >= targetCell.Position.y &&
                        Vector3.Distance(targetCell.Position, target.position) <= currentAbilityAOERadius;
                }            
            }
        }
    }

    private QuadCell targetCell;

    private void Start()
    {
        animationController = GetComponent<PlayerAnimationController>();
    }

    private void Update()
    {
        DoFindPath();
    }

    public void OnStopAction(InputAction.CallbackContext context)
    {
        if (context.performed && Player.Instance.InAction && !GameManager.Instance.stopCurrentAction)
        {
            stopAction = true;
        }
    }

    public void OnAlternativeAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            alternativeAction = !alternativeAction;
            if (GameManager.Instance.enabled && Player.Instance.IsActionPossible)
            {
                currentCell = null;

                if (alternativeAction)
                {
                    GameManager.Instance.grid.ClearHighlightsAroundPlayer();
                    GameManager.Instance.grid.HasPath = false;
                }
                else
                {
                    GameManager.Instance.grid.DisablePathHighlights(path);
                    GameManager.Instance.grid.FindPlayerDistance();
                }
            }
        }
    }
    /*
    private void OnMenuButton(InputAction.CallbackContext context)
    {
        if (AttackMode && !Casting)
        {
            AttackMode = false;
            selectedAbility = null;
            GameManager.Instance.grid.FindPlayerDistance();
        }
        else if(GameUI.Instance.abilityPanel.gameObject.activeSelf || GameUI.Instance.statsPanel.gameObject.activeSelf)
        {
            GameUI.Instance.CloseAll();
        }
        else
        {
            //Implement Menu call
        }
    }*/

    public void OnAttackMode(InputAction.CallbackContext context)
    {
        if (context.performed && !Casting)
        {
            Ability ability = PlayerInfo.hotBarAbilities[(int)context.ReadValue<float>() - 1];

            if (Player.Instance.IsActionPossible && ability != null && Player.Instance.activeAbilitiesCooldowns[ability] == 0f && (selectedAbility == null || ability != selectedAbility) && GameManager.Instance.enabled)
            {
                if (selectedAbility != null && IsValidAttackPoint)
                {
                    QuadCell cell = targetCell;
                    IsValidAttackPoint = false;
                    selectedAbility = ability;
                    TargetCell = cell;
                    IsValidAttackPoint = true;
                }
                else
                {
                    selectedAbility = ability;
                }

                currentAbilityMaxDistance = 2f * QuadMetrics.radius * selectedAbility.GetStatValue(AbilityStats.MaxDistance);
                AttackMode = true;
                GameManager.Instance.grid.ClearHighlightsAroundPlayer();
            }
        }
    }
    /*
    private void OnOpenActionListButton(InputAction.CallbackContext context)
    {
        if (Player.Instance)
        {
            if (AttackMode && !Casting)
            {
                AttackMode = false;
                selectedAbility = null;
            }
            GameUI.Instance.OpenPlayerActionList();
        }
    }

    private void OnOpenPlayerStatsButton(InputAction.CallbackContext context)
    {
        if (Player.Instance)
        {
            if (AttackMode && !Casting)
            {
                AttackMode = false;
                selectedAbility = null;
            }
            GameUI.Instance.OpenPlayerStats();
        }
    }*/

    private void DoFindPath()
    {
        if (alternativeAction)
        {
            if (GameManager.Instance.enabled && !AttackMode && UpdateCurrentCellFarTravel() && !EventSystem.current.IsPointerOverGameObject() &&
                GameManager.Instance.grid.FindDistanceHeuristic(Player.Instance, Player.Instance.Location, currentCell, false, true))
            {
                if(path.Count > 0)
                {
                    GameManager.Instance.grid.DisablePathHighlights(path);
                }

                path = GameManager.Instance.grid.FindPathClear(Player.Instance.Location, currentCell);
                GameManager.Instance.grid.HasPath = true;
                GameManager.Instance.grid.EnablePathHighlights(Player.Instance.Location, currentCell);
            }
            else if(path.Count > 0 && currentCell != path[path.Count - 1])
            {
                GameManager.Instance.grid.DisablePathHighlights(path);
                GameManager.Instance.grid.HasPath = false;               
                path.Clear();
            }
        }
        else
        {
            if (GameManager.Instance.enabled && !AttackMode && UpdateCurrentCell() && currentCell && !EventSystem.current.IsPointerOverGameObject())
            {
                GameManager.Instance.grid.GetMoveToCell(currentCell);
            }
        }
    }

    private bool UpdateCurrentCell()
    {
        QuadCell cell = GameManager.Instance.grid.GetCell(CameraMain.Instance._camera.ScreenPointToRay(GameManager.Instance.mouse.position.ReadValue()));
        if (cell != null && cell != currentCell && Player.Instance.IsActionPossible && cell.SearchDistancePhase > GameManager.Instance.grid.searchFrontierPhase)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    private bool UpdateCurrentCellFarTravel()
    {
        QuadCell cell = GameManager.Instance.grid.GetCell(CameraMain.Instance._camera.ScreenPointToRay(GameManager.Instance.mouse.position.ReadValue()));
        if (cell != null && cell != currentCell && Player.Instance.IsActionPossible)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    public void OnExecuteActionWithMouse(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (Player.Instance.IsActionPossible)
            {
                if (AttackMode)
                {
                    if (attackAreaCommand != null && IsValidAttackPoint)
                    {
                        if (selectedAbility.isOnUnitUse)
                        {
                            affectedEntities.Add(targetCell.Unit);

                            if (affectedEntities.Count < selectedAbility.projectilesCount)
                            {
                                return;
                            }
                        }
                        else if (selectedAbility.isTrap)
                        {
                            trapCells.Add(targetCell);

                            if (trapCells.Count < selectedAbility.projectilesCount)
                            {
                                return;
                            }
                        }

                        Player.Instance.ActionPoints = GetCastTime(selectedAbility.GetStatValue(AbilityStats.CastTime));
                        Player.Instance.InAction = true;
                        GameManager.Instance.grid.HasPath = false;
                        projectilesFired = 0;
                        Casting = true;
                        attackMode = false;
                        stopAction = false;
                        IsValidAttackPoint = false;
                        attackAreaCommand.Execute();
                    }
                }
                else if (GameManager.Instance.grid.HasPath)
                {
                    QuadCell cell = GameManager.Instance.grid.GetCell(CameraMain.Instance._camera.ScreenPointToRay(GameManager.Instance.mouse.position.ReadValue()));
                    if (cell != null && cell == currentCell && !EventSystem.current.IsPointerOverGameObject())
                    {
                        GameManager.Instance.grid.HasPath = false;
                        if (alternativeAction)
                        {
                            GameManager.Instance.grid.DisablePathHighlights(path);
                            if (moveCommand != null)
                            {
                                moveCommand.Execute();
                            }
                        }
                        else
                        {
                            GameManager.Instance.grid.ClearHighlightsAroundPlayer();
                            if (moveCommand != null)
                            {
                                moveCommand.Execute();
                            }
                        }
                    }
                }
            }
            else if (AttackMode && Casting)
            {
                if (IsValidAttackPoint)
                {
                    projectilesFired++;

                    SetAbilityProjectile();

                    if (projectilesFired < selectedAbility.projectilesCount)
                        return;

                    IsValidAttackPoint = false;
                    Player.Instance.SpentActionPoints++;
                    attackMode = false;
                }
            }
        }         
    }

    public void DoMoveKeyboard(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (GameManager.Instance.grid.HasPath && Player.Instance.IsActionPossible && !AttackMode)
            {
                GameManager.Instance.grid.HasPath = false;
                if (alternativeAction)
                {
                    GameManager.Instance.grid.DisablePathHighlights(path);
                    if (moveCommand != null)
                    {
                        moveCommand.Execute();
                    }
                }
                else
                {
                    GameManager.Instance.grid.ClearHighlightsAroundPlayer();
                    if (moveCommand != null)
                    {
                        moveCommand.Execute();
                    }
                }
            }
        }
    }

    public void WaitTurn()
    {
        if (Player.Instance.IsActionPossible)
        {
            Player.Instance.ActionPoints = 1f;
            Player.Instance.InAction = true;

            if (waitCommand != null)
            {
                waitCommand.Execute();
            }
        }   
    }

    public List<QuadCell> GetPath()
    {
        stopAction = false;
        currentPathIndex = 2;

        if (alternativeAction)
        {
            Player.Instance.ActionPoints = GameManager.Instance.grid.GetPathCost(path);
            
            return path.GetRange(0, path.Count);
        }
        else
        {
            path = new List<QuadCell>
            {
                Player.Instance.Location,
                currentCell
            };

            Player.Instance.ActionPoints = GameManager.Instance.grid.GetPathCost(path);

            return path;
        } 
    }

    public void MoveStart(QuadCell endCell)
    {
        Player.Instance.SetPositionForMove(endCell);
        Player.Instance.Location.DisableCoverHighlight();
        Player.Instance.InAction = true;
        Player.Instance.InCover = false;

        animationController.SetRunningAnimation(true);
        Player.Instance.audioController.PlayAudio("Steps");
    }

    public bool MoveIterationEnd()
    {
        GameManager.Instance.grid.ClearVision();
        GameManager.Instance.grid.SetVisibleCells(Player.Instance.Location, Player.Instance.VisionRange);
        OnLocationChange?.Invoke();

        if(currentPathIndex < path.Count && 
            (path[currentPathIndex].LockedForTravel || IsPathCellNeighborCellLocked(path[currentPathIndex], currentCell)))
        {
            Player.Instance.ActionPoints = 1f;
            GameManager.Instance.stopCurrentAction = true;
        }

        return GameManager.Instance.stopCurrentAction;
    }

    public void StartClimbing(bool isSmallCliff)
    {
        animationController.SetClimbingAnimation(isSmallCliff);     
    }

    public bool ActionPointSpend()
    {
        Player.Instance.SpentActionPoints++;

        return false;
    }

    public bool IterationEndActionPointSpend(QuadCell currentCell)
    {
        Player.Instance.SpentActionPoints++;
        Player.Instance.SetPositionForMove(currentCell);

        if (stopAction && !GameManager.Instance.stopCurrentAction)
        {
            Player.Instance.ActionPoints = 1f;
            GameManager.Instance.stopCurrentAction = true;
        }

        currentPathIndex++;

        return false;
    }

    public void MoveEnd()
    {
        Player.Instance.audioController.StopAudio("Steps");
        animationController.SetRunningAnimation(false);

        stopAction = false;
        Player.Instance.PreviousLocation = Player.Instance.Location;
        Player.Instance.GetCover();
        Player.Instance.InAction = false;

        GameManager.Instance.CheckForExitZone(Player.Instance.Location);
    }

    private bool IsPathCellNeighborCellLocked(QuadCell cell, QuadCell cellToIgnore)
    {
        for(int i = 0; i < 8; i++)
        {
            QuadCell current = cell.GetNeighbor(i);
            if (current.LockedForTravel && current != cellToIgnore)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 RotationDirection()
    {
        if (Physics.Raycast(CameraMain.Instance._camera.ScreenPointToRay(GameManager.Instance.mouse.position.ReadValue()), out RaycastHit hitInfo, Mathf.Infinity, layerMask))
        {
            return new Vector3(hitInfo.point.x, transform.position.y, hitInfo.point.z);
        }

        return transform.forward + transform.position;
    }

    public void OnAttackModeIterationEnd()
    {
        if (selectedAbility.isTrap || selectedAbility.isOnUnitUse)
        {
            if (Physics.Raycast(CameraMain.Instance._camera.ScreenPointToRay(
                GameManager.Instance.mouse.position.ReadValue()), out RaycastHit hit, Mathf.Infinity, layerMask) &&
                GameManager.Instance.grid.GetCell(hit.point).Explorable &&
                Vector3.Distance(transform.position + Vector3.up * Player.Instance.Height / 2f, hit.point) <= currentAbilityMaxDistance)
            {
                if (selectedAbility.isOnUnitUse)
                {
                    Entity entity = hit.transform.GetComponentInParent<Entity>();

                    if(entity == null || entity.Location == null)
                    {
                        Debug.LogWarning("Entity or (and) location are set tu null: " + entity);
                        return;
                    }

                    QuadCell cell = hit.transform.GetComponentInParent<Entity>().Location;
                    target.position = cell.Position + Vector3.up * cell.Unit.Height / 2f;
                    TargetCell = cell;
                }
                else
                {
                    target.position = hit.point;
                    TargetCell = GameManager.Instance.grid.GetCell(target.position);
                }              
                
                IsValidAttackPoint = true;

                ActionUI.Instance.SetActionPointsPopup(selectedAbility.GetStatValue(AbilityStats.AbilitySpeed), hit.point);
            }
            else
            {
                IsValidAttackPoint = false;
            }

            return;
        }

        if (Physics.Raycast(CameraMain.Instance._camera.ScreenPointToRay(
            GameManager.Instance.mouse.position.ReadValue()), out RaycastHit hitInfo, Mathf.Infinity, layerMask) &&
            GameManager.Instance.grid.GetCell(hitInfo.point).Explorable)
        {
            if (Vector3.Distance(transform.position + Vector3.up * Player.Instance.Height / 2f, hitInfo.point) > currentAbilityMaxDistance)
            {
                float yPos = hitInfo.point.y;
                hitInfo.point = transform.position + Vector3.up * Player.Instance.Height / 2f 
                    + (hitInfo.point - transform.position + Vector3.up * Player.Instance.Height / 2f).normalized * currentAbilityMaxDistance;
                hitInfo.point = new Vector3(hitInfo.point.x, Mathf.Min(GameManager.Instance.grid.GetCell(hitInfo.point).Position.y, yPos), hitInfo.point.z);
            }

            if (abilityTrajectoryType == TrajectoryTypes.None)
            {
                target.position = hitInfo.point;
                TargetCell = GameManager.Instance.grid.GetCell(target.position);

                IsValidAttackPoint = true;

                ActionUI.Instance.SetActionPointsPopup(selectedAbility.GetStatValue(AbilityStats.AbilitySpeed), hitInfo.point);

                return;
            }
            else if(abilityTrajectoryType == TrajectoryTypes.Linear)
            {
                float segmentFragment = 1f / CameraMain.Instance.abilityLines.segmentsPerLine;

                Vector3 destination = hitInfo.point;

                Vector3 origin = Player.Instance.transform.position;
                origin.y += Player.Instance.Height / 2f;
                float turns = selectedAbility.GetStatValue(AbilityStats.AbilitySpeed);

                for (int i = 0; i < CameraMain.Instance.abilityLines.segmentsPerLine; i++)
                {
                    Vector3 currentDestination = Vector3.Lerp(origin, destination, segmentFragment * (i + 1));
                    if (GameManager.Instance.grid.GetCell(currentDestination).Position.y > currentDestination.y)
                    {
                        CameraMain.Instance.abilityLines.currentSegmentsPerLine = i + 1;
                        CameraMain.Instance.abilityLines.destination = destination;

                        target.position = currentDestination;
                        TargetCell = GameManager.Instance.grid.GetCell(currentDestination);

                        IsValidAttackPoint = true;
                        
                        ActionUI.Instance.SetActionPointsPopup(
                             Mathf.Min(turns, Mathf.Ceil(Vector3.Distance(origin, target.position) * turns / currentAbilityMaxDistance)), currentDestination);

                        return;
                    }
                }

                CameraMain.Instance.abilityLines.currentSegmentsPerLine = CameraMain.Instance.abilityLines.segmentsPerLine;
                CameraMain.Instance.abilityLines.destination = destination;

                target.position = destination;
                TargetCell = GameManager.Instance.grid.GetCell(destination);

                IsValidAttackPoint = true;
                
                ActionUI.Instance.SetActionPointsPopup(
                    Mathf.Min(turns, Mathf.Ceil(Vector3.Distance(origin, target.position) * turns / currentAbilityMaxDistance)), destination);
            }
            else if(abilityTrajectoryType == TrajectoryTypes.Curve)
            {
                float segmentFragment = 1f / CameraMain.Instance.abilityLines.segmentsPerLine;

                Vector3 destination = hitInfo.point;

                Vector3 origin = Player.Instance.transform.position;
                origin.y += Player.Instance.Height / 2f;

                Vector3 middle = Vector3.Lerp(origin, destination, 0.5f);
                middle.y = Mathf.Max(destination.y, origin.y) + currentAbilityCurveMiddlePoint;

                for (int i = 0; i < CameraMain.Instance.abilityLines.segmentsPerLine; i++)
                {
                    Vector3 currentDestination = Bezier.GetPoint(origin, middle, destination, segmentFragment * (i + 1));
                    if (GameManager.Instance.grid.GetCell(currentDestination).Position.y > currentDestination.y)
                    {
                        CameraMain.Instance.abilityLines.currentSegmentsPerLine = i + 1;
                        CameraMain.Instance.abilityLines.destination = destination;

                        target.position = currentDestination;
                        partialCurveLength = segmentFragment * (i + 1);
                        TargetCell = GameManager.Instance.grid.GetCell(currentDestination);
                        
                        IsValidAttackPoint = true;
                        ActionUI.Instance.SetActionPointsPopup(
                            Mathf.Ceil(Bezier.GetLength(origin, middle, hitInfo.point, partialCurveLength) * selectedAbility.GetStatValue(AbilityStats.AbilitySpeed) / fullCurveLength), currentDestination);

                        return;
                    }
                }

                CameraMain.Instance.abilityLines.currentSegmentsPerLine = CameraMain.Instance.abilityLines.segmentsPerLine;
                CameraMain.Instance.abilityLines.destination = destination;

                target.position = destination;
                partialCurveLength = 1f;
                TargetCell = GameManager.Instance.grid.GetCell(destination);

                IsValidAttackPoint = true;

                ActionUI.Instance.SetActionPointsPopup(
                    Mathf.Ceil(Bezier.GetLength(origin, middle, hitInfo.point, 1f) * selectedAbility.GetStatValue(AbilityStats.AbilitySpeed) / fullCurveLength), destination);
            }
        }
        else
        {
            IsValidAttackPoint = false;
        }
    }

    private void ApplyOnEntityAbility()
    {
        for (int i = 0; i < affectedEntities.Count; i++)
        {
            if (affectedEntities[i] != null)
            {
                GameManager.Instance.grid.ApplyOnEntityAbility(
                    GetAbilityDamage(selectedAbility.GetStatValue(AbilityStats.Power)), selectedAbility.title, (CellHazards)selectedAbility.cellHazard,
                    affectedEntities[i], GetTargetedCellsCenterLocked(selectedAbility.excludeTargetCell, affectedEntities[i].Location));
            }
        }

        affectedEntities.Clear();
    }

    private void SetTrapAbility()
    {
        for(int i = 0; i < trapCells.Count; i++)
        {
            GameManager.Instance.grid.InstantiateTrapAbility(
                (int)selectedAbility.GetStatValue(AbilityStats.AbilitySpeed), GetAbilityDamage(selectedAbility.GetStatValue(AbilityStats.Power)),
                selectedAbility.title, (CellHazards)selectedAbility.cellHazard, trapCells[i], GetTargetedCellsCenterLocked(true, trapCells[i]));
        }

        trapCells.Clear();
    }

    private void SetAbilityProjectile()
    {
        float speedMultiplier = 1f;
        int turns = (int)selectedAbility.GetStatValue(AbilityStats.AbilitySpeed);
        List<Vector3> points = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        if (abilityTrajectoryType == TrajectoryTypes.None)
        {
            points.Add(target.position);
        }
        else if (abilityTrajectoryType == TrajectoryTypes.Linear)
        {          
            points.Add(Player.Instance.transform.position + Vector3.up * Player.Instance.Height / 2f);

            if(turns > 1)
            {
                float currentTurnsCount = Mathf.Min(turns, Mathf.Ceil(Vector3.Distance(points[0], target.position) * turns / currentAbilityMaxDistance));
                Vector3 normal = (target.position - points[0]).normalized;

                for (int i = 1; i < currentTurnsCount; i++)
                {
                    points.Add(points[i - 1] + normal * currentAbilityMaxDistance / turns);
                    normals.Add((points[i] - points[i - 1]).normalized);
                }

                points.Add(target.position);
                speedMultiplier = currentAbilityMaxDistance /
                    (Vector3.Distance(points[points.Count - 1], points[points.Count - 2]) * turns);

                turns = (int)currentTurnsCount;
            }
            else
            {
                points.Add(target.position);
                speedMultiplier = currentAbilityMaxDistance /
                    (Vector3.Distance(points[points.Count - 1], points[points.Count - 2]) * turns);
            }          

            normals.Add((points[points.Count - 1] - points[points.Count - 2]).normalized);
        }
        else if(abilityTrajectoryType == TrajectoryTypes.Curve)
        {
            if(Physics.Raycast(CameraMain.Instance._camera.ScreenPointToRay(
                GameManager.Instance.mouse.position.ReadValue()), out RaycastHit hitInfo, Mathf.Infinity, layerMask))
            {
                if (Vector3.Distance(transform.position + Vector3.up * Player.Instance.Height / 2f, hitInfo.point) > currentAbilityMaxDistance)
                {
                    float yPos = hitInfo.point.y;
                    hitInfo.point = transform.position + Vector3.up * Player.Instance.Height / 2f
                        + (hitInfo.point - transform.position + Vector3.up * Player.Instance.Height / 2f).normalized * currentAbilityMaxDistance;
                    hitInfo.point = new Vector3(hitInfo.point.x, Mathf.Min(GameManager.Instance.grid.GetCell(hitInfo.point).Position.y, yPos), hitInfo.point.z);
                }

                Vector3 origin = Player.Instance.transform.position + Vector3.up * Player.Instance.Height / 2f;
                Vector3 middle = Vector3.Lerp(origin, hitInfo.point, 0.5f);
                middle.y = Mathf.Max(hitInfo.point.y, origin.y) + currentAbilityCurveMiddlePoint;

                float partialLength = Bezier.GetLength(origin, middle, hitInfo.point, partialCurveLength);

                int segmentsCount = Mathf.CeilToInt(partialLength * 100f);              
                float segmentFragment = 1f / segmentsCount;

                float fullTurnSegmentLength = fullCurveLength / turns;
                float realTurns = Mathf.Ceil(partialLength / fullTurnSegmentLength);

                if (realTurns > 1f)
                {
                    float segmentsModifier = (((fullTurnSegmentLength - Mathf.Min(partialLength - (realTurns - 1f) * fullTurnSegmentLength, fullTurnSegmentLength))
                        / (realTurns - 1f)) + fullTurnSegmentLength) / fullTurnSegmentLength;
                    int fullSegmentsCount = Mathf.FloorToInt(segmentsCount - segmentsCount / realTurns);

                    for (int i = 0; i < fullSegmentsCount; i++)
                    {
                        points.Add(Bezier.GetPoint(origin, middle, hitInfo.point, segmentFragment * segmentsModifier * i));
                        normals.Add(Bezier.GetDerivative(origin, middle, hitInfo.point, segmentFragment * segmentsModifier * i));
                    }

                    segmentsModifier = segmentFragment * segmentsModifier * (fullSegmentsCount - 1);
                    float lastSegmentModifier = (1f - segmentsModifier) / (segmentsCount - fullSegmentsCount - 1);

                    for (int i = 1; i < segmentsCount - fullSegmentsCount - 1; i++)
                    {
                        points.Add(Bezier.GetPoint(origin, middle, hitInfo.point, segmentsModifier + lastSegmentModifier * i));
                        normals.Add(Bezier.GetDerivative(origin, middle, hitInfo.point, segmentsModifier + lastSegmentModifier * i));
                    }

                    points.Add(Bezier.GetPoint(origin, middle, hitInfo.point, segmentsModifier + lastSegmentModifier * (segmentsCount - fullSegmentsCount - 1)));

                    int fullSegmentCount = Mathf.CeilToInt(fullCurveLength * 100f) / turns;
                    segmentsCount = segmentsCount - fullSegmentCount * (segmentsCount / fullSegmentCount);

                    speedMultiplier = fullSegmentCount / (float)segmentsCount;             
                }
                else
                {
                    for (int i = 0; i < segmentsCount - 1; i++)
                    {
                        points.Add(Bezier.GetPoint(origin, middle, hitInfo.point, segmentFragment * i));
                        normals.Add(Bezier.GetDerivative(origin, middle, hitInfo.point, segmentFragment * i));
                    }

                    points.Add(Bezier.GetPoint(origin, middle, hitInfo.point, segmentFragment * (segmentsCount - 1)));

                    speedMultiplier = fullTurnSegmentLength / partialLength;
                }

                turns = (int)realTurns;
            }
        }

        GameManager.Instance.grid.InstantiateAbilityProjectile(
            selectedAbility.isLeavingTrail, turns, speedMultiplier, GetAbilityDamage(selectedAbility.GetStatValue(AbilityStats.Power)), 
            selectedAbility.title, (CellHazards)selectedAbility.cellHazard, GetTargetedCells(), points, normals);
    }

    public void OnChangeCurveMiddlePointScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (AttackMode && abilityTrajectoryType == TrajectoryTypes.Curve)
            {
                float value = context.ReadValue<float>() / 120f;

                if (value > 0f)
                {
                    currentAbilityCurveMiddlePoint = Mathf.Min(currentAbilityCurveMiddlePoint + value, maxAbilityCurveMiddlePoint);
                }
                else if (value < 0f)
                {
                    currentAbilityCurveMiddlePoint = Mathf.Max(currentAbilityCurveMiddlePoint + value, minAbilityCurveMiddlePoint);
                }
            }
        }
    }

    private void SetTargetedCellsCenterLocked(bool isTargeted)
    {
        int aoeRadius = (int)Math.Ceiling(selectedAbility.GetStatValue(AbilityStats.AOERadius));

        if (aoeRadius == 0)
        {
            TargetCell.IsTargeted = isTargeted;
            return;
        }

        for (int z = Math.Max(targetCell.coordinates.Z - aoeRadius, 0);
                z < Math.Min(targetCell.coordinates.Z + aoeRadius + 1, GameManager.Instance.grid.cellCountZ - 1); z++)
        {
            for (int x = Math.Max(targetCell.coordinates.X - aoeRadius, 0);
                x < Math.Min(targetCell.coordinates.X + aoeRadius + 1, GameManager.Instance.grid.cellCountX - 1); x++)
            {
                QuadCell cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z));

                if (isTargeted)
                {
                    QuadDirection direction = TargetCell.coordinates.GetRelativeDirection(cell.coordinates);

                    if (((int)direction & 1) != 0)
                    {
                        cell.IsTargeted =
                            GameManager.Instance.grid.IsCellAffectedByBlastDiagonal(targetCell.Position, new Vector3(cell.Position.x, cell.Position.y + 1.5f * QuadMetrics.elevationStep, cell.Position.z), cell, direction);
                    }
                    else
                    {
                        cell.IsTargeted =
                            GameManager.Instance.grid.IsCellAffectedByBlastLine(targetCell.Position, TargetCell, cell, direction);
                    }
                }
                else
                {
                    cell.IsTargeted = false;
                }
            }
        }

        if (selectedAbility.excludeTargetCell)
        {
            targetCell.IsTargeted = false;
        }
    }

    private void SetTargetedCells(bool isTargeted)
    {
        int aoeRadius = (int)Math.Ceiling(selectedAbility.GetStatValue(AbilityStats.AOERadius));

        if(aoeRadius == 0)
        {
            TargetCell.IsTargeted = isTargeted;
            return;
        }

        for (int z = Math.Max(targetCell.coordinates.Z - aoeRadius, 0); 
                z < Math.Min(targetCell.coordinates.Z + aoeRadius + 1, GameManager.Instance.grid.cellCountZ - 1); z++)
        {
            for(int x = Math.Max(targetCell.coordinates.X - aoeRadius, 0);
                x < Math.Min(targetCell.coordinates.X + aoeRadius + 1, GameManager.Instance.grid.cellCountX - 1); x++)
            {
                QuadCell cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z));

                if (isTargeted && Vector3.Distance(cell.Position, target.position) <= currentAbilityAOERadius)
                {
                    QuadDirection direction = TargetCell.coordinates.GetRelativeDirection(cell.coordinates);

                    if (((int)direction & 1) != 0)
                    {
                        cell.IsTargeted =
                            GameManager.Instance.grid.IsCellAffectedByBlastDiagonal(target.position, new Vector3(cell.Position.x, cell.Position.y + 1.5f * QuadMetrics.elevationStep, cell.Position.z), cell, direction);
                    }
                    else
                    {
                        cell.IsTargeted =
                            GameManager.Instance.grid.IsCellAffectedByBlastLine(target.position, TargetCell, cell, direction);
                    }
                }
                else
                {
                    cell.IsTargeted = false;
                }
            }
        }

        if (selectedAbility.isLeavingTrail)
        {
            SetTargetedCellsInLine(isTargeted);
        }

        if (selectedAbility.excludeTargetCell)
        {
            targetCell.IsTargeted = false;
        }
    }

    private List<QuadCell> GetTargetedCellsCenterLocked(bool excludeMainCell, QuadCell mainCell)
    {
        int aoeRadius = (int)Math.Ceiling(selectedAbility.GetStatValue(AbilityStats.AOERadius));
        List<QuadCell> cells = new List<QuadCell>();
        for (int z = Math.Max(mainCell.coordinates.Z - aoeRadius, 0);
                z < Math.Min(mainCell.coordinates.Z + aoeRadius + 1, GameManager.Instance.grid.cellCountZ - 1); z++)
        {
            for (int x = Math.Max(mainCell.coordinates.X - aoeRadius, 0);
                x < Math.Min(mainCell.coordinates.X + aoeRadius + 1, GameManager.Instance.grid.cellCountX - 1); x++)
            {
                QuadCell cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z));

                QuadDirection direction = mainCell.coordinates.GetRelativeDirection(cell.coordinates);

                if (((int)direction & 1) != 0)
                {
                    if(GameManager.Instance.grid.IsCellAffectedByBlastDiagonal(mainCell.Position, new Vector3(cell.Position.x, cell.Position.y + 1.5f * QuadMetrics.elevationStep, cell.Position.z), cell, direction))
                    {
                        cells.Add(cell);
                    }
                }
                else
                {
                    if (GameManager.Instance.grid.IsCellAffectedByBlastLine(mainCell.Position  , mainCell, cell, direction))
                    {
                        cells.Add(cell);
                    }
                }
            }
        }

        if (excludeMainCell && cells.Contains(mainCell))
        {
            cells.Remove(mainCell);
        }

        return cells;
    }
    //transfer to grid
    private List<QuadCell> GetTargetedCells()
    {
        int aoeRadius = (int)Math.Ceiling(selectedAbility.GetStatValue(AbilityStats.AOERadius));
        List<QuadCell> cells = new List<QuadCell>();
        for (int z = Math.Max(targetCell.coordinates.Z - aoeRadius, 0);
                z < Math.Min(targetCell.coordinates.Z + aoeRadius + 1, GameManager.Instance.grid.cellCountZ - 1); z++)
        {
            for (int x = Math.Max(targetCell.coordinates.X - aoeRadius, 0);
                x < Math.Min(targetCell.coordinates.X + aoeRadius + 1, GameManager.Instance.grid.cellCountX - 1); x++)
            {
                QuadCell cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z));
                if (cell.IsTargeted)
                {
                    cells.Add(cell);
                }
            }
        }

        if (selectedAbility.isLeavingTrail && abilityTrajectoryType == TrajectoryTypes.None)
        {
            GetTargetedCellsInLine(cells);
        }

        if (selectedAbility.excludeTargetCell && targetCell.IsTargeted)
        {
            cells.Remove(targetCell);
        }

        return cells;
    }

    private void SetTargetedCellsInLine(bool isTargeted)
    {
        if (isTargeted)
        {
            previousTargetPosition = target.position;
        }

        if (Player.Instance.Location == targetCell)
            return;

        QuadDirection direction = Player.Instance.Location.coordinates.GetRelativeDirection(targetCell.coordinates);
        QuadCell current;

        if ((int)direction % 2 == 0)
        {
            current = Player.Instance.Location.GetNeighbor(direction);

            while (current != targetCell)
            {
                current.IsTargeted = isTargeted;

                current = current.GetNeighbor(direction);
            }

            return;
        }

        QuadDirection direction1, direction2, activeDirection;

        float angle1, angle2;
        float angleTan1, angleTan2;
        float xSign, zSign;
        Vector3 position1, position2;
        Vector3 corner1, corner2;

        if (direction == QuadDirection.SouthEast || direction == QuadDirection.NorthWest)
        {
            direction1 = direction.Next();
            direction2 = direction.Previous();
            if (direction == QuadDirection.SouthEast)
            {
                xSign = 1f;
                zSign = -1f;
            }
            else
            {
                xSign = -1f;
                zSign = 1f;
            }
            corner1 = QuadMetrics.GetSecondCorner(direction1);
            corner2 = QuadMetrics.GetFirstCorner(direction2.Previous());
        }
        else
        {
            direction1 = direction.Previous();
            direction2 = direction.Next();
            if (direction == QuadDirection.NorthEast)
            {
                xSign = 1f;
                zSign = 1f;
            }
            else
            {
                xSign = -1f;
                zSign = -1f;
            }

            corner1 = QuadMetrics.GetFirstCorner(direction1.Previous());
            corner2 = QuadMetrics.GetSecondCorner(direction2);
        }

        angleTan1 = Mathf.Abs(previousTargetPosition.x - Player.Instance.Location.Position.x) / Mathf.Abs(previousTargetPosition.z - Player.Instance.Location.Position.z);
        angleTan2 = 1f / angleTan1;
        angle1 = Mathf.Atan(angleTan1) * Mathf.Rad2Deg;
        angle2 = 90f - angle1;

        if (angle1 < angle2)
        {
            position1 = Player.Instance.Location.Position + QuadMetrics.GetFirstCorner(direction1);
            position1.x += xSign * QuadMetrics.radius * angleTan1;
            current = Player.Instance.Location.GetNeighbor(direction1);
            activeDirection = direction1;
        }
        else if (angle1 > angle2)
        {
            position1 = Player.Instance.Location.Position + QuadMetrics.GetFirstCorner(direction2);
            position1.z += zSign * QuadMetrics.radius * angleTan2;
            current = Player.Instance.Location.GetNeighbor(direction2);
            activeDirection = direction2;
        }
        else
        {
            current = Player.Instance.Location.GetNeighbor(direction);

            while (current != targetCell)
            {
                current.IsTargeted = isTargeted;

                current = current.GetNeighbor(direction);
            }

            return;
        }

        if (activeDirection == direction1)
        {
            position2 = current.Position + corner2;

            if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.x - position2.x)) * Mathf.Rad2Deg)
            {
                activeDirection = direction2;
                position2.z += zSign * angleTan2 * Mathf.Abs(position1.x - position2.x);
            }
            else
            {
                position2 = position1;
                position2.x += xSign * 2f * QuadMetrics.radius / angleTan2;
                position2.z += zSign * 2f * QuadMetrics.radius;
            }
        }
        else
        {
            position2 = current.Position + corner1;

            if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.z - position2.z)) * Mathf.Rad2Deg)
            {
                activeDirection = direction1;
                position2.x += xSign * angleTan1 * Mathf.Abs(position1.z - position2.z);
            }
            else
            {
                position2 = position1;
                position2.z += zSign * 2f * QuadMetrics.radius / angleTan1;
                position2.x += xSign * 2f * QuadMetrics.radius;
            }
        }

        position1 = position2;
        current.IsTargeted = isTargeted;
        current = current.GetNeighbor(activeDirection);

        while (current != targetCell)
        {
            if (activeDirection == direction1)
            {
                position2 = current.Position + corner2;

                if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.x - position2.x)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction2;
                    position2.z += zSign * angleTan2 * Mathf.Abs(position1.x - position2.x);
                }
                else
                {
                    position2 = position1;
                    position2.x += xSign * 2f * QuadMetrics.radius / angleTan2;
                    position2.z += zSign * 2f * QuadMetrics.radius;
                }
            }
            else
            {
                position2 = current.Position + corner1;

                if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.z - position2.z)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction1;
                    position2.x += xSign * angleTan1 * Mathf.Abs(position1.z - position2.z);
                }
                else
                {
                    position2 = position1;
                    position2.z += zSign * 2f * QuadMetrics.radius / angleTan1;
                    position2.x += xSign * 2f * QuadMetrics.radius;
                }
            }

            position1 = position2;
            current.IsTargeted = isTargeted;
            current = current.GetNeighbor(activeDirection);
        }
    }

    private void GetTargetedCellsInLine(List<QuadCell> cells)
    {
        if (Player.Instance.Location == targetCell)
            return;

        QuadDirection direction = Player.Instance.Location.coordinates.GetRelativeDirection(targetCell.coordinates);
        QuadCell current;

        if ((int)direction % 2 == 0)
        {
            current = Player.Instance.Location.GetNeighbor(direction);

            while (current != targetCell)
            {
                if (!cells.Contains(current))
                {
                    cells.Add(current);
                }

                current = current.GetNeighbor(direction);
            }

            return;
        }

        QuadDirection direction1, direction2, activeDirection;

        float angle1, angle2;
        float angleTan1, angleTan2;
        float xSign, zSign;
        Vector3 position1, position2;
        Vector3 corner1, corner2;

        if (direction == QuadDirection.SouthEast || direction == QuadDirection.NorthWest)
        {
            direction1 = direction.Next();
            direction2 = direction.Previous();
            if (direction == QuadDirection.SouthEast)
            {
                xSign = 1f;
                zSign = -1f;
            }
            else
            {
                xSign = -1f;
                zSign = 1f;
            }
            corner1 = QuadMetrics.GetSecondCorner(direction1);
            corner2 = QuadMetrics.GetFirstCorner(direction2.Previous());
        }
        else
        {
            direction1 = direction.Previous();
            direction2 = direction.Next();
            if (direction == QuadDirection.NorthEast)
            {
                xSign = 1f;
                zSign = 1f;
            }
            else
            {
                xSign = -1f;
                zSign = -1f;
            }

            corner1 = QuadMetrics.GetFirstCorner(direction1.Previous());
            corner2 = QuadMetrics.GetSecondCorner(direction2);
        }

        angleTan1 = Mathf.Abs(target.position.x - Player.Instance.Location.Position.x) / Mathf.Abs(target.position.z - Player.Instance.Location.Position.z);
        angleTan2 = 1f / angleTan1;
        angle1 = Mathf.Atan(angleTan1) * Mathf.Rad2Deg;
        angle2 = 90f - angle1;

        if (angle1 < angle2)
        {
            position1 = Player.Instance.Location.Position + QuadMetrics.GetFirstCorner(direction1);
            position1.x += xSign * QuadMetrics.radius * angleTan1;
            current = Player.Instance.Location.GetNeighbor(direction1);
            activeDirection = direction1;
        }
        else if (angle1 > angle2)
        {
            position1 = Player.Instance.Location.Position + QuadMetrics.GetFirstCorner(direction2);
            position1.z += zSign * QuadMetrics.radius * angleTan2;
            current = Player.Instance.Location.GetNeighbor(direction2);
            activeDirection = direction2;
        }
        else
        {
            current = Player.Instance.Location.GetNeighbor(direction);

            while (current != targetCell)
            {
                if (!cells.Contains(current))
                {
                    cells.Add(current);
                }

                current = current.GetNeighbor(direction);
            }

            return;
        }

        if (activeDirection == direction1)
        {
            position2 = current.Position + corner2;

            if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.x - position2.x)) * Mathf.Rad2Deg)
            {
                activeDirection = direction2;
                position2.z += zSign * angleTan2 * Mathf.Abs(position1.x - position2.x);
            }
            else
            {
                position2 = position1;
                position2.x += xSign * 2f * QuadMetrics.radius / angleTan2;
                position2.z += zSign * 2f * QuadMetrics.radius;
            }
        }
        else
        {
            position2 = current.Position + corner1;

            if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.z - position2.z)) * Mathf.Rad2Deg)
            {
                activeDirection = direction1;
                position2.x += xSign * angleTan1 * Mathf.Abs(position1.z - position2.z);
            }
            else
            {
                position2 = position1;
                position2.z += zSign * 2f * QuadMetrics.radius / angleTan1;
                position2.x += xSign * 2f * QuadMetrics.radius;
            }
        }

        position1 = position2;
        if (!cells.Contains(current))
        {
            cells.Add(current);
        }
        current = current.GetNeighbor(activeDirection);

        while (current != targetCell)
        {
            if (activeDirection == direction1)
            {
                position2 = current.Position + corner2;

                if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.x - position2.x)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction2;
                    position2.z += zSign * angleTan2 * Mathf.Abs(position1.x - position2.x);
                }
                else
                {
                    position2 = position1;
                    position2.x += xSign * 2f * QuadMetrics.radius / angleTan2;
                    position2.z += zSign * 2f * QuadMetrics.radius;
                }
            }
            else
            {
                position2 = current.Position + corner1;

                if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.z - position2.z)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction1;
                    position2.x += xSign * angleTan1 * Mathf.Abs(position1.z - position2.z);
                }
                else
                {
                    position2 = position1;
                    position2.z += zSign * 2f * QuadMetrics.radius / angleTan1;
                    position2.x += xSign * 2f * QuadMetrics.radius;
                }
            }

            position1 = position2;
            if (!cells.Contains(current))
            {
                cells.Add(current);
            }
            current = current.GetNeighbor(activeDirection);
        }
    }

    public void ExitAttackMode()
    {
        AttackMode = false;
        selectedAbility = null;       
    }

    public bool IsWaitingForExecuteTurn()
    {
        if (stopAction && !GameManager.Instance.stopCurrentAction)
        {
            Player.Instance.ActionPoints = 1f;
            GameManager.Instance.stopCurrentAction = true;
        }

        if (GetCastTime(selectedAbility.GetStatValue(AbilityStats.CastTime)) - 1f > Player.Instance.SpentActionPoints)
        {
            Player.Instance.SpentActionPoints++;
            return true;
        }
        else
        {
            if(selectedAbility.isTrap || selectedAbility.isOnUnitUse)
            {
                if (selectedAbility.isTrap)
                {
                    SetTrapAbility();
                }
                else
                {
                    ApplyOnEntityAbility();
                }

                attackMode = false;              
                IsValidAttackPoint = false;
                Player.Instance.SpentActionPoints++;

                return false;
            }

            attackMode = true;
            if (attackModeCommand != null)
            {
                attackModeCommand.Execute();
            }

            return false;
        }
    }

    public bool WaitingIterationEnd()
    {
        return GameManager.Instance.stopCurrentAction;
    }

    public void CancelAttack()
    {
        trapCells.Clear();
        affectedEntities.Clear();

        Casting = false;
        stopAction = false;
        selectedAbility = null;
        Player.Instance.InAction = false;
    }

    public bool ExecuteAttack()
    {
        return attackMode;
    }

    public void AttackAreaEnd()
    {
        Casting = false;
        stopAction = false;
        attackMode = false;
        Player.Instance.activeAbilitiesCooldowns[selectedAbility] = SetAbilityCooldown();
        if (GameUI.Instance)
        {
            GameUI.Instance.SetCoolDownToAbility(1f, selectedAbility);
        }
        selectedAbility = null;
        Player.Instance.InAction = false;
    }

    private float SetAbilityCooldown()
    {
        return Mathf.Round(selectedAbility.GetStatValue(AbilityStats.Cooldown) / PlayerInfo.GetCooldownTime());
    }

    private float GetAbilityDamage(float rawDamage)
    {
        if(Random.Range(0f, 100f) > PlayerInfo.GetCritChance())
        {
            return rawDamage * PlayerInfo.GetDamageMultiplier();
        }
        else
        {
            return (1f + PlayerInfo.GetCritDamage()) * rawDamage * PlayerInfo.GetDamageMultiplier();
        }      
    }

    private float GetCastTime(float rawCastTime)
    {
        return Mathf.Round(rawCastTime / PlayerInfo.GetCastTime());
    }
}
