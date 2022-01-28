
public class Intelligence : StatData
{
    public override StatType StatType => StatType.Intelligence;
    public float damageMultiplier;

    public int MaxAbilityLevel
    {
        get
        {
            return maxAbilityLevel;
        }
        set
        {
            if (value == maxAbilityLevel)
                return;

            maxAbilityLevel = UnityEngine.Mathf.Clamp(value, 0, Ability.maxRank);                   
        }
    }

    private int maxAbilityLevel;

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

    public Intelligence(int value)
    {
        damageMultiplier = StatConstants.normalDamageMultiplier;
        Value = value;
    }

    public override void StatLoad(int statValue)
    {
        value = 0;
        maxAbilityLevel = 0;
        damageMultiplier = StatConstants.normalDamageMultiplier;

        Value = statValue;
    }

    protected override void OnStatChange()
    {
        damageMultiplier += GetDamageMultiplierValueDifference(valueDifference);
        MaxAbilityLevel += GetMaxAbilityLevelValueDifference(valueDifference, value);
    }

    public void OnDamageChange(float value)
    {
        damageMultiplier += value;
    }

    public void OnMaxAbilityLevelChange(int value)
    {
        MaxAbilityLevel += value;
    }

    public float GetDamageMultiplierValueDifference(int valueDifference)
    {
        return valueDifference * 5f / 100f;
    }

    public float GetDamageMultiplier()
    {
        return damageMultiplier;
    }

    public int GetMaxAbilityLevelValueDifference(int valueDifference, int value)
    {
        return (int)(value / 5f) - (int)((value - valueDifference) / 5f);
    }

    public int GetMaxAbilityLevel()
    {
        return MaxAbilityLevel;
    }
}
