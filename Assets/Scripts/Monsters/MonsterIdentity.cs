using UnityEngine;

// Stable identity component on monster prefab Root.
// Bridges Prefab asset → runtime MonsterArchetype registry by archetypeId string.
// Future: MonsterRenderer / MonsterManager will instantiate the prefab and read this to resolve the archetype.
public class MonsterIdentity : MonoBehaviour
{
    [SerializeField] private string archetypeId = "slime";

    public string ArchetypeId => archetypeId;

    public MonsterArchetype Resolve()
    {
        if (string.IsNullOrEmpty(archetypeId))
        {
            Debug.LogWarning($"[MonsterIdentity] archetypeId is empty on {gameObject.name}.");
            return null;
        }
        var archetype = MonsterArchetypeRegistry.Get(archetypeId);
        if (archetype == null)
            Debug.LogWarning($"[MonsterIdentity] No archetype registered for id '{archetypeId}' on {gameObject.name}.");
        return archetype;
    }
}
