using System.Linq;
using TMPro;
using UnityEngine;

public class ModifiersList : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private TMP_Text damageMod;
    [SerializeField] private TMP_Text critDamageMod;
    [SerializeField] private TMP_Text critChanceMod;
    [SerializeField] private TMP_Text maxAbilityLvl;
    [SerializeField] private TMP_Text maxAbilitySlots;
#pragma warning restore 0649

    private void OnEnable()
    {
        PlayerInfo.OnStatChange += HandleIntelligenceChange;
        PlayerInfo.OnStatChange += HandleCritChange;
        PlayerInfo.OnStatChange += HandleMaxAbilitySlotsChange;

        HandleIntelligenceChange(PlayerInfo.stats.First(s => s.StatType == StatType.Intelligence));
        HandleCritChange(PlayerInfo.stats.First(s => s.StatType == StatType.Wits));
        HandleMaxAbilitySlotsChange(PlayerInfo.stats.First(s => s.StatType == StatType.Memory));
    }

    private void OnDestroy()
    {
        PlayerInfo.OnStatChange -= HandleIntelligenceChange;
        PlayerInfo.OnStatChange -= HandleMaxAbilitySlotsChange;
        PlayerInfo.OnStatChange -= HandleCritChange;
    }

    private void HandleIntelligenceChange(StatData statData)
    {
        if (statData.StatType == StatType.Intelligence)
        {
            float changedValue = PlayerInfo.GetDamageMultiplier();
            damageMod.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetDamageMultiplierUnchanged());
            damageMod.SetText("{0}x", Mathf.Round(changedValue * 100f) / 100f);

            changedValue = PlayerInfo.GetMaxAbilityLevel();
            maxAbilityLvl.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetMaxAbilityLevelUnchanged());
            maxAbilityLvl.SetText(changedValue.ToString());
        }
    }

    private void HandleCritChange(StatData statData)
    {
        if (statData.StatType == StatType.Wits)
        {
            float changedValue = PlayerInfo.GetCritDamage();
            critDamageMod.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetCritDamageUnchanged());
            critDamageMod.SetText("{0}x", Mathf.Round(changedValue * 100f) / 100f);

            changedValue = PlayerInfo.GetCritChance();
            critChanceMod.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetCritChanceUnchanged());
            critChanceMod.SetText("{0}", Mathf.Round(changedValue * 100f) / 100f);
        }
    }

    private void HandleMaxAbilitySlotsChange(StatData statData)
    {
        if (statData.StatType == StatType.Memory)
        {
            maxAbilitySlots.SetText(PlayerInfo.GetMaxAbilitySlots().ToString());      
        }
    }
}
