using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AbilityDatabase
{
    private static List<Ability> abilities = new List<Ability>();

    public static void InitializeAbilitiesDatabase(TextAsset jsonAbilities)
    {
        abilities = JsonHelper.FromJson<Ability>(jsonAbilities.text);

        foreach(Ability ability in abilities)
        {
            ability.icon = Resources.Load<Sprite>("Abilities/Sprites/" + ability.title);
        }
    }
    
    public static Ability GetAbility(int id)
    {
        return abilities.Find(ability => ability.id == id);
    }

    public static Ability GetAbility(string abilityName)
    {
        return abilities.Find(ability => ability.title == abilityName);
    }

    public static Ability GetRandomAbility()
    {
        List<Ability> abilitiesToAdd = new List<Ability>();

        foreach(var ability in abilities)
        {
            if (!PlayerInfo.characterAbilities.Any(x => x.id == ability.id))
            {
                abilitiesToAdd.Add(ability);
            }
        }

        return abilitiesToAdd[Random.Range(0, abilitiesToAdd.Count)];
    }
}
