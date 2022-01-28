using UnityEngine;
using UnityEngine.UI;

public class GameUI : Singleton<GameUI>
{
    public Color positiveValueDifferenceColor = Color.green;
    public Color zeroValueDifferenceColor = Color.black;
    public Color negativeValueDifferenceColor = Color.red;

    [HideInInspector]
    public string activeObjectiveName;

    public UIActiveAbilities activeAbilityTab;
    public HotbarTab hotBarPanel;
    public Transform abilityPanel;
    public Transform statsPanel;
    public Transform objectivesPanel;
    public Menu menuPanel;
    public UIAbility selectedAbility;
    public Tooltip tooltip;

    public ObjectiveItem objectivePrefab;

    public string[] objectiveNames;

#pragma warning disable 0649
    [SerializeField] private UIBasicAbility abilityListUI;
    [SerializeField] private UIBuffDebuffList buffDebuffListUI;
#pragma warning restore 0649

    private void Awake()
    {
        if (!RegisterMe())
        {
            return;
        }
        tooltip.gameObject.SetActive(false);
    }

    public void OnChangeScene(bool isSceneAction)
    {
        hotBarPanel.gameObject.SetActive(isSceneAction);
        objectivesPanel.parent.gameObject.SetActive(isSceneAction);
    }

    public void HandleAbilityAdd(Ability abilityToAdd)
    {
        if (abilityListUI.transform.parent.gameObject.activeSelf)
            abilityListUI.AddAbility(abilityToAdd);
    }

    public void HandleBuffDebuffAdd(BuffDebuff buffDebuffToAdd)
    {
        if (buffDebuffListUI.gameObject.activeSelf)
            buffDebuffListUI.AddBuffDebuff(buffDebuffToAdd);
    }

    public void HandleBuffDebuffRemove(BuffDebuff buffDebuffToRemove)
    {
        if (buffDebuffListUI.gameObject.activeSelf)
            buffDebuffListUI.RemoveSlot(buffDebuffToRemove);
    }

    public void InitializePlayerUI()
    {
        hotBarPanel.InitializeHotBar();
    }

    public void OpenPlayerActionList()
    {
        if (statsPanel.gameObject.activeSelf)
            statsPanel.gameObject.SetActive(false);

        if (!GameManager.Instance.IsActionMap) 
        {
            hotBarPanel.gameObject.SetActive(!abilityPanel.gameObject.activeSelf);
        }

        abilityPanel.gameObject.SetActive(!abilityPanel.gameObject.activeSelf);
        if (!abilityPanel.gameObject.activeSelf)
        {
            tooltip.gameObject.SetActive(false);
            selectedAbility.UpdateAbility(null);
        }       
    }

    public void OpenPlayerStats()
    {
        if (abilityPanel.gameObject.activeSelf)
        {
            abilityPanel.gameObject.SetActive(false);
            tooltip.gameObject.SetActive(false);

            selectedAbility.UpdateAbility(null);
        }

        if (!GameManager.Instance.IsActionMap)
        {
            hotBarPanel.gameObject.SetActive(false);
        }

        statsPanel.gameObject.SetActive(!statsPanel.gameObject.activeSelf);
    }

    public void CloseAll()
    {
        abilityPanel.gameObject.SetActive(false);        
        tooltip.gameObject.SetActive(false);
        statsPanel.gameObject.SetActive(false);

        selectedAbility.UpdateAbility(null);

        if (!GameManager.Instance.IsActionMap)
        {
            hotBarPanel.gameObject.SetActive(false);
        }
    }

    public void OpenMenu()
    {
        menuPanel.gameObject.SetActive(true);
        GameManager.Instance.Pause();
    }

    public void CloseMenu()
    {
        if (menuPanel.CloseMenu())
        {
            GameManager.Instance.UnPause();
        }
    }

    public void SetCoolDownToAbility(float cooldown, Ability ability)
    {
        activeAbilityTab.UpdateAbilitiesCooldown(cooldown, ability);
        hotBarPanel.UpdateAbilitiesCooldown(cooldown, ability);
    }

    public void UpdateObjectiveItem(bool isObjectiveActive, bool isObjectiveComplete, int objectiveId, QuadCell targetCell)
    {
        activeObjectiveName = objectiveNames[objectiveId];

        if (isObjectiveActive)
        {
            for(int i = 0; i < objectivesPanel.childCount; i++)
            {
                if(objectivesPanel.GetChild(i).GetComponent<LocalizedObjectiveChangeHandler>().objectiveId == objectiveId)
                {
                    objectivesPanel.GetChild(i).GetChild(0).GetComponent<Toggle>().isOn = isObjectiveComplete;
                    break;
                }
            }
        }
        else
        {
            ObjectiveItem objective = Instantiate(objectivePrefab);
            objective.transform.SetParent(objectivesPanel);
            objective.GetComponent<LocalizedObjectiveChangeHandler>().objectiveId = objectiveId;
            objective.targetCell = targetCell;
            objective.transform.GetChild(0).GetComponent<Toggle>().isOn = isObjectiveComplete;
        }
    }

    public Color GetStatsPanelColor(int valueDifference)
    {
        if(valueDifference > 0)
        {
            return positiveValueDifferenceColor;
        }
        else if(valueDifference < 0)
        {
            return negativeValueDifferenceColor;
        }
        else
        {
            return zeroValueDifferenceColor;
        }
    }

    public Color GetStatsPanelColor(float valueDifference)
    {
        if (valueDifference > 0f)
        {
            return positiveValueDifferenceColor;
        }
        else if (valueDifference < 0f)
        {
            return negativeValueDifferenceColor;
        }
        else
        {
            return zeroValueDifferenceColor;
        }
    }
}
