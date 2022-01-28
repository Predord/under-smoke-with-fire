using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class LocalizedResistsChangeHandler : MonoBehaviour
{
    [HideInInspector]
    public NegativeEffects currentEffect = NegativeEffects.Knockdown;

    public LocalizedString stringRef = new LocalizedString() { TableReference = "PlayerStats", TableEntryReference = "Knockdown" };

    [SerializeField]
    private TMP_Text resistName;

    private void Awake()
    {
        stringRef = new LocalizedString() { TableReference = "PlayerStats", TableEntryReference = currentEffect.ToString() };
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
        resistName.text = translatedValue;
    }
}
