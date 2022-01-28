using System.Text;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class HealthPanel : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private Image healthBar;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private GameObject panelRoot;
#pragma warning restore 0649

    private Entity boundEntity;

    private void OnDisable()
    {
        if (boundEntity)
            boundEntity.OnHealthChange -= HandleHealthChange;
    }

    public void Bind(Entity entity)
    {
        if(boundEntity)
            boundEntity.OnHealthChange -= HandleHealthChange;

        boundEntity = entity;

        if (boundEntity)
        {
            panelRoot.SetActive(true);
            boundEntity.OnHealthChange += HandleHealthChange;
            HandleHealthChange();
        }
        else
        {
            panelRoot.SetActive(false);
        }
    }

    private void HandleHealthChange()
    {
        if(boundEntity.Health == 0)
        {
            boundEntity.OnHealthChange -= HandleHealthChange;
            boundEntity = null;
            
            panelRoot.SetActive(false);
        }
        else
        {
            if (healthText)
            {
                StringBuilder text = new StringBuilder(32);
                text.Append("Health: ").AppendFormat("{0}/{1}", boundEntity.Health, boundEntity.GetMaxHealth());
                healthText.SetText(text.ToString());
            }

            healthBar.DOFillAmount(boundEntity.Health / boundEntity.GetMaxHealth(), .2f).SetEase(Ease.Linear);
        }
    }
}
