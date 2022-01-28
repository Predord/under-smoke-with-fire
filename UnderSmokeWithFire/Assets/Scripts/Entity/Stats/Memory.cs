
public class Memory : StatData
{
    public override StatType StatType => StatType.Memory;
    public float coolDownTime;
    public int maxAbilitySlots;

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

    public Memory(int value)
    {
        coolDownTime = StatConstants.maxCooldown;
        Value = value;
    }

    public override void StatLoad(int statValue)
    {
        value = 0;
        maxAbilitySlots = 0;
        coolDownTime = StatConstants.maxCooldown;

        Value = statValue;
    }

    protected override void OnStatChange()
    {
        maxAbilitySlots += valueDifference / 2;

        coolDownTime += GetCooldownTimeValueDifference(valueDifference, value);
    }

    public void OnCooldownChange(float value)
    {
        coolDownTime -= value;
    }

    public float GetCooldownTimeValueDifference(int valueDifference, int value)
    {
        if (value - valueDifference <= StatConstants.maxNormalCooldownTime)
        {
            if (value <= StatConstants.maxNormalCooldownTime)
            {
                return (-1f) * valueDifference * StatConstants.normalCooldownMultiplier;
            }
            else
            { 
                float result = (-1f) * (StatConstants.maxNormalCooldownTime - value + valueDifference) * StatConstants.normalCooldownMultiplier;

                for (int i = 1; i <= value - StatConstants.maxNormalCooldownTime; i++)
                    result += (-1f) * StatConstants.reducedCooldownMultiplier / i;

                return result;
            }
        }
        else
        {
            if (valueDifference < 0)
            {
                float result = 0f;

                for (int i = value - valueDifference - StatConstants.maxNormalCooldownTime; i > 0; i--)
                    result += StatConstants.reducedCooldownMultiplier / i;

                if (value <= StatConstants.maxNormalCooldownTime)
                    result += (StatConstants.maxNormalCooldownTime - value) * StatConstants.normalCooldownMultiplier;

                return result;
            }
            else
            {
                float result = 0f;

                for (int i = value - valueDifference - StatConstants.maxNormalCooldownTime; i < value - StatConstants.maxNormalCooldownTime; i++)
                    result -= StatConstants.reducedCooldownMultiplier / (i + 1);

                return result;
            }
        }
    }

    public float GetCooldownTime()
    {
        return coolDownTime;
    }

    public int GetMaxAbilitySlots()
    {
        return maxAbilitySlots;
    }
}
