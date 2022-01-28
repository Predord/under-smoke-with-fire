using UnityEngine;
using TMPro;

public class UIZoneItem : MonoBehaviour
{
    public int index;
    public ZoneManager zoneManager;
    public ZoneConnectionManager zoneConnectionManager;

    public static string[] zoneNames =
    {
        "PlayerSpawn ",
        "EnemySpawn ",
        "DefencePosition ",
        "ExitZone ",
        "ScoutedArea"
    };

    public void SetZoneName(int zoneIndex)
    {
        transform.GetChild(0).GetComponent<TMP_Text>().text = zoneNames[zoneIndex] + index;
    }

    public void Select()
    {
        if (zoneManager)
        {
            zoneManager.SelectItem(index);
        }
        else if(zoneConnectionManager)
        {
            zoneConnectionManager.SelectItem(index);
        }        
    }
}
