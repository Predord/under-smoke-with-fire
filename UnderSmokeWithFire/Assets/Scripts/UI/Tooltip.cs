using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

public class Tooltip : MonoBehaviour
{
    public string abilitiesTableCollectionName = "Abilities";
    public string abilitiesStatsTableCollectionName = "Abilities_stats";

    public string buffsDebuffsTableCollectionName = "BuffsDebuffs";
    public string buffsDebuffsStatsTableCollectionName = "BuffsDebuffs_stats";

    private bool localizationInitializationOperationIsDone;
    private Coroutine generatingTooltip;
    private Ability currentAbility;
    private BuffDebuff currentBuffDebuff;
    private TMP_Text tooltipText;
    private Transform _transform;
    private RectTransform rectTransform;

    private string m_TranslatedAbilityName;
    private string m_TranslatedAbilityDescription;
    private string m_TranslatedAbilityRank;

    private string[] m_TranslatedAbilityStats = new string[Enum.GetNames(typeof(AbilityStats)).Length];

    private string m_TranslatedBuffDebuffName;
    private string m_TranslatedBuffDebuffDescription;

    private string m_TranslatedMaxAbilityLevel;
    private string m_TranslatedDodgeModifier;
    private string m_TranslatedSightModifier;
    private string m_TranslatedMaxHealthModifier;
    private string m_TranslatedDamageModifier;
    private string m_TranslatedCooldownModifier;
    private string m_TranslatedCastTimeModifier;
    private string m_TranslatedCritDamageModifier;
    private string m_TranslatedCritChanceModifier;
    private string[] m_TranslatedBuffDebuffStats = new string[Enum.GetNames(typeof(StatType)).Length];
    private string[] m_TranslatedBuffDebuffResists = new string[Enum.GetNames(typeof(NegativeEffects)).Length];

    private void Awake()
    {
        _transform = transform;
        rectTransform = gameObject.GetComponentsInChildren<RectTransform>()[1];
        tooltipText = rectTransform.GetComponentInChildren<TMP_Text>();
        rectTransform.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        StartCoroutine(LoadLocalizedTooltip());
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        rectTransform.gameObject.SetActive(false);
        generatingTooltip = null;
    }
    private void OnSelectedLocaleChanged(Locale obj)
    {
        StartCoroutine(LoadLocalizedTooltip());
    }

    private IEnumerator LoadLocalizedTooltip()
    {
        localizationInitializationOperationIsDone = false;

        if(currentAbility != null)
        {
            var loadingOperation = LocalizationSettings.StringDatabase.GetTableAsync(abilitiesTableCollectionName);
            yield return loadingOperation;

            if (loadingOperation.Status == AsyncOperationStatus.Succeeded)
            {
                var stringTable = loadingOperation.Result;
                m_TranslatedAbilityName = GetLocalizedString(stringTable, currentAbility.title + "_name");
                m_TranslatedAbilityDescription = GetLocalizedString(stringTable, currentAbility.title + "_description");
            }
            else
            {
                Debug.LogError("Could not load Abilities Table\n" + loadingOperation.OperationException.ToString());
            }

            loadingOperation = LocalizationSettings.StringDatabase.GetTableAsync(abilitiesStatsTableCollectionName);
            yield return loadingOperation;

            if (loadingOperation.Status == AsyncOperationStatus.Succeeded)
            {
                var stringTable = loadingOperation.Result;
                m_TranslatedAbilityRank = GetLocalizedString(stringTable, "Rank");
                m_TranslatedAbilityStats[0] = GetLocalizedString(stringTable, "Power");
                m_TranslatedAbilityStats[1] = GetLocalizedString(stringTable, "CastTime");
                m_TranslatedAbilityStats[2] = GetLocalizedString(stringTable, "Cooldown");
                m_TranslatedAbilityStats[3] = GetLocalizedString(stringTable, "MaxDistance");
                m_TranslatedAbilityStats[4] = GetLocalizedString(stringTable, "AOERadius");
                m_TranslatedAbilityStats[5] = GetLocalizedString(stringTable, "AbilitySpeed");
                //m_TranslatedAbilityStats[6] = GetLocalizedString(stringTable, "InflictedCellHazard");
                //m_TranslatedAbilityStats[7] = GetLocalizedString(stringTable, "TrajectoryType");
            }
            else
            {
                Debug.LogError("Could not load Stats Table\n" + loadingOperation.OperationException.ToString());
            }
        }

        if(currentBuffDebuff != null)
        {
            var loadingOperation = LocalizationSettings.StringDatabase.GetTableAsync(buffsDebuffsTableCollectionName);
            yield return loadingOperation;

            if (loadingOperation.Status == AsyncOperationStatus.Succeeded)
            {
                var stringTable = loadingOperation.Result;
                m_TranslatedBuffDebuffName = GetLocalizedString(stringTable, currentBuffDebuff.title + "_name");
                m_TranslatedBuffDebuffDescription = GetLocalizedString(stringTable, currentBuffDebuff.title + "_description");
            }
            else
            {
                Debug.LogError("Could not load Abilities Table\n" + loadingOperation.OperationException.ToString());
            }

            loadingOperation = LocalizationSettings.StringDatabase.GetTableAsync(buffsDebuffsStatsTableCollectionName);
            yield return loadingOperation;

            if (loadingOperation.Status == AsyncOperationStatus.Succeeded)
            {
                var stringTable = loadingOperation.Result;
                m_TranslatedMaxAbilityLevel = GetLocalizedString(stringTable, "Max_Ability_Level_Modifier");
                m_TranslatedDodgeModifier = GetLocalizedString(stringTable, "Dodge_Modifier");
                m_TranslatedSightModifier = GetLocalizedString(stringTable, "Sight_Modifier");
                m_TranslatedMaxHealthModifier = GetLocalizedString(stringTable, "Max_Health_Modifier");
                m_TranslatedDamageModifier = GetLocalizedString(stringTable, "Damage_Modifier");
                m_TranslatedCooldownModifier = GetLocalizedString(stringTable, "Cooldown_Modifier");
                m_TranslatedCastTimeModifier = GetLocalizedString(stringTable, "Cast_Time_Modifier");
                m_TranslatedCritDamageModifier = GetLocalizedString(stringTable, "Crit_Damage_Modifier");
                m_TranslatedCritChanceModifier = GetLocalizedString(stringTable, "Crit_Chance_Modifier");

                m_TranslatedBuffDebuffStats[0] = GetLocalizedString(stringTable, "Intelligence_Modifier");
                m_TranslatedBuffDebuffStats[1] = GetLocalizedString(stringTable, "Memory_Modifier");
                m_TranslatedBuffDebuffStats[2] = GetLocalizedString(stringTable, "Speed_Modifier");
                m_TranslatedBuffDebuffStats[3] = GetLocalizedString(stringTable, "Wits_Modifier");
                m_TranslatedBuffDebuffStats[4] = GetLocalizedString(stringTable, "Constitution_Modifier");

                m_TranslatedBuffDebuffResists[0] = GetLocalizedString(stringTable, "Knockdown_Modifier");
                m_TranslatedBuffDebuffResists[1] = GetLocalizedString(stringTable, "Stun_Modifier");
                m_TranslatedBuffDebuffResists[2] = GetLocalizedString(stringTable, "Fire_Modifier");
                m_TranslatedBuffDebuffResists[3] = GetLocalizedString(stringTable, "Bleed_Modifier");
            }
            else
            {
                Debug.LogError("Could not load Stats Table\n" + loadingOperation.OperationException.ToString());
            }
        }

        localizationInitializationOperationIsDone = true;
    }

    private string GetLocalizedString(StringTable table, string entryName)
    {
        var entry = table.GetEntry(entryName);
        return entry.GetLocalizedString(); 
    }

    public void SetAbilityForTooltip(Ability ability, Vector3 uiPosition, bool isHotBar, bool worldToScreenChange)
    {
        currentAbility = ability;
        currentBuffDebuff = null;

        gameObject.SetActive(true);

        if (worldToScreenChange)
        {
            uiPosition = Camera.current.WorldToScreenPoint(uiPosition);
        }

        if(generatingTooltip == null)
            generatingTooltip = StartCoroutine(GenerateTooltip(uiPosition, isHotBar));
    }

    public void SetBuffDebuffForTooltip(BuffDebuff buffDebuff, Vector3 uiPosition)
    {
        currentAbility = null;
        currentBuffDebuff = buffDebuff;

        gameObject.SetActive(true);

        if (generatingTooltip == null)
            generatingTooltip = StartCoroutine(GenerateTooltip(uiPosition));
    }

    private IEnumerator GenerateTooltip(Vector3 uiPosition, bool isHotBar = false)
    {
        yield return new WaitUntil(() => localizationInitializationOperationIsDone);

        string tooltip;

        StringBuilder statText = new StringBuilder(64);

        if(currentAbility != null)
        {
            for (int i = 0; i < Enum.GetNames(typeof(AbilityStats)).Length; i++)
            {
                statText.Append(m_TranslatedAbilityStats[i]).AppendFormat(": {0}", currentAbility.GetStatValue((AbilityStats)i));
                statText.AppendLine();
            }

            tooltip = string.Format("{0} \n {1} \n{2}: {3} \n\n {4}",
                                    m_TranslatedAbilityName, m_TranslatedAbilityDescription, m_TranslatedAbilityRank, currentAbility.Rank, statText.ToString());

            if (isHotBar)
            {
                _transform.position = new Vector3(uiPosition.x, uiPosition.y * 2f + rectTransform.sizeDelta.y, uiPosition.z);
            }
            else
            {
                _transform.position = new Vector3(uiPosition.x + 30f, uiPosition.y, uiPosition.z);
            }
        }
        else
        {
            for (int i = 0; i < Enum.GetNames(typeof(StatType)).Length; i++)
            {
                int statModifierValue = currentBuffDebuff.GetStatModifierValue((StatType)i);

                if(statModifierValue != 0)
                {
                    statText.Append(m_TranslatedBuffDebuffStats[i]).AppendFormat(": {0}", statModifierValue);
                    statText.AppendLine();
                }
            }

            for (int i = 0; i < Enum.GetNames(typeof(NegativeEffects)).Length; i++)
            {
                float resistModifierValue = currentBuffDebuff.GetStatModifierValue((NegativeEffects)i);

                if (resistModifierValue != 0)
                {
                    statText.Append(m_TranslatedBuffDebuffResists[i]).AppendFormat(": {0}", resistModifierValue);
                    statText.AppendLine();
                }
            }

            if(currentBuffDebuff.maxAbilityLevelModifier != 0)
            {
                statText.Append(m_TranslatedMaxAbilityLevel).AppendFormat(": {0}", currentBuffDebuff.maxAbilityLevelModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.dodgeModifier != 0)
            {
                statText.Append(m_TranslatedDodgeModifier).AppendFormat(": {0}", currentBuffDebuff.dodgeModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.sightModifier != 0)
            {
                statText.Append(m_TranslatedSightModifier).AppendFormat(": {0}", currentBuffDebuff.sightModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.maxHealthModifier != 0)
            {
                statText.Append(m_TranslatedMaxHealthModifier).AppendFormat(": {0}", currentBuffDebuff.maxHealthModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.damageModifier != 0)
            {
                statText.Append(m_TranslatedDamageModifier).AppendFormat(": {0}", currentBuffDebuff.damageModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.cooldownModifier != 0)
            {
                statText.Append(m_TranslatedCooldownModifier).AppendFormat(": {0}", currentBuffDebuff.cooldownModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.castTimeModifier != 0)
            {
                statText.Append(m_TranslatedCastTimeModifier).AppendFormat(": {0}", currentBuffDebuff.castTimeModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.critDamageModifier != 0)
            {
                statText.Append(m_TranslatedCritDamageModifier).AppendFormat(": {0}", currentBuffDebuff.critDamageModifier);
                statText.AppendLine();
            }

            if (currentBuffDebuff.critChanceModifier != 0)
            {
                statText.Append(m_TranslatedCritChanceModifier).AppendFormat(": {0}", currentBuffDebuff.critChanceModifier);
                statText.AppendLine();
            }

            tooltip = string.Format("{0} \n {1} \n\n {2}",
                        m_TranslatedBuffDebuffName, m_TranslatedBuffDebuffDescription, statText.ToString());

            _transform.position = new Vector3(uiPosition.x + 30f, uiPosition.y, uiPosition.z);
        }

        tooltipText.text = tooltip;
        rectTransform.gameObject.SetActive(true);
        generatingTooltip = null;
    }
}
