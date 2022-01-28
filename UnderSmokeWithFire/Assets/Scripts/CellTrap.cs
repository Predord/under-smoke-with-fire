using System.Collections.Generic;
using UnityEngine;

public class CellTrap
{
    public GameObject explosion;
    public QuadCell mainCell;

    private bool isActive;
    private int duration;
    private float trapDamage;
    private CellHazards hazard;    
    private List<QuadCell> targetedCells;

    public CellTrap(int duration, float trapDamage, string title, CellHazards hazard, QuadCell mainCell, List<QuadCell> targetedCells)
    {
        this.duration = duration;
        this.trapDamage = trapDamage;
        this.hazard = hazard;
        this.mainCell = mainCell;
        this.targetedCells = targetedCells;

        explosion = Resources.Load("Abilities/Effects/" + title + "Explosion") as GameObject;

        if(explosion == null)
        {
            Debug.LogWarning("Explosion anim set to null");
        }
    }

    public bool CheckForEntity()
    {
        if (isActive)
            return false;

        if(duration > 0)
        {
            duration--;
        }
        else
        {
            if (mainCell.Unit != null) 
                mainCell.Unit.Health -= trapDamage;

            mainCell.CellHazard = hazard;

            for (int i = 0; i < targetedCells.Count; i++)
            {
                if (targetedCells[i].Unit)
                {
                    targetedCells[i].Unit.Health -= trapDamage;
                }

                targetedCells[i].CellHazard = hazard;
            }

            isActive = true;
            return true;      
        }

        if(mainCell.Unit != null)
        {
            mainCell.Unit.Health -= trapDamage;
            mainCell.CellHazard = hazard;

            for (int i = 0; i < targetedCells.Count; i++)
            {
                if(targetedCells[i].Unit)
                {
                    targetedCells[i].Unit.Health -= trapDamage;
                }

                targetedCells[i].CellHazard = hazard;
            }

            isActive = true;
            return true;
        }

        return false;
    }
}
