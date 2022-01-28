using System.Collections.Generic;
using System.Linq;
using System.IO;

public class EnemyCamp
{
    public int zoneIndex;
    public EnemyTroop campTroop;
    public List<int> zonesToGetReinforcementsFrom = new List<int>();
    public Dictionary<int, int> reinforcementsZones = new Dictionary<int, int>();

    public EnemyCamp(int zoneIndex)
    {
        this.zoneIndex = zoneIndex;
        campTroop = new EnemyTroop();
        campTroop.zoneIndex = zoneIndex;
    }

    public bool IsAlerted
    {
        get
        {
            return isAlerted;
        }
        set
        {
            if (isAlerted == value)
                return;

            isAlerted = value;

            //increase vision
        }
    }

    private bool isAlerted;

    public bool UnderAttack
    {
        get
        {
            return underAttack;
        }
        set
        {
            if (underAttack == value)
                return;

            underAttack = value;
            IsAlerted = true;

            if (underAttack)
            {
                Player.Instance.OnSpentActionPoint += AlertIncrease;

                foreach (var unit in campTroop.units)
                {
                    unit.CurrentBehavior = unit.priorityBehaviour;
                }
            }
            else
            {
                Player.Instance.OnSpentActionPoint -= AlertIncrease;
                AlertIncrease();
            }
        }
    }

    private bool underAttack;

    public int TurnsForAlert
    {
        get
        {
            return turnsForAlert;
        }
        set
        {
            if (turnsForAlert == value)
                return;

            turnsForAlert = value;

            foreach(var index in reinforcementsZones.Keys.ToList())
            {
                int campIndex = GameManager.Instance.grid.camps.FindIndex(x => x.zoneIndex == index);
                //add cooldown as property
                if (reinforcementsZones[index] <= turnsForAlert && (campIndex == -1 || !GameManager.Instance.grid.camps[campIndex].UnderAttack) && !zonesToGetReinforcementsFrom.Any(zone => zone == zoneIndex))
                {
                    zonesToGetReinforcementsFrom.Add(zoneIndex);

                    if (GameManager.Instance.grid.specialZones[index].zoneType == SpecialZoneType.DefencePosition)
                    {                        
                        GameManager.Instance.grid.camps[campIndex].IsAlerted = true;
                        GameManager.Instance.grid.camps[campIndex].campTroop.CreateReinforcementGroupFromCamp(zoneIndex);
                    }
                    else if(GameManager.Instance.grid.specialZones[index].zoneType == SpecialZoneType.EnemySpawn)
                    {
                        EnemyTroop.CreateReinforcementGroupFromSpawn(index, zoneIndex);
                    }
                }
            }
        }
    }

    private int turnsForAlert;

    private void AlertIncrease()
    {
        TurnsForAlert += 1;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)zoneIndex);
        writer.Write((byte)reinforcementsZones.Count);

        foreach(var zone in reinforcementsZones)
        {
            writer.Write((byte)zone.Key);
            writer.Write(zone.Value);
        }
    }

    public static void Load(BinaryReader reader, int header)
    {
        EnemyCamp enemyCamp = new EnemyCamp(reader.ReadByte());
        int reinforcementsZonesCount = reader.ReadByte();

        for (int i = 0; i < reinforcementsZonesCount; i++)
        {
            enemyCamp.reinforcementsZones.Add(reader.ReadByte(), reader.ReadInt32());
        }

        GameManager.Instance.grid.camps.Add(enemyCamp);
    }
}
