
public class Speed : StatData
{
    public override StatType StatType => StatType.Speed;
    public float castTime;
    public int dodge;

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

    public Speed(int value)
    {
        castTime = StatConstants.maxCastTime;
        Value = value;
    }

    public override void StatLoad(int statValue)
    {
        value = 0;
        dodge = 0;
        castTime = StatConstants.maxCastTime;

        Value = statValue;
    }

    protected override void OnStatChange()
    {
        dodge += GetDodgeValueDifference(valueDifference, value);

        castTime += GetCastTimeValueDifference(valueDifference, value);
    }

    public void OnCastTimeChange(float value)
    {
        castTime -= value;
    }

    public void OnDodgeChange(int value)
    {
        dodge += value;
    }

    public float GetCastTimeValueDifference(int valueDifference, int value)
    {
        if (value - valueDifference <= StatConstants.maxNormalCastTime)
        {
            if (value <= StatConstants.maxNormalCastTime)
            {
                return (-1f) * valueDifference * StatConstants.normalCastTimeMultiplier;
            }
            else
            {
                float result = (-1f) * (StatConstants.maxNormalCastTime - value + valueDifference) * StatConstants.normalCastTimeMultiplier;

                for (int i = 1; i <= value - StatConstants.maxNormalCastTime; i++)
                    result += (-1f) * StatConstants.reducedCastTimeMultiplier / i;

                return result;
            }
        }
        else
        {
            if (valueDifference < 0)
            {
                float result = 0f;

                for (int i = value - valueDifference - StatConstants.maxNormalCastTime; i > 0; i--)
                    result += StatConstants.reducedCastTimeMultiplier / i;

                if (value <= StatConstants.maxNormalCastTime)
                    result += (StatConstants.maxNormalCastTime - value) * StatConstants.normalCastTimeMultiplier;

                return result;
            }
            else
            {
                float result = 0f;

                for (int i = value - valueDifference - StatConstants.maxNormalCastTime; i < value - StatConstants.maxNormalCastTime; i++)
                    result -= StatConstants.reducedCastTimeMultiplier / (i + 1);

                return result;
            }
        }
    }

    public float GetCastTime()
    {
        return castTime;
    }

    public int GetDodgeValueDifference(int valueDifference, int value)
    {
        return (int)(value / 5f) - (int)((value - valueDifference) / 5f);
    }

    public int GetDodge()
    {
        return dodge;
    }
}
