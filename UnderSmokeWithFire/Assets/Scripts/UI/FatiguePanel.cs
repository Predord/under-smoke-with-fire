using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FatiguePanel : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private Image fatigueBar;
    [SerializeField] private GameObject panelRoot;
#pragma warning restore 0649

    private void OnEnable()
    {
        if (Player.Instance)
            Bind();      
    }

    private void OnDisable()
    {
        if (Player.Instance)
            Unbind();
    }

    public void Bind()
    {
        Player.Instance.OnFatigueChange += HandleFatigueChange;
        panelRoot.SetActive(true);
        HandleFatigueChange();
    }

    public void Unbind()
    {
        Player.Instance.OnFatigueChange -= HandleFatigueChange;
        panelRoot.SetActive(false);
    }

    private void HandleFatigueChange()
    {
        fatigueBar.DOFillAmount(Player.Instance.Fatigue / StatConstants.normalMaxFatigue, .2f).SetEase(Ease.Linear);
    }
}
