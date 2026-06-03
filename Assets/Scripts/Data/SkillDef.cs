using System;

[Serializable]
public class SkillDef
{
    public string skillId;
    public string effectId;
    public float  baseCoefficient;
    public string description;
    public string parentSkillId;  // null or empty = base skill (no prerequisite)
}
