using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StatsList : MonoBehaviour
{
    public int availablePoints;
    public int currentSpentStatPoints;

#pragma warning disable 0649
    [SerializeField] private Transform panelRoot;
    [SerializeField] private Transform statPrefab;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button resetButton;
#pragma warning restore 0649

    private Dictionary<StatType, StatHolder> holders = new Dictionary<StatType, StatHolder>();

    private void OnEnable()
    {
        currentSpentStatPoints = PlayerInfo.currentSpentStatPoints;
        PlayerInfo.OnStatChange += HandleStatChange;
        PlayerInfo.OnLevelUp += CheckForStatPoints;
        SetResetAcceptButtons(false);

        if (holders.Count == 0)
            InstantiateStats();

        CheckForStatPoints();
    }

    private void OnDisable()
    {
        ResetValueChanges();
        PlayerInfo.OnStatChange -= HandleStatChange;
        PlayerInfo.OnLevelUp -= CheckForStatPoints;
    }

    public void AcceptStatChanges()
    {
        for (int i = 0; i < holders.Count; i++)
        {
            PlayerInfo.currentSpentStatPoints += holders[(StatType)i].valueChanges;

            var statData = PlayerInfo.stats.First(s => s.StatType == (StatType)i);
            statData.Value = holders[(StatType)i].valueChanges;
            PlayerInfo.ApplyStatChange(statData);

            holders[(StatType)i].valueChanges = 0;
        }

        resetButton.interactable = false;
        if (PlayerInfo.currentSpentStatPoints == PlayerInfo.currentLevel * StatConstants.pointsGainPerLevel)
            acceptButton.interactable = false;
    }

    public void ResetStatChanges()
    {
        EnableAddButtons();
        ResetValueChanges();

        currentSpentStatPoints = PlayerInfo.currentSpentStatPoints;
        availablePoints = PlayerInfo.currentLevel * StatConstants.pointsGainPerLevel - currentSpentStatPoints;
        SetResetAcceptButtons(false);
    }

    public void DisableStatsAddButtons()
    {
        for (int i = 0; i < holders.Count; i++)
        {
            holders[(StatType)i].addButton.gameObject.SetActive(false);
        }
    }

    public void SetResetAcceptButtons(bool isActive)
    {
        resetButton.interactable = isActive;
        acceptButton.interactable = isActive;
    }

    private void HandleStatChange(StatData statData)
    {
        holders.TryGetValue(statData.StatType, out var valueToChange);
        valueToChange.SetData(PlayerInfo.GetStatValue(statData.StatType), PlayerInfo.GetStatValueUnchanged(statData.StatType), statData.StatType);
    }

    private void InstantiateStats()
    {
        foreach (var stat in PlayerInfo.stats)
        {
            CreateStatHolder(stat);
        }
    }

    private void CreateStatHolder(StatData stat)
    {
        var instance = Instantiate(statPrefab, panelRoot);

        StatHolder statHolder = instance.gameObject.GetComponent<StatHolder>();
        statHolder.SetData(PlayerInfo.GetStatValue(stat.StatType), PlayerInfo.GetStatValueUnchanged(stat.StatType), stat.StatType);
        instance.gameObject.GetComponent<LocalizedStatsChangeHandler>().currentStat = stat.StatType;
        instance.gameObject.SetActive(true);
        holders.Add(stat.StatType, statHolder);
    }

    private void CheckForStatPoints()
    {
        if (currentSpentStatPoints != PlayerInfo.currentLevel * StatConstants.pointsGainPerLevel)
        {
            availablePoints = PlayerInfo.currentLevel * StatConstants.pointsGainPerLevel - currentSpentStatPoints;

            EnableAddButtons();
        }
    }

    private void ResetValueChanges()
    {
        for (int i = 0; i < holders.Count; i++)
        {
            holders[(StatType)i].SetStat(-holders[(StatType)i].valueChanges);
            holders[(StatType)i].valueChanges = 0;
        }
    }

    private void EnableAddButtons()
    {
        for (int i = 0; i < holders.Count; i++)
        {
            holders[(StatType)i].addButton.gameObject.SetActive(true);
        }
    }
}
