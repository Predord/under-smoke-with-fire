using System.Collections.Generic;
using UnityEngine;

public abstract class AIControls : MonoBehaviour
{
    public int patrolTurnsToWait = 2;
    public int pursueTurnsToWait = 1;
    public int searchTurnsToWait = 2;
    public int waitInPositionTurns = 5;
    public int searchZoneSize = 18;
    public int currentTurnsWaited = 0;

    public Command activeCommand;
    public Command noActionCommand;
    public Command moveCommand;
    //public Command climbCommand;
    public Command attackCommand;
    public QuadCell cellToTravel;

    protected bool previousCellsVisibility;
    //protected int pathLength;
    protected int currentPatrolIndex = 1;   
    protected QuadCell previousCell;
    protected QuadCell lockedForCoverCell;
    protected List<QuadCell> path = new List<QuadCell>();
    protected Enemy enemy;

    public abstract void SetActiveAction();

    protected virtual void GetPath(bool ignoreUnit, bool checkLockedCover)
    {
        //check if cell occupied
        if (GameManager.Instance.grid.FindDistanceHeuristic(enemy, enemy.Location, cellToTravel, true, false, true, checkLockedCover))
        {
            if (!ignoreUnit && cellToTravel.Unit)
            {
                path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel.PathFrom);
            }
            else
            {
                path = GameManager.Instance.grid.FindEnemyPath(enemy.Location, cellToTravel);
            }

            activeCommand = moveCommand;
        }
        else
        {
            activeCommand = noActionCommand;
            Debug.LogWarning("Impossible to reach location: " + cellToTravel.coordinates.ToString());
        }
    }
}
