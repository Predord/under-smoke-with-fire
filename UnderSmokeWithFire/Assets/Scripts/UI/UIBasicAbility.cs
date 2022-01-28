using System.Collections.Generic;
using UnityEngine;

public class UIBasicAbility : MonoBehaviour
{
    public List<UIAbility> UIAbilities = new List<UIAbility>();
    public RectTransform slotPrefab;
    public Transform slotPanel;

    public virtual void AddAbility(Ability ability)
    {
        InstantiateSlot();
        UpdateSlot(UIAbilities.Count - 1, ability);
    }

    public virtual void UpdateSlot(int slot, Ability ability)
    {
        UIAbilities[slot].UpdateAbility(ability);
    }

    protected virtual void InstantiateSlot()
    {
    }
}
