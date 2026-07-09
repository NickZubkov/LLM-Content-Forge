using UnityEngine;

namespace ContentForge
{
    /// <summary>A generated game item asset. Fields mirror the server contract 1:1.</summary>
    [CreateAssetMenu(menuName = "Content Forge/Item Definition", fileName = "NewItem")]
    public sealed class ItemDefinition : ScriptableObject
    {
        // Named itemName (not name) to avoid shadowing UnityEngine.Object.name.
        public string itemName;
        [TextArea] public string description;
        public Rarity rarity;
        public int power;
        public int value;
    }
}
