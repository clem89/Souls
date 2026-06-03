using System;

[Serializable]
public class SkillDef
{
    public string   skillId;
    public string   effectId;
    public float    baseCoefficient;
    public string   description;
    public string[] nextSkillIds;
}
