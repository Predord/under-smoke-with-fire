using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIAbility : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool inAbilityList;
    public bool isHotBar;
    public bool interactable = true;
    public Ability ability;
    public Image CooldownCover;

    private static bool fromHotBar;
    private Transform _transform;
    private Image spriteImage;
    private UIAbility selectedAbility;
    private Tooltip tooltip;
    private UIBasicAbility activeAbilityTab;
    private UIBasicAbility hotBarTab;

#pragma warning disable 0649
    [SerializeField] private bool isSelectedAbility;
#pragma warning restore 0649

    private void Awake()
    {
        spriteImage = GetComponent<Image>();
        _transform = transform;
        UpdateAbility(null);
    }

    private void Start()
    {
        if (!isSelectedAbility)
        {
            tooltip = GameUI.Instance.tooltip;

            if (interactable)
            {
                selectedAbility = GameUI.Instance.selectedAbility;
                activeAbilityTab = GameUI.Instance.activeAbilityTab;
                hotBarTab = GameUI.Instance.hotBarPanel;
            }
        }
    }

    private void OnDestroy()
    {
        if(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            tooltip.gameObject.SetActive(false);
        }
    }

    public void UpdateAbility(Ability ability)
    {
        this.ability = ability;

        if (ability != null)
        {
            spriteImage.color = Color.white;
            spriteImage.sprite = ability.icon;
            if (!inAbilityList && !isSelectedAbility)
            {
                if (Player.Instance)
                {
                    CooldownCover.fillAmount = Player.Instance.activeAbilitiesCooldowns[ability] / ability.GetStatValue(AbilityStats.Cooldown);
                }
                else
                {
                    CooldownCover.fillAmount = 0f;
                }               
            }
        }
        else
        {
            spriteImage.color = Color.clear;
            if (!isSelectedAbility)
            {
                CooldownCover.fillAmount = 0f;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable && eventData.button == PointerEventData.InputButton.Left)
        {
            if (ability != null)
            {
                selectedAbility.UpdateAbility(ability);

                if (isHotBar)
                {
                    UpdateAbility(null);
                    PlayerInfo.hotBarAbilities[_transform.parent.GetSiblingIndex()] = null;
                    fromHotBar = true;
                }
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (interactable)
        {
            if (selectedAbility.ability != null && eventData.button == PointerEventData.InputButton.Left)
            {
                if (!inAbilityList && IsMouseOnActiveSlot())
                {
                    if (!isHotBar && !fromHotBar)
                    {
                        if (!GameManager.Instance.IsActionMap)
                        {
                            foreach (var uIability in activeAbilityTab.UIAbilities)
                            {
                                if (uIability.ability == selectedAbility.ability)
                                {
                                    if (ability != null)
                                    {
                                        PlayerInfo.activeAbilities.Remove(ability);
                                        for (int i = 0; i < PlayerInfo.hotBarMaxSlots; i++)
                                        {
                                            if (PlayerInfo.hotBarAbilities[i] == ability)
                                            {
                                                PlayerInfo.hotBarAbilities[i] = null;
                                                hotBarTab.UpdateSlot(i, null);
                                            }
                                        }
                                    }

                                    uIability.UpdateAbility(null);
                                    UpdateAbility(selectedAbility.ability);
                                    selectedAbility.UpdateAbility(null);
                                    return;
                                }
                            }

                            if (ability != null)
                            {
                                PlayerInfo.activeAbilities.Remove(ability);

                                for (int i = 0; i < PlayerInfo.hotBarMaxSlots; i++)
                                {
                                    if (PlayerInfo.hotBarAbilities[i] == ability)
                                    {
                                        PlayerInfo.hotBarAbilities[i] = null;
                                        hotBarTab.UpdateSlot(i, null);
                                    }
                                }
                            }

                            PlayerInfo.activeAbilities.Add(selectedAbility.ability);
                            UpdateAbility(selectedAbility.ability);
                        }
                    }
                    else if (isHotBar)
                    {
                        if (PlayerInfo.activeAbilities.Contains(selectedAbility.ability))
                        {
                            PlayerInfo.hotBarAbilities[_transform.parent.GetSiblingIndex()] = selectedAbility.ability;
                            UpdateAbility(selectedAbility.ability);
                            selectedAbility.UpdateAbility(null);
                        }
                    }
                }

                fromHotBar = false;
                selectedAbility.UpdateAbility(null);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                selectedAbility.UpdateAbility(null);
                if (ability != null && !inAbilityList)
                {
                    if (!isHotBar)
                    {
                        if (GameManager.Instance.IsActionMap)
                        {
                            PlayerInfo.activeAbilities.Remove(ability);
                            for (int i = 0; i < PlayerInfo.hotBarMaxSlots; i++)
                            {
                                if (PlayerInfo.hotBarAbilities[i] == ability)
                                {
                                    PlayerInfo.hotBarAbilities[i] = null;
                                    hotBarTab.UpdateSlot(i, null);
                                }
                            }

                            UpdateAbility(null);
                        }
                    }
                    else
                    {
                        PlayerInfo.hotBarAbilities[_transform.parent.GetSiblingIndex()] = null;
                        UpdateAbility(null);
                    }
                }
            }
        }       
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ability != null && !isSelectedAbility && tooltip != null)
        {
            tooltip.SetAbilityForTooltip(ability, _transform.position, isHotBar, !interactable);
        }

        eventData.pointerPress = gameObject;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.gameObject.SetActive(false);
    }

    public void AddActiveComponent()
    {
        UIAbilityActive active = gameObject.AddComponent<UIAbilityActive>();
    }

    private bool IsMouseOnActiveSlot()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = GameManager.Instance.mouse.position.ReadValue()
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            if (raycastResults[i].gameObject.GetComponent<UIAbilityActive>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
