using UnityEngine;

namespace ContentForge
{
    /// <summary>A generated game enemy asset. Fields mirror the server contract 1:1.</summary>
    [CreateAssetMenu(menuName = "Content Forge/Enemy Definition", fileName = "NewEnemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string enemyName;
        [TextArea] public string description;
        public int level;
        public int health;
        public int damage;
    }
}
