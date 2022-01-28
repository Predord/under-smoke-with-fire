using System.Linq;
using TMPro;
using UnityEngine;

public class PhysicalStatsList : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private TMP_Text sight;
    [SerializeField] private TMP_Text cooldownTime;
    [SerializeField] private TMP_Text castTime;
    [SerializeField] private TMP_Text dodge;
#pragma warning restore 0649

    private void OnEnable()
    {
        PlayerInfo.OnStatChange += HandleSightChange;
        PlayerInfo.OnStatChange += HandleCooldownChange;
        PlayerInfo.OnStatChange += HandleCastTimeChange;
        PlayerInfo.OnStatChange += HandleDodgeChange;

        HandleSightChange(PlayerInfo.stats.First(s => s.StatType == StatType.Wits));
        HandleCooldownChange(PlayerInfo.stats.First(s => s.StatType == StatType.Memory));
        HandleCastTimeChange(PlayerInfo.stats.First(s => s.StatType == StatType.Speed));
        HandleDodgeChange(PlayerInfo.stats.First(s => s.StatType == StatType.Speed));
    }

    private void OnDestroy()
    {
        PlayerInfo.OnStatChange -= HandleSightChange;
        PlayerInfo.OnStatChange -= HandleCooldownChange;
        PlayerInfo.OnStatChange -= HandleCastTimeChange;
        PlayerInfo.OnStatChange -= HandleDodgeChange;
    }

    private void HandleDodgeChange(StatData statData)
    {
        if (statData.StatType == StatType.Speed)
        {
            float changedValue = PlayerInfo.GetDodge();
            dodge.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetDodgeUnchanged());
            dodge.SetText(changedValue.ToString());
        }
    }

    private void HandleSightChange(StatData statData)
    {
        if (statData.StatType == StatType.Wits)
        {
            float changedValue = PlayerInfo.GetSight();
            sight.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetSightUnchanged());
            sight.SetText(changedValue.ToString());        
        }
    }

    private void HandleCooldownChange(StatData statData)
    {
        if (statData.StatType == StatType.Memory)
        {
            float changedValue = PlayerInfo.GetCooldownTime();
            cooldownTime.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetCooldownTimeUnchanged());
            cooldownTime.SetText("{0}x", Mathf.Round(changedValue * 10000f) / 10000f);       
        }
    }

    private void HandleCastTimeChange(StatData statData)
    {
        if (statData.StatType == StatType.Speed)
        {
            float changedValue = PlayerInfo.GetCastTime();
            castTime.color = GameUI.Instance.GetStatsPanelColor(changedValue - PlayerInfo.GetCastTimeUnchanged());
            castTime.SetText("{0}x", Mathf.Round(changedValue * 10000f) / 10000f);      
        }
    }
}
