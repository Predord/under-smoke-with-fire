using UnityEngine;
using UnityEngine.UI;

public class HotbarTab : UIBasicAbility
{
    public Sprite borderSprite;

    private bool initialized;

    private void Start()
    {
        InitializeHotBar();
    }

    public void InitializeHotBar()
    {
        if (!initialized)
        {
            for (int i = 0; i < PlayerInfo.hotBarMaxSlots; i++)
            {
                InstantiateSlot();
                UpdateSlot(i, PlayerInfo.hotBarAbilities[i]);
                if(PlayerInfo.hotBarAbilities[i] != null)
                {
                    if (Player.Instance)
                    {
                        UIAbilities[i].CooldownCover.fillAmount = Player.Instance.activeAbilitiesCooldowns[PlayerInfo.hotBarAbilities[i]]
                            / PlayerInfo.hotBarAbilities[i].GetStatValue(AbilityStats.Cooldown);
                    }
                    else
                    {
                        UIAbilities[i].CooldownCover.fillAmount = 0f;
                    }
                }
            }

            initialized = true;
        }
    }

    public override void UpdateSlot(int slot, Ability ability)
    {
        UIAbilities[slot].isHotBar = true;
        base.UpdateSlot(slot, ability);
    }

    protected override void InstantiateSlot()
    {
        RectTransform instance = Instantiate(slotPrefab);
        instance.transform.SetParent(slotPanel);

        UIAbility uIAbility = instance.GetComponentInChildren<UIAbility>();
        uIAbility.AddActiveComponent();

        Image image = instance.GetComponent<Image>();
        image.type = Image.Type.Simple;
        image.sprite = borderSprite;
        image.color = Color.black;

        UIAbilities.Add(uIAbility);
    }

    public void UpdateAbilitiesCooldown(float cooldown, Ability ability)
    {
        foreach(var uIability in UIAbilities)
        {
            if(uIability.ability != null && uIability.ability.id == ability.id)
            {
                uIability.CooldownCover.fillAmount = cooldown;
            }
        }
    }
}
