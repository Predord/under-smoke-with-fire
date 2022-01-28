using UnityEngine;

public class UIAbilityList : UIBasicAbility
{
    public int maxSlots = 32;

    private void OnEnable()
    {
        if (UIAbilities.Count != PlayerInfo.characterAbilities.Count)
        {
            for (int i = UIAbilities.Count; i < PlayerInfo.characterAbilities.Count; i++)
            {
                AddAbility(PlayerInfo.characterAbilities[i]);
            }
        }
    }

    public override void UpdateSlot(int slot, Ability ability)
    {
        UIAbilities[slot].inAbilityList = true;
        base.UpdateSlot(slot, ability);
    }

    protected override void InstantiateSlot()
    {
        RectTransform instance = Instantiate(slotPrefab);
        instance.transform.SetParent(slotPanel);
        UIAbilities.Add(instance.GetComponentInChildren<UIAbility>());
    }
}
