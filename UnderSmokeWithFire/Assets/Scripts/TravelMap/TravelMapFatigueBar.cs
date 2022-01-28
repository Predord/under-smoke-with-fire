using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TravelMapFatigueBar : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private Image fatigueBar;
#pragma warning restore 0649

    private void OnEnable()
    {
        PlayerInfo.OnFatigueChange += HandleFatigueChange;

        HandleFatigueChange();
    }

    private void OnDisable()
    {
        PlayerInfo.OnFatigueChange-= HandleFatigueChange;
    }

    private void HandleFatigueChange()
    {
        fatigueBar.DOFillAmount(PlayerInfo.Fatigue / StatConstants.normalMaxFatigue, .2f).SetEase(Ease.Linear);
    }
}
