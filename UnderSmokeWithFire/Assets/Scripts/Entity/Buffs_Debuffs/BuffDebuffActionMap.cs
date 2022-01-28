using System;
using UnityEngine;

[Serializable]
public class BuffDebuffActionMap 
{
    public int id;
    public string title;

    public int turnsAmount;
    public int damageType;
    public int damageOverTime;    
    public int sightRaw;
    public int sightModifier;

    public Sprite icon;
}
