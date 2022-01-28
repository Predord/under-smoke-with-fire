using TMPro;
using UnityEngine;

public class ResistHolder : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private TMP_Text value;
#pragma warning restore 0649

    public void SetData(float valueChanged, float valueUnchanged, NegativeEffects effect)
    {
        value.color = GameUI.Instance.GetStatsPanelColor(valueChanged - valueUnchanged);
        value.SetText("{0}%", Mathf.Round(valueChanged * 100f));
    }
}
