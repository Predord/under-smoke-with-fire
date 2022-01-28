using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class LocalizedObjectiveChangeHandler : MonoBehaviour
{
    [HideInInspector]
    public int objectiveId;

    public LocalizedString stringRef = new LocalizedString() { TableReference = "Objectives", TableEntryReference = "KILL_TARGET" };

    [SerializeField]
    private TMP_Text objectiveName;

    private void Awake()
    {
        if (GameUI.Instance)
        {
            stringRef = new LocalizedString() { TableReference = "Objectives", TableEntryReference = GameUI.Instance.activeObjectiveName };
        }
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
        objectiveName.text = translatedValue;
    }
}
