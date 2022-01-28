using System.Collections.Generic;
using UnityEngine;

public class ResistsList : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform statPrefab;
#pragma warning restore 0649

    private Dictionary<NegativeEffects, ResistHolder> holders = new Dictionary<NegativeEffects, ResistHolder>();

    private void OnEnable()
    {
        PlayerInfo.OnStatChange += HandleResistChangeWithStats;

        if (holders.Count == 0)
            InstantiateResists();
    }

    private void OnDestroy()
    {
        PlayerInfo.OnStatChange -= HandleResistChangeWithStats;
    }

    private void HandleResistChangeWithStats(StatData statData)
    {
        if (statData.StatType == StatType.Constitution)
        {
            float[] resists = PlayerInfo.GetResists();
            float[] resistsUnchanged = PlayerInfo.GetResistsUnchanged();

            for (int i = 0; i < resists.Length; i++)
            {
                holders[(NegativeEffects)i].SetData(resists[i], resistsUnchanged[i], (NegativeEffects)i);
            }
        }
    }

    private void InstantiateResists()
    {
        float[] resists = PlayerInfo.GetResists();
        float[] resistsUnchanged = PlayerInfo.GetResistsUnchanged();

        for (int i = 0; i < resists.Length; i++)
        {
            var instance = Instantiate(statPrefab, panelRoot);

            ResistHolder resistHolder = instance.gameObject.GetComponent<ResistHolder>();
            resistHolder.SetData(resists[i], resistsUnchanged[i], (NegativeEffects)i);
            instance.gameObject.GetComponent<LocalizedResistsChangeHandler>().currentEffect = (NegativeEffects)i;
            instance.gameObject.SetActive(true);
            holders.Add((NegativeEffects)i, resistHolder);
        }
    }
}
