using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBuffDebuff : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public BuffDebuff buffDebuff;

    private Image spriteImage;
    private Transform _transform;
    private Tooltip tooltip;

    private void Awake()
    {
        spriteImage = GetComponent<Image>();
        _transform = transform;
    }

    private void Start()
    {
        tooltip = GameUI.Instance.tooltip;
    }

    public void UpdateBuffDebuff(BuffDebuff buffDebuff)
    {
        this.buffDebuff = buffDebuff;

        if (buffDebuff != null)
        {
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
            tooltip.SetBuffDebuffForTooltip(buffDebuff, _transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.gameObject.SetActive(false);
    }
}
