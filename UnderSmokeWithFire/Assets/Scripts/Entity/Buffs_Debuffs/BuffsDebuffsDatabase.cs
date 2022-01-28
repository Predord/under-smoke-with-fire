using System.Collections.Generic;
using UnityEngine;

public static class BuffsDebuffsDatabase 
{
    private static List<BuffDebuff> buffsDebuffs = new List<BuffDebuff>();

    public static void InitializebuffsDebuffsDatabase(TextAsset jsonBuffsDebuffs)
    {
        buffsDebuffs = JsonHelper.FromJson<BuffDebuff>(jsonBuffsDebuffs.text);

        foreach (BuffDebuff buffDebuff in buffsDebuffs)
        {
            buffDebuff.icon = Resources.Load<Sprite>("BuffsDebuffs/Sprites/" + buffDebuff.title);
        }
    }

    public static BuffDebuff GetBuffDebuff(int id)
    {
        return buffsDebuffs.Find(buffDebuff => buffDebuff.id == id);
    }

    public static BuffDebuff GetBuffDebuff(string buffDebuffName)
    {
        return buffsDebuffs.Find(buffDebuff => buffDebuff.title == buffDebuffName);
    }
}
