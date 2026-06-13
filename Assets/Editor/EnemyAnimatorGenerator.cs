using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class EnemyAnimatorGenerator : EditorWindow
{
    const string SPRITE_BASE = "Assets/Resources/Characters(100x100)";
    const string OUTPUT_BASE = "Assets/Animations/Enemies";

    string _characterName = "Skeleton";

    [MenuItem("Tools/Animator Generator/Generate Enemy Animator")]
    static void ShowWindow() => GetWindow<EnemyAnimatorGenerator>("Enemy Animator Generator").Show();

    void OnGUI()
    {
        _characterName = EditorGUILayout.TextField("Character Name", _characterName);
        GUI.enabled = !string.IsNullOrWhiteSpace(_characterName);
        if (GUILayout.Button("Generate")) Generate(_characterName.Trim());
        GUI.enabled = true;
    }

    static void Generate(string name)
    {
        string spriteRoot = $"{SPRITE_BASE}/{name}/{name}";
        string outputDir  = $"{OUTPUT_BASE}/{name}";
        string ctrlPath   = $"{outputDir}/{name}Animator.controller";

        EnsureFolders(name);

        var defs = new (string State, float Fps, bool Loop)[]
        {
            ($"{name}-Idle",     8f,  true),
            ($"{name}-Walk",     8f,  true),
            ($"{name}-Attack01", 10f, false),
            ($"{name}-Attack02", 10f, false),
            ($"{name}-Block",    8f,  true),
            ($"{name}-Hurt",     10f, false),
            ($"{name}-Death",    10f, false),
        };

        var clips = new Dictionary<string, AnimationClip>();
        foreach (var (state, fps, loop) in defs)
        {
            string pngPath = $"{spriteRoot}/{state}.png";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath)
                              .OfType<Sprite>()
                              .OrderBy(s => SpriteIndex(s.name))
                              .ToArray();

            if (sprites.Length == 0) continue;

            var clip     = new AnimationClip { frameRate = fps };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var binding   = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                keyframes[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AssetDatabase.CreateAsset(clip, $"{outputDir}/{state}.anim");
            clips[state] = clip;
        }

        if (clips.Count == 0)
        {
            Debug.LogError($"[EnemyAnimatorGenerator] 스프라이트 없음: {spriteRoot} — SpriteSheetSlicer 먼저 실행하세요.");
            return;
        }

        BuildController(name, ctrlPath, clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EnemyAnimatorGenerator] '{name}' 생성 완료");
    }

    static void BuildController(string name, string ctrlPath, Dictionary<string, AnimationClip> clips)
    {
        if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        var sm   = ctrl.layers[0].stateMachine;

        ctrl.AddParameter("Speed",         AnimatorControllerParameterType.Float);
        ctrl.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("HurtTrigger",   AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("IsDead",        AnimatorControllerParameterType.Bool);

        AnimationClip C(string state) => clips.TryGetValue($"{name}-{state}", out var c) ? c : null;

        var idle = sm.AddState("Idle");
        idle.motion     = C("Idle");
        sm.defaultState = idle;

        if (C("Walk") != null)
        {
            var walk = sm.AddState("Walk");
            walk.motion = C("Walk");
            Trans(idle, walk, false, (AnimatorConditionMode.Greater, 0.01f, "Speed"));
            Trans(walk, idle, false, (AnimatorConditionMode.Less,    0.01f, "Speed"));
        }

        if (C("Death") != null)
        {
            var dead = sm.AddState("Dead");
            dead.motion = C("Death");
            AnyTrans(sm, dead, false, (AnimatorConditionMode.If, 0f, "IsDead"));
        }

        if (C("Hurt") != null)
        {
            var hurt = sm.AddState("Hurt");
            hurt.motion = C("Hurt");
            AnyTrans(sm, hurt, false, (AnimatorConditionMode.If, 0f, "HurtTrigger"));
            var hurtExit = hurt.AddTransition(idle);
            hurtExit.hasExitTime = true; hurtExit.exitTime = 1f; hurtExit.duration = 0f;
            hurtExit.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
        }

        if (C("Attack01") != null)
        {
            var atk1 = sm.AddState("Attack01");
            atk1.motion = C("Attack01");
            AnyTrans(sm, atk1, false, (AnimatorConditionMode.If, 0f, "AttackTrigger"));

            if (C("Attack02") != null)
            {
                var atk2 = sm.AddState("Attack02");
                atk2.motion = C("Attack02");
                Trans(atk1, atk2, true);
                Trans(atk2, idle, true);
            }
            else
            {
                Trans(atk1, idle, true);
            }
        }

        EditorUtility.SetDirty(ctrl);
    }

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

    static void EnsureFolders(string name)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(OUTPUT_BASE))
            AssetDatabase.CreateFolder("Assets/Animations", "Enemies");
        if (!AssetDatabase.IsValidFolder($"{OUTPUT_BASE}/{name}"))
            AssetDatabase.CreateFolder(OUTPUT_BASE, name);
    }

    static int SpriteIndex(string spriteName)
    {
        int u = spriteName.LastIndexOf('_');
        return u >= 0 && int.TryParse(spriteName.Substring(u + 1), out int idx) ? idx : 0;
    }
}
