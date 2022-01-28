using System;
using UnityEngine;

[Serializable]
public class BuffDebuff
{
    public int id;
    public string title;

    public bool disappearAfterMap;
    public int maxAbilityLevelModifier;
    public int dodgeModifier;
    public int sightModifier;
    public float maxHealthModifier;
    public float damageModifier;   
    public float cooldownModifier;  
    public float castTimeModifier;
    public float critDamageModifier;
    public float critChanceModifier;
    
    public int[] statsModifiers = new int[Enum.GetNames(typeof(StatType)).Length];
    public float[] resistsModifiers = new float[Enum.GetNames(typeof(NegativeEffects)).Length];

    public Sprite icon;

    public int GetStatModifierValue(StatType statType)
    {
        return statsModifiers[(int)statType];
    }

    public float GetStatModifierValue(NegativeEffects negativeEffects)
    {
        return resistsModifiers[(int)negativeEffects];
    }
}
