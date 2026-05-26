# Knight Sprite Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Knight 스프라이트시트를 Unity Animator로 연결해 플레이어 상태(Idle/Walk/Attack1-3/Parry/Riposte/Hurt/Dead)를 실제 애니메이션으로 재생한다.

**Architecture:** Editor 스크립트(`KnightAnimatorGenerator`)가 슬라이싱된 Knight PNG에서 AnimationClip과 AnimatorController를 자동 생성한다. `PlayerAnimator.cs`는 색상 기반 프로토타입에서 Animator 파라미터 드라이버로 교체된다. IFrame 깜빡임만 코드에서 처리한다.

**Tech Stack:** Unity 2D, UnityEditor.Animations, AnimatorController API, SpriteRenderer PPtrCurve

---

## File Map

| 작업 | 경로 |
|------|------|
| Create | `Assets/Editor/KnightAnimatorGenerator.cs` |
| Modify | `Assets/Scripts/Player/PlayerAnimator.cs` |
| Generated | `Assets/Animations/Knight/Knight-{Idle,Walk,Attack01-03,Block,Hurt,Death}.anim` |
| Generated | `Assets/Animations/KnightAnimator.controller` |

---

## Task 1: Knight 스프라이트 슬라이싱 확인

**Files:**
- Read: `Assets/Resources/Characters(100x100)/Knight/Knight/*.png`

- [ ] **Step 1: Unity 에디터에서 슬라이싱 실행**

  Unity 메뉴: `Tools → Sprite Slicer → Slice Character Sprites (100x100)`
  (스크립트가 없으면 이전 단계 `Assets/Editor/SpriteSheetSlicer.cs` 먼저 생성)

- [ ] **Step 2: 슬라이싱 결과 확인**

  Project 창에서 `Assets/Resources/Characters(100x100)/Knight/Knight/Knight-Idle.png` 선택.
  Inspector에서 하위에 `Knight-Idle_0`, `Knight-Idle_1`, ... 서브스프라이트가 보이면 완료.

- [ ] **Step 3: 메인 Knight.png 확인 (overview 시트)**

  `Knight.png`는 Single 모드로 설정되어 있어야 한다. 서브스프라이트가 없는 것이 정상.

---

## Task 2: KnightAnimatorGenerator.cs 작성

**Files:**
- Create: `Assets/Editor/KnightAnimatorGenerator.cs`

- [ ] **Step 1: 파일 생성**

```csharp
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

        var idle    = MakeState(sm, "Idle",    Clip(clips, "Knight-Idle"));
        var walk    = MakeState(sm, "Walk",    Clip(clips, "Knight-Walk"));
        var atk1    = MakeState(sm, "Attack1", Clip(clips, "Knight-Attack01"));
        var atk2    = MakeState(sm, "Attack2", Clip(clips, "Knight-Attack02"));
        var atk3    = MakeState(sm, "Attack3", Clip(clips, "Knight-Attack03"));
        var parry   = MakeState(sm, "Parry",   Clip(clips, "Knight-Block"));
        var riposte = MakeState(sm, "Riposte", Clip(clips, "Knight-Block"));
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
            Trans(a, idle, false, (AnimatorConditionMode.Equals, 0f, "AttackStep"));

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
```

- [ ] **Step 2: 컴파일 확인**

  Unity Console에 에러 없음 확인. 경고는 무시 가능.

---

## Task 3: 제너레이터 실행

**Files:**
- Generated: `Assets/Animations/Knight/*.anim`, `Assets/Animations/KnightAnimator.controller`

- [ ] **Step 1: 메뉴 실행**

  Unity 메뉴: `Tools → Animator Generator → Generate Knight Animator`

- [ ] **Step 2: 출력물 확인**

  Project 창 `Assets/Animations/Knight/` 폴더에:
  - `Knight-Idle.anim`, `Knight-Walk.anim`, `Knight-Attack01.anim` ~ `Knight-Attack03.anim`
  - `Knight-Block.anim`, `Knight-Hurt.anim`, `Knight-Death.anim`
  - `Assets/Animations/KnightAnimator.controller`

  Console에 `[KnightAnimatorGenerator] 생성 완료` 출력 확인.

- [ ] **Step 3: 클립 내용 확인**

  `Knight-Idle.anim` 더블클릭 → Animation 창에서 스프라이트 프레임 키프레임이 있는지 확인.

---

## Task 4: PlayerAnimator.cs 교체

**Files:**
- Modify: `Assets/Scripts/Player/PlayerAnimator.cs`

- [ ] **Step 1: 파일 전체 교체**

```csharp
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    static readonly int SpeedHash        = Animator.StringToHash("Speed");
    static readonly int AttackStepHash   = Animator.StringToHash("AttackStep");
    static readonly int IsParryingHash   = Animator.StringToHash("IsParrying");
    static readonly int RiposteReadyHash = Animator.StringToHash("RiposteReady");
    static readonly int HurtTriggerHash  = Animator.StringToHash("HurtTrigger");
    static readonly int IsDeadHash       = Animator.StringToHash("IsDead");

    SpriteRenderer _sr;
    Animator _animator;
    PlayerController _controller;
    PlayerCombat _combat;
    PlayerDodge _dodge;
    PlayerHealth _health;

    void Awake()
    {
        _sr         = GetComponentInChildren<SpriteRenderer>();
        _animator   = GetComponentInChildren<Animator>();
        _controller = GetComponent<PlayerController>();
        _combat     = GetComponent<PlayerCombat>();
        _dodge      = GetComponent<PlayerDodge>();
        _health     = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (_health != null) _health.OnHpChanged += OnHpChanged;
    }

    void OnDisable()
    {
        if (_health != null) _health.OnHpChanged -= OnHpChanged;
    }

    void Update()
    {
        if (_animator == null) return;
        UpdateAnimatorParams();
        UpdateIFrameAlpha();
    }

    void UpdateAnimatorParams()
    {
        _animator.SetBool(IsDeadHash, _health != null && _health.CurrentHp <= 0f);

        int step = (_combat != null && _combat.IsAttacking) ? _combat.ComboStep : 0;
        _animator.SetInteger(AttackStepHash, step);

        _animator.SetBool(IsParryingHash,   _combat != null && _combat.IsParrying);
        _animator.SetBool(RiposteReadyHash, _combat != null && _combat.RiposteReady);

        float speed = _controller != null ? _controller.MoveDirection.sqrMagnitude : 0f;
        _animator.SetFloat(SpeedHash, speed);
    }

    void UpdateIFrameAlpha()
    {
        if (_sr == null || _dodge == null) return;
        if (_dodge.IsInvincible)
            _sr.color = new Color(1f, 1f, 1f, Mathf.PingPong(Time.time * 8f, 1f));
        else
            _sr.color = Color.white;
    }

    void OnHpChanged(float ratio)
    {
        // ratio == 0은 사망 — IsDead bool이 담당하므로 Hurt는 생존 피격만
        if (ratio > 0f && _animator != null)
            _animator.SetTrigger(HurtTriggerHash);
    }
}
```

- [ ] **Step 2: 컴파일 확인**

  Console에 에러 없음 확인.

---

## Task 5: 씬에서 Animator 컴포넌트 연결 및 테스트

- [ ] **Step 1: Player 계층 구조 확인**

  Hierarchy에서 Player GameObject 선택 → Inspector에서 `PlayerAnimator` 컴포넌트 있는지 확인.
  `SpriteRenderer`가 Player 하위 자식 오브젝트에 있는지 확인 (자식 이름 메모).

- [ ] **Step 2: Animator 컴포넌트 추가**

  `SpriteRenderer`가 있는 자식 GameObject 선택 →
  Inspector `Add Component → Animator` 추가.

- [ ] **Step 3: KnightAnimator 컨트롤러 할당**

  추가된 Animator 컴포넌트의 `Controller` 필드에
  `Assets/Animations/KnightAnimator.controller` 드래그 앤 드롭.
  `Apply Root Motion` 체크 해제.

- [ ] **Step 4: Play Mode 테스트**

  Play Mode 진입 후 확인:
  - 정지 시 Idle 애니메이션 재생
  - WASD 이동 시 Walk 애니메이션 재생
  - LMB 공격 시 Attack1 → Attack2 → Attack3 순서로 재생
  - Space(패리) 입력 시 Block 애니메이션 재생
  - 피해 받으면 Hurt 애니메이션 잠깐 재생 후 복귀
  - 회피 중 alpha 깜빡임 확인 (IFrame)
  - 사망 시 Death 애니메이션 재생 후 정지

- [ ] **Step 5: Animator 파라미터 실시간 확인 (선택)**

  Play Mode 중 Hierarchy에서 Player 선택 →
  Window → Animation → Animator 창 열기 →
  파라미터 값이 상태에 따라 변하는지 실시간 모니터링.

---

## Task 6: 커밋

- [ ] **Step 1: 변경 파일 확인**

  ```bash
  git status
  ```

  예상 변경:
  - `Assets/Editor/KnightAnimatorGenerator.cs` (new)
  - `Assets/Scripts/Player/PlayerAnimator.cs` (modified)
  - `Assets/Animations/` (generated files)

- [ ] **Step 2: 커밋**

  ```bash
  git add Assets/Editor/KnightAnimatorGenerator.cs
  git add Assets/Scripts/Player/PlayerAnimator.cs
  git add Assets/Animations/
  git commit -m "feat: Knight 스프라이트 애니메이션 시스템 구현

  - KnightAnimatorGenerator: AnimationClip + AnimatorController 자동 생성
  - PlayerAnimator: 색상 프로토타입 → Animator 파라미터 드라이버로 교체
  - IFrame 중 alpha 깜빡임 코드 처리 유지
  - Hurt 이벤트 PlayerHealth.OnHpChanged 구독으로 처리

  Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
  ```

---

## 트러블슈팅

| 증상 | 원인 | 해결 |
|---|---|---|
| Console에 "스프라이트 없음" 경고 | 슬라이싱 미실행 | Task 1 재실행 |
| 애니메이션이 재생되지 않음 | Animator 컴포넌트 미연결 | Task 5 Step 2-3 확인 |
| 공격 애니메이션이 안 바뀜 | AttackStep이 0으로 남아 있음 | PlayerCombat.IsAttacking + ComboStep 로그 확인 |
| Hurt가 반복 재생됨 | OnHpChanged 중복 구독 | OnEnable/OnDisable 쌍 확인 |
| Riposte 후 Idle 복귀 안 됨 | RiposteReady가 false로 안 바뀜 | PlayerCombat.CancelRiposte() 호출 경로 확인 |
