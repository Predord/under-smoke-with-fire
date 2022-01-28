using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIBuffDebuffAMList : MonoBehaviour
{
    public RectTransform slotPrefab;
    public RectTransform rectTransform;

    private Entity boundEntity;
    private List<UIBuffDebuffAM> UIBuffsDebuffs = new List<UIBuffDebuffAM>();

    public void BindList(Entity entity)
    {
        boundEntity = entity;

        if (boundEntity)
        {
            rectTransform.gameObject.SetActive(true);

            for (int i = 0; i < UIBuffsDebuffs.Count; i++)
            {
                RemoveSlot(UIBuffsDebuffs[i].buffDebuff);
            }
            foreach (var buffDebuff in boundEntity.characterActionMapBuffsDebuffs.Keys.ToList())
            {
                AddBuffDebuff(buffDebuff);
            }
        }
    }

    public void UnbindList()
    {
        boundEntity = null;

        for (int i = 0; i < UIBuffsDebuffs.Count; i++)
        {
            RemoveSlot(UIBuffsDebuffs[i].buffDebuff);
        }

        rectTransform.gameObject.SetActive(false);
    }

    public void OnBuffDebuffChange(int turns, BuffDebuffActionMap buffDebuff)
    {
        if(UIBuffsDebuffs.Exists(x => x.buffDebuff == buffDebuff))
        {
            int index = UIBuffsDebuffs.FindIndex(x => x.buffDebuff == buffDebuff);
            UIBuffsDebuffs[index].turns = turns;

            if(UIBuffsDebuffs[index].turns <= 0)
            {
                RemoveSlot(buffDebuff);
            }
        }
        else
        {
            AddBuffDebuff(buffDebuff);
        }
    }

    public void AddBuffDebuff(BuffDebuffActionMap buffDebuff)
    {
        InstantiateSlot();
        UpdateSlot(UIBuffsDebuffs.Count - 1, buffDebuff);
    }

    public void UpdateSlot(int slot, BuffDebuffActionMap buffDebuff)
    {
        UIBuffsDebuffs[slot].UpdateBuffDebuff(buffDebuff);
    }

    public void RemoveSlot(BuffDebuffActionMap buffDebuff)
    {
        int index = UIBuffsDebuffs.FindIndex(x => x.buffDebuff == buffDebuff);
        Destroy(UIBuffsDebuffs[index].transform.parent.gameObject);
        UIBuffsDebuffs.RemoveAt(index);
    }

    private void InstantiateSlot()
    {
        RectTransform instance = Instantiate(slotPrefab);
        instance.transform.SetParent(rectTransform);
        UIBuffsDebuffs.Add(instance.GetComponentInChildren<UIBuffDebuffAM>());
    }
}
