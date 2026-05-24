using UnityEngine;

[CreateAssetMenu(menuName = "Souls/Combat/Skill Effect Data", fileName = "SkillEffectData")]
public class SkillEffectData : ScriptableObject
{
    public GameObject effectPrefab;
    public float damage = 20f;
    public float attackRange = 1.2f;
    public float lifetime = 0.12f;
    [Min(1)] public int poolSize = 4;
}
