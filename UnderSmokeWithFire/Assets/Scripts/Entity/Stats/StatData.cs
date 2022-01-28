using System.Collections.Generic;

public abstract class StatData
{
    public static int minValue = 1;

    public StatData() { }

    public abstract StatType StatType { get; }
    public abstract int Value { get; set; }
    public List<string> additionalBonuses = new List<string>();

    public virtual void StatLoad(int statValue)
    {
    }

    protected virtual void OnStatChange()
    {
    }
}
