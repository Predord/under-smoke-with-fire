using System.Linq;
using TMPro;
using UnityEngine;

public class HpStatusList : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private TMP_Text healthPoints;
    [SerializeField] private TMP_Text fatiguePoints;
#pragma warning restore 0649

    private void OnEnable()
    {
        PlayerInfo.OnStatChange += HandleHealthChange;
        PlayerInfo.OnHealthChange += HandleHealthChange;
        PlayerInfo.OnFatigueChange += HandleFatigueChange;
        HandleHealthChange(PlayerInfo.stats.First(s => s.StatType == StatType.Constitution));
        HandleFatigueChange();
    }

    private void OnDestroy()
    {
        PlayerInfo.OnStatChange -= HandleHealthChange;
        PlayerInfo.OnHealthChange -= HandleHealthChange;
        PlayerInfo.OnFatigueChange -= HandleFatigueChange;
    }

    private void HandleHealthChange(StatData statData)
    {
        if (statData.StatType == StatType.Constitution)
        {
            healthPoints.SetText("{0}/{1}", PlayerInfo.Health, PlayerInfo.GetMaxHealth());          
        }
    }

    private void HandleHealthChange()
    {
        healthPoints.SetText("{0}/{1}", PlayerInfo.Health, PlayerInfo.GetMaxHealth());
    }

    private void HandleFatigueChange()
    {
        fatiguePoints.SetText("{0}/{1}", PlayerInfo.Fatigue, StatConstants.normalMaxFatigue);
    }
}
