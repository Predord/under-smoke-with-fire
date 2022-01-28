using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ZoneManager : MonoBehaviour
{
    public Button deleteButton;
    public RectTransform listContent;
    public ZoneConnectionManager connectionManager;
    public UIZoneItem itemPrefab;

    public int currentIndex = -1;

    public void Open()
    {
        FillList();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        connectionManager.Close();
        gameObject.SetActive(false);
    }

    public void SelectItem(int index)
    {
        if(currentIndex == index)
        {
            if(GameManager.Instance.grid.specialZones[currentIndex].zoneType == SpecialZoneType.DefencePosition)
            {
                connectionManager.gameObject.SetActive(true);
                connectionManager.Open(currentIndex, UIZoneItem.zoneNames[(int)GameManager.Instance.grid.specialZones[currentIndex].zoneType]);
            }
        }
        else
        {
            if(currentIndex != -1)
            {
                GameManager.Instance.grid.specialZones[currentIndex].ShowZoneHighlights(QuadMetrics.GetZoneColor(GameManager.Instance.grid.specialZones[currentIndex].zoneType));
            }           

            currentIndex = index;

            GameManager.Instance.grid.specialZones[currentIndex].ShowZoneHighlights(Color.white);

            if (connectionManager.gameObject.activeSelf)
            {
                if ((GameManager.Instance.grid.specialZones[currentIndex].zoneType == SpecialZoneType.DefencePosition ||
                    GameManager.Instance.grid.specialZones[currentIndex].zoneType == SpecialZoneType.EnemySpawn) &&
                    GameManager.Instance.grid.camps.Find(x => x.zoneIndex == connectionManager.currentIndex) != GameManager.Instance.grid.camps.Find(x => x.zoneIndex == currentIndex) &&
                    !GameManager.Instance.grid.camps.Find(x => x.zoneIndex == connectionManager.currentIndex).reinforcementsZones.ContainsKey(currentIndex))
                {
                    connectionManager.addButton.gameObject.SetActive(true);
                }
                else
                {
                    connectionManager.addButton.gameObject.SetActive(false);
                }
            }

            deleteButton.gameObject.SetActive(true);
        }
    }

    public void DeleteItem()
    {
        if (GameManager.Instance.grid.specialZones[currentIndex].zoneType == SpecialZoneType.DefencePosition)
        {
            GameManager.Instance.grid.camps.RemoveAll(x => x.zoneIndex == currentIndex);

            if (connectionManager.currentIndex == currentIndex)
            {
                connectionManager.aIControlsMenu.zoneToAssignIndex = -1;
                connectionManager.Close();
            }
        }

        foreach (Enemy enemy in GameManager.Instance.grid.units.Where(x => x.index != 0))
        {
            if (enemy.AssignedDefencePositionIndex > currentIndex) 
            {
                enemy.AssignedDefencePositionIndex -= 1;
            }
            else if(enemy.AssignedDefencePositionIndex == currentIndex)
            {
                enemy.AssignedDefencePositionIndex = -1;
            }
        }

        foreach (var camp in GameManager.Instance.grid.camps)
        {
            if (camp.zoneIndex > currentIndex)
            {
                camp.zoneIndex--;
            }

            if (camp.reinforcementsZones.ContainsKey(currentIndex))
            {
                camp.reinforcementsZones.Remove(currentIndex);
            }

            List<int> keys = camp.reinforcementsZones.Keys.ToList();
            keys.Sort();

            for(int i = 0; i < keys.Count; i++)
            {
                if(keys[i] > currentIndex)
                {
                    keys[i] -= 1;
                }
            }

            bool gap = false; 

            for(int i = 0; i < keys.Count; i++)
            {
                if(keys[i] >= currentIndex)
                {
                    if (camp.reinforcementsZones.ContainsKey(keys[i]))
                    {
                        camp.reinforcementsZones[keys[i]] = camp.reinforcementsZones[keys[i] + 1];
                        gap = true;
                    }
                    else
                    {
                        camp.reinforcementsZones.Add(keys[i], camp.reinforcementsZones[keys[i] + 1]);
                        if (gap)
                        {
                            camp.reinforcementsZones.Remove(camp.reinforcementsZones[keys[i - 1] + 1]);
                            gap = false;
                        }
                    }
                }
            }
           
            if (keys.Count > 0 && keys[keys.Count - 1] >= currentIndex)
            {
                camp.reinforcementsZones.Remove(keys[keys.Count - 1] + 1);
            }    
        }

        GameManager.Instance.grid.specialZones[currentIndex].ClearZoneHighlights();
        GameManager.Instance.grid.specialZones.RemoveAt(currentIndex);
        for(int i = 0; i < GameManager.Instance.grid.specialZones.Count; i++)
        {
            GameManager.Instance.grid.specialZones[i].ShowZoneHighlights(QuadMetrics.GetZoneColor(GameManager.Instance.grid.specialZones[i].zoneType));
        }
        FillList();

        if (connectionManager.gameObject.activeSelf)
        {
            if (connectionManager.currentIndex > currentIndex)
                connectionManager.currentIndex--;

            connectionManager.Open(connectionManager.currentIndex, UIZoneItem.zoneNames[(int)GameManager.Instance.grid.specialZones[connectionManager.currentIndex].zoneType]);
        }

        currentIndex = -1;
        deleteButton.gameObject.SetActive(false);
    }

    public void DeleteAll()
    {
        for(int i = 0; i < GameManager.Instance.grid.specialZones.Count; i++)
        {
            GameManager.Instance.grid.specialZones[i].ClearZoneHighlights();
        }
        GameManager.Instance.grid.camps.Clear();
        GameManager.Instance.grid.specialZones.Clear();

        foreach (Enemy enemy in GameManager.Instance.grid.units.Where(x => x.index != 0))
        {
            enemy.AssignedDefencePositionIndex = -1;
        }

        connectionManager.aIControlsMenu.zoneToAssignIndex = -1;
        connectionManager.Close();
        deleteButton.gameObject.SetActive(false);
        FillList();
    }

    private void FillList()
    {
        for (int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        for (int i = 0; i < GameManager.Instance.grid.specialZones.Count; i++)
        {
            UIZoneItem item = Instantiate(itemPrefab);
            item.zoneManager = this;
            item.index = i;
            item.SetZoneName((int)GameManager.Instance.grid.specialZones[i].zoneType);
            item.transform.SetParent(listContent, false);
        }
    }
}
