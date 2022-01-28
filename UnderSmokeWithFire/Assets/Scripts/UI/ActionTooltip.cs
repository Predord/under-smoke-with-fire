using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

public class ActionTooltip : MonoBehaviour
{
    public string buffsDebuffsAMTableCollectionName = "BuffsDebuffsActionMap";
    public string buffsDebuffsAMStatsTableCollectionName = "BuffsDebuffsActionMap_stats";

    private bool localizationInitializationOperationIsDone;
    private Coroutine generatingTooltip;
    private BuffDebuffActionMap currentBuffDebuff;
    private TMP_Text tooltipText;
    private Transform _transform;
    private RectTransform rectTransform;

    private string m_TranslatedBuffDebuffName;
    private string m_TranslatedBuffDebuffDescription;

    private string m_TranslatedTurns;
    private string m_TranslatedDamageType;
    private string m_TranslatedDOT;
    private string m_TranslatedSightRaw;
    private string m_TranslatedSightModifier;

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

        if (currentBuffDebuff != null)
        {
            var loadingOperation = LocalizationSettings.StringDatabase.GetTableAsync(buffsDebuffsAMTableCollectionName);
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

            loadingOperation = LocalizationSettings.StringDatabase.GetTableAsync(buffsDebuffsAMStatsTableCollectionName);
            yield return loadingOperation;

            if (loadingOperation.Status == AsyncOperationStatus.Succeeded)
            {
                var stringTable = loadingOperation.Result;
                m_TranslatedTurns = GetLocalizedString(stringTable, "Turns_Amount");
                m_TranslatedDamageType = GetLocalizedString(stringTable, "Damage_Type");
                m_TranslatedDOT = GetLocalizedString(stringTable, "Damage_Over_Time");
                m_TranslatedSightRaw = GetLocalizedString(stringTable, "Sight_Raw");
                m_TranslatedSightModifier = GetLocalizedString(stringTable, "Sight_Modifier");
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

    public void SetBuffDebuffForTooltip(int turns, Vector3 uiPosition, BuffDebuffActionMap buffDebuff)
    {
        currentBuffDebuff = buffDebuff;

        gameObject.SetActive(true);

        if (generatingTooltip == null)
            generatingTooltip = StartCoroutine(GenerateTooltip(turns, uiPosition));
    }

    private IEnumerator GenerateTooltip(int turns, Vector3 uiPosition)
    {
        yield return new WaitUntil(() => localizationInitializationOperationIsDone);

        string tooltip;

        StringBuilder statText = new StringBuilder(64);

        statText.Append(m_TranslatedTurns).AppendFormat(": {0}", turns);
        statText.AppendLine();

        if (currentBuffDebuff.damageOverTime != 0)
        {
            statText.Append(m_TranslatedDamageType).AppendFormat(": {0}", (NegativeEffects)currentBuffDebuff.damageType);
            statText.AppendLine();
            statText.Append(m_TranslatedDOT).AppendFormat(": {0}", currentBuffDebuff.damageOverTime);
            statText.AppendLine();
        }

        if (currentBuffDebuff.sightRaw != -1)
        {
            statText.Append(m_TranslatedSightRaw).AppendFormat(": {0}", currentBuffDebuff.sightRaw);
            statText.AppendLine();
        }
        else
        {
            if (currentBuffDebuff.sightModifier != 0)
            {
                statText.Append(m_TranslatedSightModifier).AppendFormat(": {0}", currentBuffDebuff.sightModifier);
                statText.AppendLine();
            }
        }


        tooltip = string.Format("{0} \n {1} \n\n {2}",
            m_TranslatedBuffDebuffName, m_TranslatedBuffDebuffDescription, statText.ToString());

        _transform.position = new Vector3(uiPosition.x + 30f, uiPosition.y, uiPosition.z);      

        tooltipText.text = tooltip;
        rectTransform.gameObject.SetActive(true);
        generatingTooltip = null;
    }
}
