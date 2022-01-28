using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;

public class DailyActions : Singleton<DailyActions>
{
    public const int hoursToSpendForUpgrade = 4;
    public const int hoursToSpendForDevelop = 6;
    public const int hoursToScoutArea = 2;
    public const int hoursToAlertGarnison = 2;
    public const int additionalGarnisonStrengthWhenAlerted = 1;
    public const int daySkipsPerRun = 1;

    public UIAbility developAbilityIcon;
    public UpdateAbilitySlot updateAbilitiesSlotPrefab;

    public Button sleepButton;
    public Button developAbilityButton;
    public Button scoutAreaButton;
    public Button alertGarnisonButton;
    public Button skipDayButton;
    public Toggle[] sleepToggles;
    public TMP_Text[] remainingHours;
    public Transform updateAbilitiesContent;
    public Transform developAbilityPanel;

    public Action<bool> OnUpdateAbilityPerform;

    private int sleepIndex = 0;
    private int hoursToSleep = 4;
    private static int skipedDays;
    private const int hoursToSpendPerDay = 12;

    private float[][] healthFatigueChange =
    {
        new float[] { 50f, -10f },
        new float[] { 100f, -40f},
        new float[] { 150f, -70f},
        new float[] { 200f, -100f}
    };
    
    private TMP_Text sleepButtonText;

    private void Awake()
    {
        if (!RegisterMe())
        {
            return;
        }

        sleepButtonText = sleepButton.GetComponentInChildren<TMP_Text>();

        OnNewDayStart(false);
    }

    public int CurrentHoursToSpend
    {
        get
        {
            return currentHoursToSpend;
        }
        set
        {
            if (value == currentHoursToSpend)
                return;

            currentHoursToSpend = value;

            for(int i = 0; i < 4; i++)
            {
                if(currentHoursToSpend < 4 + 2 * i)
                {
                    if(i != 0)
                    {
                        sleepToggles[i - 1].isOn = true;
                        sleepButton.interactable = true;
                        sleepButtonText.color = new Color(1f, 0f, 0f, 1f);

                    }
                    else
                    {
                        sleepButton.interactable = false;
                        sleepButtonText.color = new Color(1f, 0f, 0f, 0.3f);
                    }

                    sleepToggles[i].isOn = false;

                    for (int j = i; j < 4; j++)
                    {
                        sleepToggles[j].interactable = false;
                    }

                    break;
                }
                else
                {
                    sleepButton.interactable = true;
                    sleepToggles[i].interactable = true;
                }
            }

            if (currentHoursToSpend < hoursToSpendForUpgrade)
            {
                OnUpdateAbilityPerform?.Invoke(false);
            }

            developAbilityButton.interactable = currentHoursToSpend >= hoursToSpendForDevelop;

            if (currentHoursToSpend < hoursToScoutArea)
            {
                scoutAreaButton.interactable = false;
            }

            if(currentHoursToSpend < hoursToAlertGarnison)
            {
                alertGarnisonButton.interactable = false;
            }

            for (int i = 0; i < remainingHours.Length; i++)
            {
                remainingHours[i].text = string.Format("{0}h", currentHoursToSpend);
            }            
        }
    }

    private int currentHoursToSpend;

    public Location CurrentChoosenLocation
    {
        get
        {
            return currentChoosenLocation;
        }
        set
        {
            if (currentChoosenLocation == value)
                return;

            currentChoosenLocation = value;

            if (currentChoosenLocation == null)
                return;

            scoutAreaButton.interactable = CurrentHoursToSpend >= hoursToScoutArea && !currentChoosenLocation.playerScoutedArea;

            alertGarnisonButton.interactable = CurrentHoursToSpend >= hoursToAlertGarnison && !currentChoosenLocation.PlayerAlertedGarnison
                && currentChoosenLocation.GarnisonStrength < TravelPath.maxLocationStrength;
        }
    }

    private Location currentChoosenLocation;

    public void OnNewDayStart(bool skiped)
    {
        //change to locations hasScoutedArea when add skip day
        PlayerInfo.HasScoutedArea = false;
        GameManager.Instance.CurrentMapStrength = 0;
        DisableScoutAlertArea();

        OnUpdateAbilityPerform?.Invoke(true);

        CurrentHoursToSpend = hoursToSpendPerDay;

        if (skiped)
        {
            skipedDays++;
        }

        if (skipedDays >= daySkipsPerRun)
        {
            skipDayButton.interactable = false;
        }
    }

    public void ChangeHoursToSleep(int value)
    {
        if (value == hoursToSleep)
            return;

        hoursToSleep = value;
    }

    public void ChangeSleepIndex(int value)
    {
        if (value == sleepIndex)
            return;

        sleepIndex = value;
    }

    public void ActionSleep()
    {
        PlayerInfo.Health += healthFatigueChange[sleepIndex][0];
        PlayerInfo.Fatigue += healthFatigueChange[sleepIndex][1];

        CurrentHoursToSpend -= hoursToSleep;
    }

    public void OnOpenUpdateWindow()
    {
        for (int i = updateAbilitiesContent.childCount - 1; i >= 0; i--) 
        { 
            Destroy(updateAbilitiesContent.GetChild(i).gameObject); 
        }

        foreach(var ability in PlayerInfo.characterAbilities)
        {
            if (ability.Rank < Ability.maxRank)
            {
                InstantiateUpdateAbilitySlot(ability, ability.Rank + 1);
            }        
        }
    }

    private void InstantiateUpdateAbilitySlot(Ability ability, int upgradedRank)
    {
        UpdateAbilitySlot instance = Instantiate(updateAbilitiesSlotPrefab);
        instance.transform.SetParent(updateAbilitiesContent, false);

        instance.InstantiateUpdateAbilitySlot(ability, upgradedRank);
    }

    public void OnOpenDevelopAbilityWindow()
    {
        Ability abilityToAdd = AbilityDatabase.GetRandomAbility();
        PlayerInfo.GiveAbility(abilityToAdd.id, 0);

        developAbilityPanel.gameObject.SetActive(true);
        developAbilityIcon.UpdateAbility(abilityToAdd);

        CurrentHoursToSpend -= hoursToSpendForDevelop;
    }

    public void OnCloseDevelopAbilityWindow()
    {
        developAbilityPanel.gameObject.SetActive(false);
    }

    public void OnScoutAreaAction()
    {
        CurrentChoosenLocation.playerScoutedArea = true;
        scoutAreaButton.interactable = false;

        CurrentHoursToSpend -= hoursToScoutArea;
    }

    public void OnAlertGarnisonAction()
    {
        CurrentChoosenLocation.PlayerAlertedGarnison = true;
        alertGarnisonButton.interactable = false;

        CurrentHoursToSpend -= hoursToAlertGarnison;
    }

    public void DisableScoutAlertArea()
    {
        scoutAreaButton.interactable = false;
        alertGarnisonButton.interactable = false;

        currentChoosenLocation = null;
    }

    public static void Save(BinaryWriter writer)
    {
        writer.Write((byte)skipedDays);
    }

    public static void Load(BinaryReader reader, int header)
    {
        skipedDays = reader.ReadByte();
    }
}
