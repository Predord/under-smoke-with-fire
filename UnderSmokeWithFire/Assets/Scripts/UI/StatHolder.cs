using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatHolder : MonoBehaviour
{
    public int valueChanges;
    public Button addButton;

#pragma warning disable 0649
    [SerializeField] private TMP_Text value;
#pragma warning restore 0649

    private StatsList statsList;

    private void OnEnable()
    {
        statsList = GetComponentInParent<StatsList>();
    }

    public void SetData(int statValueChanged, int statValueUnchanged, StatType statType)
    {
        value.color = GameUI.Instance.GetStatsPanelColor(statValueChanged - statValueUnchanged);
        value.SetText(statValueChanged.ToString());
    }

    public void EnableAddButton()
    {
        addButton.gameObject.SetActive(true);

        valueChanges = 0;
    }

    public void AddStatWithButton()
    {
        if (GameManager.Instance.IsActionMap)
            return;

        SetStat(1);
        valueChanges++;

        statsList.SetResetAcceptButtons(true);
        statsList.currentSpentStatPoints++;
        statsList.availablePoints--;
        if (statsList.availablePoints == 0)
            statsList.DisableStatsAddButtons();
    }

    public void SetStat(int amount)
    {
        int currentValue = int.Parse(value.text);
        value.SetText((currentValue + amount).ToString());
    }
}
