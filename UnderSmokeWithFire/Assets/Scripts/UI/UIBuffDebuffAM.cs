using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBuffDebuffAM : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int turns;
    public BuffDebuffActionMap buffDebuff;

    private Image spriteImage;
    private Transform _transform;
    private ActionTooltip tooltip;

    private void Awake()
    {
        spriteImage = GetComponent<Image>();
        _transform = transform;
    }

    private void Start()
    {
        tooltip = ActionUI.Instance.tooltip;
    }

    public void UpdateBuffDebuff(BuffDebuffActionMap buffDebuff)
    {
        this.buffDebuff = buffDebuff;

        if (buffDebuff != null)
        {
            turns = buffDebuff.turnsAmount;
            spriteImage.color = Color.white;
            spriteImage.sprite = buffDebuff.icon;
        }
        else
        {
            spriteImage.color = Color.clear;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buffDebuff != null && tooltip != null)
        {
            tooltip.SetBuffDebuffForTooltip(turns, _transform.position, buffDebuff);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.gameObject.SetActive(false);
    }
}
