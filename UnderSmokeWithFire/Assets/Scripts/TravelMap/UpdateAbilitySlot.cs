using UnityEngine;
using UnityEngine.UI;

public class UpdateAbilitySlot : MonoBehaviour
{
    public Button updateButton;

    private Transform _transform;
    private Ability upgradedAbility;

    private void Awake()
    {
        _transform = transform;

        ControlUpdateButton(DailyActions.Instance.CurrentHoursToSpend >= DailyActions.hoursToSpendForUpgrade);
    }

    private void OnEnable()
    {
        DailyActions.Instance.OnUpdateAbilityPerform += ControlUpdateButton;
    }

    private void OnDisable()
    {
        DailyActions.Instance.OnUpdateAbilityPerform -= ControlUpdateButton;
    }

    public void InstantiateUpdateAbilitySlot(Ability ability, int upgradedRank)
    {
        SetAbilitySlots(ability, upgradedRank);
    }

    public void UpgradeAbility()
    {
        PlayerInfo.characterAbilities.Find(ability => ability.id == upgradedAbility.id).Rank = upgradedAbility.Rank;
        DailyActions.Instance.CurrentHoursToSpend -= DailyActions.hoursToSpendForUpgrade;

        SetAbilitySlots(upgradedAbility, upgradedAbility.Rank + 1);
    }

    public void ControlUpdateButton(bool enabled)
    {
        updateButton.interactable = enabled;
    }

    private void SetAbilitySlots(Ability ability, int upgradedRank) 
    {
        if(upgradedRank > PlayerInfo.GetMaxAbilityLevelUnchanged())
        {
            Destroy(gameObject);
            return;
        }

        _transform.GetChild(0).GetComponentInChildren<UIAbility>().UpdateAbility(ability);

        upgradedAbility = new Ability(ability.id, ability.title, ability.isTrap, ability.isLeavingTrail, ability.excludeTargetCell, ability.isOnUnitUse, 
            ability.targetedEntitiesMask, ability.afterCastAbilityIndex, ability.trajectoryType, ability.cellHazard, ability.projectilesCount, ability.stats, upgradedRank, ability.icon);

        _transform.GetChild(1).GetComponentInChildren<UIAbility>().UpdateAbility(upgradedAbility);
    }
}
