using System.Collections.Generic;
using UnityEngine;

public class UIBuffDebuffList : MonoBehaviour
{
    public RectTransform slotPrefab;

    private Transform _transform;
    private List<UIBuffDebuff> UIBuffsDebuffs = new List<UIBuffDebuff>();

    private void Awake()
    {
        _transform = transform;
    }

    private void OnEnable()
    {
        if (UIBuffsDebuffs.Count != PlayerInfo.characterBuffsDebuffs.Count)
        {
            for (int i = UIBuffsDebuffs.Count; i < PlayerInfo.characterBuffsDebuffs.Count; i++)
            {
                AddBuffDebuff(PlayerInfo.characterBuffsDebuffs[i]);
            }
        }
    }

    public void AddBuffDebuff(BuffDebuff buffDebuff)
    {
        InstantiateSlot();
        UpdateSlot(UIBuffsDebuffs.Count - 1, buffDebuff);
    }

    public void UpdateSlot(int slot, BuffDebuff buffDebuff)
    {
        UIBuffsDebuffs[slot].UpdateBuffDebuff(buffDebuff);
    }

    public void RemoveSlot(BuffDebuff buffDebuff)
    {
        int index = UIBuffsDebuffs.FindIndex(x => x.buffDebuff == buffDebuff);
        Destroy(UIBuffsDebuffs[index].transform.parent.gameObject);
        UIBuffsDebuffs.RemoveAt(index);
    }

    private void InstantiateSlot()
    {
        RectTransform instance = Instantiate(slotPrefab);
        instance.transform.SetParent(_transform);
        UIBuffsDebuffs.Add(instance.GetComponentInChildren<UIBuffDebuff>());
    }
}
