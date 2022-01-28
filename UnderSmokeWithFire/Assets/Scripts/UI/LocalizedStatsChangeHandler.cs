using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class LocalizedStatsChangeHandler : MonoBehaviour
{
    [HideInInspector]
    public StatType currentStat = StatType.Intelligence;

    public LocalizedString stringRef = new LocalizedString() { TableReference = "PlayerStats", TableEntryReference = "Intelligence" };

    [SerializeField]
    private TMP_Text statName;

    private void Awake()
    {
        stringRef = new LocalizedString() { TableReference = "PlayerStats", TableEntryReference = currentStat.ToString() };
    }

    private void OnEnable()
    {
        stringRef.StringChanged += UpdateString;
    }

    private void OnDisable()
    {
        stringRef.StringChanged -= UpdateString;
    }

    private void UpdateString(string translatedValue)
    {
        statName.text = translatedValue;
    }
}
