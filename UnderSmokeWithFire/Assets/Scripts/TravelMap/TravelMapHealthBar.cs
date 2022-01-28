using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TravelMapHealthBar : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private Image healthBar;
#pragma warning restore 0649

    private void OnEnable()
    {
        PlayerInfo.OnHealthChange += HandleHealthChange;

        HandleHealthChange();
    }

    private void OnDisable()
    {
        PlayerInfo.OnHealthChange -= HandleHealthChange;
    }

    private void HandleHealthChange()
    {
        healthBar.DOFillAmount(PlayerInfo.Health / PlayerInfo.GetMaxHealth(), .2f).SetEase(Ease.Linear);
    }
}
