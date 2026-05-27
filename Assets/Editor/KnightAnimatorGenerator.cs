using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class KnightAnimatorGenerator
{
    const string SPRITE_ROOT = "Assets/Resources/Characters(100x100)/Knight/Knight";
    const string OUTPUT_DIR  = "Assets/Animations/Knight";
    const string CTRL_PATH   = "Assets/Animations/KnightAnimator.controller";

    struct ClipDef
    {
        public string Name; public float Fps; public bool Loop;
        public ClipDef(string n, float f, bool l) { Name=n; Fps=f; Loop=l; }
    }

    [MenuItem("Tools/Animator Generator/Generate Knight Animator")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
            AssetDatabase.CreateFolder("Assets/Animations", "Knight");

        var defs = new[]
        {
            new ClipDef("Knight-Idle",     8f,  true),
            new ClipDef("Knight-Walk",     8f,  true),
            new ClipDef("Knight-Attack01", 10f, false),
            new ClipDef("Knight-Attack02", 10f, false),
            new ClipDef("Knight-Attack03", 10f, false),
            new ClipDef("Knight-Block",    8f,  true),
            new ClipDef("Knight-Hurt",     10f, false),
            new ClipDef("Knight-Death",    10f, false),
        };

        var clips = new Dictionary<string, AnimationClip>();
        foreach (var d in defs)
        {
            var clip = CreateClip(d);
            if (clip != null) clips[d.Name] = clip;
        }

        BuildController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[KnightAnimatorGenerator] 생성 완료");
    }

    static AnimationClip CreateClip(ClipDef def)
    {
        string pngPath = $"{SPRITE_ROOT}/{def.Name}.png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath)
                          .OfType<Sprite>()
                          .OrderBy(s => SpriteIndex(s.name))
                          .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[KnightAnimatorGenerator] 스프라이트 없음: {pngPath}");
            return null;
        }

        var clip = new AnimationClip { frameRate = def.Fps };

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = def.Loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding   = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / def.Fps, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        string clipPath = $"{OUTPUT_DIR}/{def.Name}.anim";
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    static int SpriteIndex(string name)
    {
        int u = name.LastIndexOf('_');
        return u >= 0 && int.TryParse(name.Substring(u + 1), out int idx) ? idx : 0;
    }

    static void BuildController(Dictionary<string, AnimationClip> clips)
    {
        if (File.Exists(CTRL_PATH)) AssetDatabase.DeleteAsset(CTRL_PATH);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CTRL_PATH);
        var sm   = ctrl.layers[0].stateMachine;

        ctrl.AddParameter("Speed",        AnimatorControllerParameterType.Float);
        ctrl.AddParameter("AttackStep",   AnimatorControllerParameterType.Int);
        ctrl.AddParameter("IsParrying",   AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("RiposteReady", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("HurtTrigger",  AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("IsDead",       AnimatorControllerParameterType.Bool);

        if (clips.Count == 0)
        {
            Debug.LogError("[KnightAnimatorGenerator] No clips loaded — run sprite slicer first (Tools > Sprite Slicer > Slice Character Sprites)");
            return;
        }

        var idle    = MakeState(sm, "Idle",    Clip(clips, "Knight-Idle"));
        var walk    = MakeState(sm, "Walk",    Clip(clips, "Knight-Walk"));
        var atk1    = MakeState(sm, "Attack1", Clip(clips, "Knight-Attack01"));
        var atk2    = MakeState(sm, "Attack2", Clip(clips, "Knight-Attack02"));
        var atk3    = MakeState(sm, "Attack3", Clip(clips, "Knight-Attack03"));
        var parry   = MakeState(sm, "Parry",   Clip(clips, "Knight-Block"));
        var riposte = MakeState(sm, "Riposte", Clip(clips, "Knight-Block")); // intentional: no separate Riposte sprite
        var hurt    = MakeState(sm, "Hurt",    Clip(clips, "Knight-Hurt"));
        var dead    = MakeState(sm, "Dead",    Clip(clips, "Knight-Death"));
        sm.defaultState = idle;

        // Idle ↔ Walk
        Trans(idle, walk, false, (AnimatorConditionMode.Greater, 0.01f, "Speed"));
        Trans(walk, idle, false, (AnimatorConditionMode.Less,    0.01f, "Speed"));

        // AnyState → Dead (최우선: 먼저 등록)
        AnyTrans(sm, dead,  false, (AnimatorConditionMode.If,  0f, "IsDead"));

        // AnyState → Hurt
        AnyTrans(sm, hurt,  false, (AnimatorConditionMode.If,  0f, "HurtTrigger"));
        var hurtExit = hurt.AddTransition(idle);
        hurtExit.hasExitTime = true; hurtExit.exitTime = 1f; hurtExit.duration = 0f;

        // AnyState → Attack1/2/3
        AnyTrans(sm, atk1, false, (AnimatorConditionMode.Equals, 1f, "AttackStep"));
        AnyTrans(sm, atk2, false, (AnimatorConditionMode.Equals, 2f, "AttackStep"));
        AnyTrans(sm, atk3, false, (AnimatorConditionMode.Equals, 3f, "AttackStep"));
        foreach (var a in new[] { atk1, atk2, atk3 })
            Trans(a, idle, true, (AnimatorConditionMode.Equals, 0f, "AttackStep"));

        // AnyState → Parry
        AnyTrans(sm, parry, false, (AnimatorConditionMode.If,    0f, "IsParrying"));
        // Parry → Riposte (RiposteReady 우선 확인, IsParrying→Idle보다 먼저 등록)
        Trans(parry, riposte, false, (AnimatorConditionMode.If,    0f, "RiposteReady"));
        Trans(parry, idle,    false, (AnimatorConditionMode.IfNot, 0f, "IsParrying"));
        // Riposte → Idle
        Trans(riposte, idle,  false, (AnimatorConditionMode.IfNot, 0f, "RiposteReady"));

        EditorUtility.SetDirty(ctrl);
    }

    static AnimatorState MakeState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        var s = sm.AddState(name);
        if (clip != null) s.motion = clip;
        return s;
    }

    static AnimationClip Clip(Dictionary<string, AnimationClip> d, string k) =>
        d.TryGetValue(k, out var c) ? c : null;

    static void Trans(AnimatorState from, AnimatorState to, bool exitTime,
        params (AnimatorConditionMode mode, float val, string param)[] conds)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = exitTime; t.duration = 0f;
        if (exitTime) t.exitTime = 1f;
        foreach (var (m, v, p) in conds) t.AddCondition(m, v, p);
    }

    static void AnyTrans(AnimatorStateMachine sm, AnimatorState to, bool canSelf,
        params (AnimatorConditionMode mode, float val, string param)[] conds)
    {
        var t = sm.AddAnyStateTransition(to);
        t.canTransitionToSelf = canSelf; t.hasExitTime = false; t.duration = 0f;
        foreach (var (m, v, p) in conds) t.AddCondition(m, v, p);
    }
}
