using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Constitution : StatData
{
    public override StatType StatType => StatType.Constitution;
    public Dictionary<NegativeEffects, float> resists = new Dictionary<NegativeEffects, float>();
    public float maxHealth;

    public override int Value
    {
        get
        {
            return value;
        }
        set
        {
            if ((this.value + value) < minValue)
            {
                valueDifference = minValue - this.value;
                this.value = minValue;
            }
            else
            {
                valueDifference = value;
                this.value += value;
            }

            OnStatChange();
        }
    }

    private int value;
    private int valueDifference;

    public Constitution(int value)
    {
        maxHealth = StatConstants.normalMaxHealth;
        foreach (NegativeEffects effect in (NegativeEffects[])Enum.GetValues(typeof(NegativeEffects)))
        {
            resists.Add(effect, 0f);
        }
        Value = value;
    }

    public override void StatLoad(int statValue)
    {
        value = 0;
        maxHealth = StatConstants.normalMaxHealth;
        foreach (NegativeEffects effect in (NegativeEffects[])Enum.GetValues(typeof(NegativeEffects)))
        {
            resists[effect] = 0f;
        }

        Value = statValue;
    }

    protected override void OnStatChange()
    {
        maxHealth += GetMaxHealthValueDifference(valueDifference);

        foreach(var effect in resists.Keys.ToList())
        {
            resists[effect] += GetResistsValueDifference(valueDifference);
        }
    }

    public void OnResistChange(int[] negativeTypes, float value)
    {
        foreach(var effect in negativeTypes)
        {
            if (Enum.IsDefined(typeof(NegativeEffects), effect))
            {
                resists[(NegativeEffects)effect] += value;
            }
            else
            {
                Debug.LogError("Non NegativeEffects index was passed in Immunity: " + effect);
            }
        }
    }

    public void OnMaxHealthChange(float value)
    {
        maxHealth += value;
    }

    public float GetResistsValueDifference(int valueDifference)
    {
        return valueDifference * 5f / 100f;
    }

    public float[] GetResists()
    {
        float[] resistsValues = new float[resists.Count];
        foreach (var effect in resists.Keys.ToList())
        {
            resistsValues[(int)effect] = resists[effect];
        }

        return resistsValues;
    }

    public float GetMaxHealthValueDifference(int valueDifference)
    {
        return valueDifference * 30f;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
}
