
public class Wits : StatData
{
    public override StatType StatType => StatType.Wits;
    public float critDamage;
    public float critChance;
    public int sight;

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

    public Wits(int value)
    {
        critDamage = StatConstants.normalCritDamage;
        sight = StatConstants.defaultSightRange;
        Value = value;
    }

    public override void StatLoad(int statValue)
    {
        value = 0;
        critChance = 0f;
        critDamage = StatConstants.normalCritDamage;
        sight = StatConstants.defaultSightRange;

        Value = statValue;
    }

    protected override void OnStatChange()
    {
        critDamage += GetCritDamageValueDifference(valueDifference);
        critChance += GetCritChanceValueDifference(valueDifference);
        sight += GetSightValueDifference(valueDifference);
    }

    public void OnCritDamageChange(float value)
    {
        critDamage += value;
    }

    public void OnCritChanceChange(float value)
    {
        critChance += value;
    }

    public void OnSightChange(int value)
    {
        sight += value;
    }

    public float GetCritDamageValueDifference(int valueDifference)
    {
        return valueDifference * 10f / 100f;
    }

    public float GetCritDamage()
    {
        return critDamage;
    }

    public float GetCritChanceValueDifference(int valueDifference)
    {
        return valueDifference / 100f;
    }

    public float GetCritChance()
    {
        return critChance;
    }

    public int GetSightValueDifference(int valueDifference)
    {
        return valueDifference / 2;
    }

    public int GetSight()
    {
        return sight;
    }
}
