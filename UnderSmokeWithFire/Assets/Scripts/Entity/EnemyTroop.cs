using System.Collections.Generic;
using UnityEngine;

public class EnemyTroop 
{
    public int zoneIndex = -1;
    public List<Enemy> units = new List<Enemy>();

    public static int meleeIndex = 1;
    public static int rangeIndex = 2;
    public static int reinforcementMeleeCount = 3;
    public static int reinforcementRangeCount = 1;

    public bool IsReinforcements
    {
        get
        {
            return isReinforcements;
        }
        set
        {
            if (isReinforcements == value)
                return;

            isReinforcements = value;

            if (isReinforcements)
            {
                for(int i = 0; i < units.Count; i++)
                {
                    units[i].CurrentBehavior = AIBehaviors.Reinforce;
                }
            }
        }
    }

    private bool isReinforcements;

    public bool IsPlayerDetected
    {
        get
        {
            return isPlayerDetected;
        }
        set
        {
            if (isPlayerDetected == value)
                return;

            isPlayerDetected = value;

            if (isPlayerDetected)
            {
                if (!IsReinforcements)
                {
                    GameManager.Instance.grid.camps.Find(x => x.zoneIndex == zoneIndex).UnderAttack = true;
                    //units some attack some defendposition
                    foreach (var unit in units)
                    {
                        unit.lastSeenPlayerLocation = Player.Instance.Location;
                        if (unit.fov.CanSeePlayer)
                        {
                            unit.CurrentBehavior = AIBehaviors.Attack;
                        }
                        else
                        {
                            unit.CurrentBehavior = unit.priorityBehaviour;
                        }
                    }

                    Player.Instance.IsDetected = true;
                }
                else
                {
                    foreach (var unit in units)
                    {
                        unit.lastSeenPlayerLocation = Player.Instance.Location;
                        if (unit.fov.CanSeePlayer)
                        {
                            unit.CurrentBehavior = AIBehaviors.Attack;                         
                        }
                        else
                        {
                            unit.CurrentBehavior = AIBehaviors.Pursue;
                        }              
                    }

                    Player.Instance.IsDetected = true;
                }
            }
            else
            {
                if (!isReinforcements)
                {
                    foreach (var unit in units)
                    {
                        unit.CurrentBehavior = unit.priorityBehaviour;
                        unit.lastSeenPlayerLocation = Player.Instance.Location;
                        if (Player.Instance.Location != Player.Instance.PreviousLocation)
                        {
                            unit.hasSeenWherePlayerMoved = true;
                            unit.lastSeenMoveDirection = unit.lastSeenPlayerLocation.coordinates.
                                GetRelativeDirection(Player.Instance.Location.coordinates);
                        }
                        else
                        {
                            unit.hasSeenWherePlayerMoved = false;
                        }
                    }
                }
            }
        }
    }

    private bool isPlayerDetected;

    public int UnitsCanSeePlayer
    {
        get
        {
            return unitsCanSeePlayer;
        }
        set
        {
            unitsCanSeePlayer = value;

            if (unitsCanSeePlayer == 0)
            {
                IsPlayerDetected = false;
            }
        }
    }

    private int unitsCanSeePlayer;

    public void RefreshReinforcement()
    {
        if (!units.Exists(unit => unit.CurrentBehavior != AIBehaviors.Wait))
        {
            foreach (var unit in units)
            {
                unit.controls.currentTurnsWaited = 0;
                unit.CurrentBehavior = AIBehaviors.Reinforce;
            }
        }
    }

    public void CreateReinforcementGroupFromCamp(int zoneIndex)
    {
        if (units.Count < 5)
            return;

        EnemyTroop reinforcements = new EnemyTroop();

        for(int i = 0; i < 2; i++)
        {
            int index = Random.Range(0, units.Count);
            reinforcements.units.Add(units[index]);
            units[index].AssignedDefencePositionIndex = -1;
            units[index].enemyTroop = reinforcements;           
            units.RemoveAt(index);
        }

        reinforcements.zoneIndex = zoneIndex;
        reinforcements.IsReinforcements = true;
    }

    public static void CreateReinforcementGroupFromSpawn(int spawnZoneIndex, int defenceZoneIndex)
    {
        EnemyTroop reinforcements = new EnemyTroop();
        SpecialZone spawn = GameManager.Instance.grid.specialZones[spawnZoneIndex];

        for(int i = 0; i < reinforcementMeleeCount; i++)
        {           
            GameManager.Instance.grid.AddUnit(Random.Range(0f, 360f), GameManager.Instance.grid.unitPrefabs[meleeIndex], spawn.GetRandomCell(null, false, true));

            Enemy unit = (Enemy)GameManager.Instance.grid.units[GameManager.Instance.grid.units.Count - 1];
            reinforcements.units.Add(unit);
            unit.AssignedDefencePositionIndex = -1;
            unit.enemyTroop = reinforcements;

            if (unit.Location.Explorable && Player.Instance)
            {
                GameManager.Instance.grid.CheckCellVisible(unit.Location, unit.Height);
            }
        }

        for (int i = 0; i < reinforcementRangeCount; i++)
        {
            GameManager.Instance.grid.AddUnit(Random.Range(0f, 360f), GameManager.Instance.grid.unitPrefabs[rangeIndex], spawn.GetRandomCell(null, false, true));
            
            Enemy unit = (Enemy)GameManager.Instance.grid.units[GameManager.Instance.grid.units.Count - 1];
            reinforcements.units.Add(unit);
            unit.AssignedDefencePositionIndex = -1;
            unit.enemyTroop = reinforcements;

            if (unit.Location.Explorable && Player.Instance)
            {
                GameManager.Instance.grid.CheckCellVisible(unit.Location, unit.Height);
            }
        }

        GameManager.Instance.grid.OrderUnits();

        reinforcements.zoneIndex = defenceZoneIndex;
        reinforcements.IsReinforcements = true;
    }
}
