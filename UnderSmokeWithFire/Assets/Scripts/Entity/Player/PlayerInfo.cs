using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;

public static class PlayerInfo
{
    public const int hotBarMaxSlots = 32;
    public static int currentLevel;
    public static int currentSpentStatPoints;
    public static event Action OnLevelUp;
    public static event Action OnHealthChange;
    public static event Action OnFatigueChange;
    public static event Action<StatData> OnStatChange;

    public static List<StatData> stats = new List<StatData>();

    public static List<Ability> characterAbilities = new List<Ability>();
    public static List<Ability> hotBarAbilities = new List<Ability>();
    public static List<Ability> activeAbilities = new List<Ability>();

    public static List<BuffDebuff> characterBuffsDebuffs = new List<BuffDebuff>();

    private static int fatigueBuffDebuffIndex = -1;
    private static float[] fatigueThresholds = 
    { 
        0f, 
        0.1f * StatConstants.normalMaxFatigue, 
        0.4f * StatConstants.normalMaxFatigue, 
        0.6f * StatConstants.normalMaxFatigue, 
        0.8f * StatConstants.normalMaxFatigue, 
        StatConstants.normalMaxFatigue, 
        StatConstants.normalMaxFatigue + 1f 
    };

    public static bool HasScoutedArea
    {
        get
        {
            return hasScoutedArea;
        }
        set
        {
            hasScoutedArea = value;

            if (hasScoutedArea)
            {
                GiveBuffDebuff("ScoutPreparation");
            }
        }
    }

    private static bool hasScoutedArea;

    public static float Health
    {
        get
        {
            return health;
        }
        set
        {
            if (value == health)
                return;

            health = Mathf.Clamp(value, 0f, GetMaxHealth());

            OnHealthChange?.Invoke();
        }
    }

    public static float health;

    public static float Fatigue
    {
        get
        {
            return fatigue;
        }
        set
        {
            if (value == fatigue)
                return;

            value = Mathf.Clamp(value, 0f, StatConstants.normalMaxFatigue);
            if(value < Math.Truncate(fatigueThresholds[1]))
            {
                if(fatigueBuffDebuffIndex != 0)
                {
                    if (fatigueBuffDebuffIndex != -1)
                    {
                        RemoveBuffDebuff(fatigueBuffDebuffIndex);
                    }

                    fatigueBuffDebuffIndex = 0;
                    GiveBuffDebuff(0);
                }
            }
            else if(value >= Math.Truncate(fatigueThresholds[2]))
            {
                for (int i = 2; i < fatigueThresholds.Length - 1; i++)
                {
                    if (value < Math.Truncate(fatigueThresholds[i + 1]))
                    {
                        if (fatigueBuffDebuffIndex != i - 1)
                        {
                            if (fatigueBuffDebuffIndex != -1)
                            {
                                RemoveBuffDebuff(fatigueBuffDebuffIndex);
                            }

                            fatigueBuffDebuffIndex = i - 1;
                            GiveBuffDebuff(i - 1);
                        }

                        break;
                    }
                }
            }
            else if(fatigueBuffDebuffIndex != -1)
            {
                RemoveBuffDebuff(fatigueBuffDebuffIndex);
                fatigueBuffDebuffIndex = -1;               
            }

            fatigue = value;

            OnFatigueChange?.Invoke();
        }
    }

    private static float fatigue;

    public static void InitializePlayersInfo()
    {
        currentLevel = StatConstants.defaultLevel;

        InitializeStats();
        InitializeHotBarList();

        Health = StatConstants.normalMaxHealth;
        Fatigue = 10f;

        //temp
        GiveAbility(0, 0);
        GiveAbility(1, 0);
        GiveAbility(2, 0);
    }

    private static void InitializeStats()
    {
        stats.Clear();

        var allStatsTypes = Assembly.GetAssembly(typeof(StatData)).GetTypes()
                        .Where(s => typeof(StatData).IsAssignableFrom(s) && s.IsAbstract == false);

        foreach (var statType in allStatsTypes)
        {
            StatData stat = Activator.CreateInstance(statType, StatConstants.defaultStats) as StatData;
            stats.Add(stat);
        }

        stats.Sort((x, y) => x == null ? (y == null ? 0 : -1) : (y == null ? 1 : x.StatType.CompareTo(y.StatType)));
    }

    private static void InitializeHotBarList()
    {
        for (int i = 0; i < hotBarMaxSlots; i++)
            hotBarAbilities.Add(null);
    }

    public static void GetPlayersInfo()
    {
        if (Player.Instance)
        {
            Player.Instance.Health = Health;
            Player.Instance.Fatigue = fatigue;
            Player.Instance.dodge = GetDodge();

            Player.Instance.activeAbilitiesCooldowns.Clear();

            foreach(var ability in activeAbilities)
            {
                Player.Instance.activeAbilitiesCooldowns.Add(ability, 0f);
            }
        }
    }

    public static void LevelUp()
    {
        currentLevel++;

        OnLevelUp?.Invoke();
    }

    public static void GiveAbility(int id, int abilityRank)
    {
        if (characterAbilities.Any(ability => ability.id == id))
            return;

        Ability abilityToAdd = AbilityDatabase.GetAbility(id);
        abilityToAdd.Rank = abilityRank;
        characterAbilities.Add(abilityToAdd);

        if (GameUI.Instance)
        {
            GameUI.Instance.HandleAbilityAdd(abilityToAdd);
        }
    }

    public static void GiveAbility(string abilityName, int abilityRank)
    {
        if (characterAbilities.Any(ability => ability.title == abilityName))
            return;

        Ability abilityToAdd = AbilityDatabase.GetAbility(abilityName);
        abilityToAdd.Rank = abilityRank;
        characterAbilities.Add(abilityToAdd);

        if (GameUI.Instance)
        {
            GameUI.Instance.HandleAbilityAdd(abilityToAdd);
        }
    }
    //add handlestatchange
    public static void GiveBuffDebuff(int id)
    {
        if (characterBuffsDebuffs.Any(buffDebuff => buffDebuff.id == id))
            return;

        BuffDebuff buffDebuffToAdd = BuffsDebuffsDatabase.GetBuffDebuff(id);
        characterBuffsDebuffs.Add(buffDebuffToAdd);

        if (GameUI.Instance)
        {
            GameUI.Instance.HandleBuffDebuffAdd(buffDebuffToAdd);
            foreach (var stat in stats)
            {
                OnStatChange?.Invoke(stat);
            }
        }
    }
    //add handlestatchange
    public static void GiveBuffDebuff(string buffDebuffName)
    {
        if (characterBuffsDebuffs.Any(buffDebuff => buffDebuff.title == buffDebuffName))
            return;

        BuffDebuff buffDebuffToAdd = BuffsDebuffsDatabase.GetBuffDebuff(buffDebuffName);
        characterBuffsDebuffs.Add(buffDebuffToAdd);

        if (GameUI.Instance)
        {
            GameUI.Instance.HandleBuffDebuffAdd(buffDebuffToAdd);
            foreach(var stat in stats)
            {
                OnStatChange?.Invoke(stat);
            }
        }
    }

    public static void RemoveBuffDebuff(int id)
    {
        int index = characterBuffsDebuffs.FindIndex(buffDebuff => buffDebuff.id == id);        

        if (GameUI.Instance)
        {
            GameUI.Instance.HandleBuffDebuffRemove(characterBuffsDebuffs[index]);
            characterBuffsDebuffs.RemoveAt(index);

            foreach (var stat in stats)
            {
                OnStatChange?.Invoke(stat);
            }
        }
        else
        {
            characterBuffsDebuffs.RemoveAt(index);
        }
    }

    public static void RemoveBuffDebuff(string buffDebuffName)
    {
        int index = characterBuffsDebuffs.FindIndex(buffDebuff => buffDebuff.title == buffDebuffName);

        if (GameUI.Instance)
        {
            GameUI.Instance.HandleBuffDebuffRemove(characterBuffsDebuffs[index]);
            characterBuffsDebuffs.RemoveAt(index);

            foreach (var stat in stats)
            {
                OnStatChange?.Invoke(stat);
            }
        }
        else
        {
            characterBuffsDebuffs.RemoveAt(index);
        }
    }

    public static void ApplyStatChange(StatData statData)
    {
        OnStatChange?.Invoke(statData);
    }

    public static int GetStatValue(StatType statType)
    {
        int value = GetStatValueUnchanged(statType);

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.statsModifiers[(int)statType];
        }

        return Math.Max(StatData.minValue, value);
    }

    public static int GetStatValueUnchanged(StatType statType)
    {
        return stats.First(s => s.StatType == statType).Value;
    }

    public static float[] GetResists()
    {
        float[] values = GetResistsUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            for(int i = 0; i < Enum.GetNames(typeof(NegativeEffects)).Length; i++)
            {
                values[i] += buffDebuff.resistsModifiers[i];
            }

            if(buffDebuff.statsModifiers[(int)StatType.Constitution] != 0)
            {
                for(int i = 0; i < Enum.GetNames(typeof(NegativeEffects)).Length; i++)
                {
                    values[i] += ((Constitution)stats.First(s => s.StatType == StatType.Constitution)).
                        GetResistsValueDifference(GetStatValue(StatType.Constitution) - stats[(int)StatType.Constitution].Value);
                }
            }
        }

        return values;
    }

    public static float[] GetResistsUnchanged()
    {
        return ((Constitution)stats.First(s => s.StatType == StatType.Constitution)).GetResists(); 
    }

    public static float GetMaxHealth()
    {
        float value = GetMaxHealthUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.maxHealthModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Constitution] != 0)
            {
                value += ((Constitution)stats.First(s => s.StatType == StatType.Constitution)).
                    GetMaxHealthValueDifference(GetStatValue(StatType.Constitution) - stats[(int)StatType.Constitution].Value);
            }
        }

        return value;
    }

    public static float GetMaxHealthUnchanged()
    {
        return ((Constitution)stats.First(s => s.StatType == StatType.Constitution)).GetMaxHealth();
    }

    public static float GetDamageMultiplier()
    {
        float value = GetDamageMultiplierUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.damageModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Intelligence] != 0)
            {
                value += ((Intelligence)stats.First(s => s.StatType == StatType.Intelligence)).
                    GetDamageMultiplierValueDifference(GetStatValue(StatType.Intelligence) - stats[(int)StatType.Intelligence].Value);
            }
        }

        return value;
    }

    public static float GetDamageMultiplierUnchanged()
    {
        return ((Intelligence)stats.First(s => s.StatType == StatType.Intelligence)).GetDamageMultiplier();
    }

    public static int GetMaxAbilityLevel()
    {
        int value = GetMaxAbilityLevelUnchanged();

        foreach(var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.maxAbilityLevelModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Intelligence] != 0)
            {
                value += ((Intelligence)stats.First(s => s.StatType == StatType.Intelligence)).
                    GetMaxAbilityLevelValueDifference(GetStatValue(StatType.Intelligence) - stats[(int)StatType.Intelligence].Value, GetStatValue(StatType.Intelligence));
            }
        }

        return Math.Max(0, value);
    }

    public static int GetMaxAbilityLevelUnchanged()
    {
        return ((Intelligence)stats.First(s => s.StatType == StatType.Intelligence)).GetMaxAbilityLevel();
    }

    public static int GetDodge()
    {
        int value = GetDodgeUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.dodgeModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Speed] != 0)
            {
                value += ((Speed)stats.First(s => s.StatType == StatType.Speed)).
                    GetDodgeValueDifference(GetStatValue(StatType.Speed) - stats[(int)StatType.Speed].Value, GetStatValue(StatType.Speed));
            }
        }

        return value;
    }

    public static int GetDodgeUnchanged()
    {
        return ((Speed)stats.First(s => s.StatType == StatType.Speed)).GetDodge();
    }

    public static float GetCastTime()
    {
        float value = GetCastTimeUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.castTimeModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Speed] != 0)
            {
                value += ((Speed)stats.First(s => s.StatType == StatType.Speed)).
                    GetCastTimeValueDifference(GetStatValue(StatType.Speed) - stats[(int)StatType.Speed].Value, GetStatValue(StatType.Speed));
            }
        }

        return value;
    }

    public static float GetCastTimeUnchanged()
    {
        return ((Speed)stats.First(s => s.StatType == StatType.Speed)).GetCastTime();
    }

    public static float GetCooldownTime()
    {
        float value = GetCooldownTimeUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.cooldownModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Memory] != 0)
            {
                value += ((Memory)stats.First(s => s.StatType == StatType.Memory)).
                    GetCooldownTimeValueDifference(GetStatValue(StatType.Memory) - stats[(int)StatType.Memory].Value, GetStatValue(StatType.Memory));
            }
        }

        return value;
    }

    public static float GetCooldownTimeUnchanged()
    {
        return ((Memory)stats.First(s => s.StatType == StatType.Memory)).GetCooldownTime();
    }

    public static int GetMaxAbilitySlots()
    {
        return ((Memory)stats.First(s => s.StatType == StatType.Memory)).GetMaxAbilitySlots();
    }

    public static float GetCritDamage()
    {
        float value = GetCritDamageUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.critDamageModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Wits] != 0)
            {
                value += ((Wits)stats.First(s => s.StatType == StatType.Wits)).
                    GetCritDamageValueDifference(GetStatValue(StatType.Wits) - stats[(int)StatType.Wits].Value);
            }
        }

        return value;
    }

    public static float GetCritDamageUnchanged()
    {
        return ((Wits)stats.First(s => s.StatType == StatType.Wits)).GetCritDamage();
    }

    public static float GetCritChance()
    {
        float value = GetCritChanceUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.critChanceModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Wits] != 0)
            {
                value += ((Wits)stats.First(s => s.StatType == StatType.Wits)).
                    GetCritChanceValueDifference(GetStatValue(StatType.Wits) - stats[(int)StatType.Wits].Value);
            }
        }

        return value;
    }

    public static float GetCritChanceUnchanged()
    {
        return ((Wits)stats.First(s => s.StatType == StatType.Wits)).GetCritChance();
    }

    public static int GetSight()
    {
        int value = GetSightUnchanged();

        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            value += buffDebuff.sightModifier;

            if (buffDebuff.statsModifiers[(int)StatType.Wits] != 0)
            {
                value += ((Wits)stats.First(s => s.StatType == StatType.Wits)).
                    GetSightValueDifference(GetStatValue(StatType.Wits) - stats[(int)StatType.Wits].Value);
            }
        }

        return value;
    }

    public static int GetSightUnchanged()
    {
        return ((Wits)stats.First(s => s.StatType == StatType.Wits)).GetSight();
    }

    public static void OnExitActionMap()
    {
        characterBuffsDebuffs.RemoveAll(bd => bd.disappearAfterMap == true);
    }

    public static void Save(BinaryWriter writer)
    {       
        writer.Write(currentLevel);
        writer.Write(currentSpentStatPoints);
        writer.Write(Health);
        writer.Write(fatigue);

        foreach(var stat in stats)
        {
            writer.Write(stat.Value);
        }

        writer.Write(characterAbilities.Count);
        foreach(var characterAbility in characterAbilities)
        {
            writer.Write(characterAbility.id);
            writer.Write(characterAbility.Rank);
        }

        foreach (var hotBarAbility in hotBarAbilities)
        {
            if(hotBarAbility != null)
            {
                writer.Write(hotBarAbility.id);
            }
            else
            {
                writer.Write(-1);
            }          
        }

        writer.Write(activeAbilities.Count);
        foreach (var activeAbility in activeAbilities)
        {
            writer.Write(activeAbility.id);
        }

        writer.Write(characterBuffsDebuffs.Count);
        foreach (var buffDebuff in characterBuffsDebuffs)
        {
            writer.Write(buffDebuff.id);
        }
    }

    public static void Load(BinaryReader reader, int header)
    {
        currentLevel = reader.ReadInt32();
        currentSpentStatPoints = reader.ReadInt32();
        Health = reader.ReadSingle();
        fatigue = reader.ReadSingle();

        foreach(var stat in stats)
        {
            stat.StatLoad(reader.ReadInt32());
        }

        characterAbilities.Clear();
        int listCount = reader.ReadInt32();
        for(int i = 0; i < listCount; i++)
        {
            GiveAbility(reader.ReadInt32(), reader.ReadInt32());
        }

        for (int i = 0; i < hotBarMaxSlots; i++)
        {
            int id = reader.ReadInt32();

            if(id != -1)
            {
                Ability abilityToAdd = AbilityDatabase.GetAbility(id);
                abilityToAdd.Rank = characterAbilities.Find(ability => ability.id == abilityToAdd.id).Rank;
                hotBarAbilities[i] = abilityToAdd;
            }
        }

        activeAbilities.Clear();
        listCount = reader.ReadInt32();
        for (int i = 0; i < listCount; i++)
        {
            Ability abilityToAdd = AbilityDatabase.GetAbility(reader.ReadInt32());
            abilityToAdd.Rank = characterAbilities.Find(ability => ability.id == abilityToAdd.id).Rank;
            activeAbilities.Add(abilityToAdd);
        }

        listCount = reader.ReadInt32();
        for (int i = 0; i < listCount; i++)
        {
            GiveBuffDebuff(reader.ReadInt32());
        }
    }
}
