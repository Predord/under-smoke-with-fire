using System.Collections.Generic;
using UnityEngine;

public static class BuffsDebuffsActionMapDatabase
{
    private static List<BuffDebuffActionMap> buffsDebuffsAM = new List<BuffDebuffActionMap>();

    public static void InitializeBuffsDebuffsAMDatabase(TextAsset jsonbuffsDebuffsAM)
    {
        buffsDebuffsAM = JsonHelper.FromJson<BuffDebuffActionMap>(jsonbuffsDebuffsAM.text);

        foreach (BuffDebuffActionMap buffsDebuffAM in buffsDebuffsAM)
        {
            buffsDebuffAM.icon = Resources.Load<Sprite>("BuffsDebuffs/Sprites/" + buffsDebuffAM.title);
        }
    }

    public static BuffDebuffActionMap GetBuffDebuff(int id)
    {
        return buffsDebuffsAM.Find(buffsDebuffAM => buffsDebuffAM.id == id);
    }

    public static BuffDebuffActionMap GetBuffDebuff(string title)
    {
        return buffsDebuffsAM.Find(buffsDebuffAM => buffsDebuffAM.title == title);
    }
}
