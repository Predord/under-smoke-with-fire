using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneConnectionManager : MonoBehaviour
{
    public Button deleteButton;
    public Button addButton;
    public TMP_Text zoneName;
    public RectTransform listContent;
    public ZoneManager zoneManager;
    public UIZoneConnectionItem itemPrefab;
    public AIControlsMenu aIControlsMenu;

    public int currentIndex;
    private int currentChangeIndex = -1;

    public void Open(int index, string zoneName)
    {
        currentIndex = index;
        aIControlsMenu.zoneToAssignIndex = currentIndex;
        this.zoneName.text = zoneName + currentIndex.ToString();

        FillList();
        addButton.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        deleteButton.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void SelectItem(int index)
    {
        currentChangeIndex = index;
        deleteButton.gameObject.SetActive(true);
    }

    public void AddItem()
    {
        GameManager.Instance.grid.camps.Find(x => x.zoneIndex == currentIndex).reinforcementsZones.Add(zoneManager.currentIndex, 0);
        FillList();

        addButton.gameObject.SetActive(false);
    }

    public void ChangeItems()
    {
        EnemyCamp enemyCamp = GameManager.Instance.grid.camps.Find(x => x.zoneIndex == currentIndex);

        foreach (var item in GetComponentsInChildren<UIZoneConnectionItem>())
        {
            enemyCamp.reinforcementsZones[item.zone.index] = int.Parse(item.inputField.text);
        }
    }

    public void DeleteItem()
    {
        EnemyCamp camp = GameManager.Instance.grid.camps.Find(x => x.zoneIndex == currentIndex);
        camp.reinforcementsZones.Remove(currentChangeIndex);

        FillList();

        deleteButton.gameObject.SetActive(false);      
    }

    public void FillList()
    {
        currentChangeIndex = -1;
        deleteButton.gameObject.SetActive(false);

        for (int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        EnemyCamp enemyCamp = GameManager.Instance.grid.camps.Find(x => x.zoneIndex == currentIndex);

        foreach (var index in enemyCamp.reinforcementsZones.Keys.ToList())
        {
            UIZoneConnectionItem item = Instantiate(itemPrefab);
            item.zoneConnectionManager = this;
            item.inputField.text = enemyCamp.reinforcementsZones[index].ToString();
            item.zone.zoneConnectionManager = this;
            item.zone.index = index;
            item.zone.SetZoneName((int)GameManager.Instance.grid.specialZones[index].zoneType);
            item.transform.SetParent(listContent, false);
        }
    }
}
