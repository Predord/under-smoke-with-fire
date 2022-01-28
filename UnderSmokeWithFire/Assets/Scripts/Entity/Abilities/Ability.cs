using System;
using UnityEngine;

[Serializable]
public class Ability 
{
    public int id;
    public string title;

    public bool isTrap;
    public bool isLeavingTrail;
    public bool isOnUnitUse;
    public bool excludeTargetCell;      
    public int targetedEntitiesMask;
    public int afterCastAbilityIndex;
    public int trajectoryType;
    public int cellHazard;
    public int projectilesCount;
    public float[] stats = new float[Enum.GetNames(typeof(AbilityStats)).Length];
    public Sprite icon;

    public static int maxRank = 3;

    public static float[][] rankModifiers = {
        new float[] {1.25f, 1.0f, 1.0f, 1.5f, 1.25f, 1.0f},
        new float[] {1.5f, 0.5f, 0.75f, 1.75f, 1.5f, 1.0f},
        new float[] {2f, 0.5f, 0.5f, 2f, 1.75f, 0.5f}
    };

    public int Rank
    {
        get
        {
            return Math.Min(rank, PlayerInfo.GetMaxAbilityLevel());
        }
        set
        {
            rank = Math.Min(value, PlayerInfo.GetMaxAbilityLevelUnchanged());
        }
    }

    private int rank;

    public Ability(int id, string title, bool isTrap, bool isLeavingTrail, bool excludeTargetCell, bool isOnUnitUse,
         int targetedEntitiesMask, int afterCastAbilityIndex, int trajectoryType, int cellHazard, int projectilesCount, float[]stats, int rank, Sprite icon)
    {
        this.id = id;
        this.title = title;
        this.isTrap = isTrap;
        this.isLeavingTrail = isLeavingTrail;
        this.excludeTargetCell = excludeTargetCell;
        this.isOnUnitUse = isOnUnitUse;
        this.targetedEntitiesMask = targetedEntitiesMask;
        this.afterCastAbilityIndex = afterCastAbilityIndex;
        this.trajectoryType = trajectoryType;
        this.cellHazard = cellHazard;
        this.projectilesCount = projectilesCount;
        this.stats = stats;
        Rank = rank;
        this.icon = icon;
    }

    public float GetStatValue(AbilityStats stat)
    {
        if(Rank != 0)
        {
            return Mathf.CeilToInt(stats[(int)stat] * rankModifiers[Rank - 1][(int)stat]);
        }
        else
        {
            return stats[(int)stat];
        }       
    }
}
