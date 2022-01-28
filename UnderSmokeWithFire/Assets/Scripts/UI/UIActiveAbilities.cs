using System.Collections.Generic;
using UnityEngine;

public class UIActiveAbilities : UIBasicAbility
{
    private bool initialized;

    private void OnEnable()
    {
        int maxSlots = PlayerInfo.GetMaxAbilitySlots();
        int activeAbilitiesCount = PlayerInfo.activeAbilities.Count;
        int abilitiesCount = UIAbilities.Count;

        if (abilitiesCount != maxSlots)
        {
            List<Ability> activeAbilities = PlayerInfo.activeAbilities;
            activeAbilities.Sort((x, y) => x == null ? (y == null ? 0 : -1) : (y == null ? 1 : x.id.CompareTo(y.id)));

            if (abilitiesCount != 0)
            {
                for (int i = abilitiesCount; i < maxSlots; i++)
                {
                    AddAbility(null);
                }
            }
            else
            {
                for (int i = 0; i < activeAbilitiesCount; i++)
                {
                    AddAbility(activeAbilities[i]);

                    if (Player.Instance)
                    {
                        UIAbilities[i].CooldownCover.fillAmount = Player.Instance.activeAbilitiesCooldowns[activeAbilities[i]]
                            / activeAbilities[i].GetStatValue(AbilityStats.Cooldown);
                    }
                    else
                    {
                        UIAbilities[i].CooldownCover.fillAmount = 0f;
                    }
                }

                for (int i = UIAbilities.Count; i < maxSlots; i++)
                {
                    AddAbility(null);
                }
            }
        }

        initialized = true;
    }

    protected override void InstantiateSlot()
    {
        RectTransform slotInstance = Instantiate(slotPrefab);
        UIAbility uIAbility = slotInstance.GetComponentInChildren<UIAbility>();

        slotInstance.transform.SetParent(slotPanel);
        uIAbility.AddActiveComponent();
        UIAbilities.Add(uIAbility);
    }

    public void UpdateAbilitiesCooldown(float cooldown, Ability ability)
    {
        if (initialized)
        {
            UIAbilities.Find(x => x.ability != null && x.ability.id == ability.id).CooldownCover.fillAmount = cooldown;
        }      
    }
}
