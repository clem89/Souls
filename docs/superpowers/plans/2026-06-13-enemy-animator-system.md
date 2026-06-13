# Enemy Animator System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 캐릭터 이름 하나로 AnimationClip + AnimatorController를 자동 생성하는 `EnemyAnimatorGenerator`(EditorWindow)와, `DummyEnemy` 이벤트를 구독해 Hurt/Death를 구동하는 `EnemyAnimator` 컴포넌트를 구현한다.

**Architecture:** `EnemyAnimatorGenerator`는 `KnightAnimatorGenerator`와 완전히 독립된 EditorWindow. 캐릭터 이름 입력 → `Assets/Resources/Characters(100x100)/{Name}/{Name}/{Name}-{State}.png` 탐색 → 존재하는 클립만 생성 → 공통 파라미터(Speed/AttackTrigger/HurtTrigger/IsDead) 컨트롤러 생성. `EnemyAnimator`는 `DummyEnemy.OnHpChanged` / `OnDeath` 이벤트 구독으로 Hurt/Dead 구동. Walk/Attack 파라미터는 준비만 해두고 AI 단계에서 연결.

**Tech Stack:** Unity 2D, C#, UnityEditor.Animations, AnimatorController API, EditorWindow, SpriteRenderer PPtrCurve

---

## 파일 목록

| 경로 | 유형 | 역할 |
|------|------|------|
| `Assets/Editor/EnemyAnimatorGenerator.cs` | 신규 | EditorWindow — 클립 + 컨트롤러 자동 생성 |
| `Assets/Scripts/Enemy/EnemyAnimator.cs` | 신규 | DummyEnemy 이벤트 구독 → Animator 파라미터 구동 |
| `Assets/Animations/Enemies/Skeleton/*.anim` | 생성물 | Skeleton 클립 7개 |
| `Assets/Animations/Enemies/Skeleton/SkeletonAnimator.controller` | 생성물 | Skeleton 컨트롤러 |

---

## Task 1: Skeleton 스프라이트 슬라이싱 확인

**Files:**
- Read: `Assets/Resources/Characters(100x100)/Skeleton/Skeleton/`

- [ ] **Step 1: 슬라이싱 결과 확인**

  Unity Project 창에서 `Assets/Resources/Characters(100x100)/Skeleton/Skeleton/Skeleton-Idle.png` 선택.
  Inspector에서 하위에 `Skeleton-Idle_0`, `Skeleton-Idle_1`, ... 서브스프라이트가 보이면 이미 완료 — Task 2로 이동.

  보이지 않으면 Unity 메뉴: `Tools → Sprite Slicer → Slice Character Sprites (100x100)` 실행.
  완료 다이얼로그 확인 후 다시 위 Inspector 확인.

---

## Task 2: EnemyAnimatorGenerator.cs 작성

**Files:**
- Create: `Assets/Editor/EnemyAnimatorGenerator.cs`

- [ ] **Step 1: 파일 생성**

```csharp
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

        if (C("Block") != null)
        {
            var block = sm.AddState("Block");
            block.motion = C("Block");
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
```

- [ ] **Step 2: 컴파일 확인**

  Unity Console에 에러 없음 확인.
  메뉴 `Tools → Animator Generator → Generate Enemy Animator` 항목이 생겼는지 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Editor/EnemyAnimatorGenerator.cs
git commit -m "feat: EnemyAnimatorGenerator EditorWindow 추가"
```

---

## Task 3: Generator 실행 — Skeleton

**Files:**
- Generated: `Assets/Animations/Enemies/Skeleton/*.anim`, `Assets/Animations/Enemies/Skeleton/SkeletonAnimator.controller`

- [ ] **Step 1: 제너레이터 실행**

  Unity 메뉴: `Tools → Animator Generator → Generate Enemy Animator`
  창이 열리면 Character Name에 `Skeleton` 입력 → `Generate` 버튼 클릭.

- [ ] **Step 2: 출력물 확인**

  Project 창 `Assets/Animations/Enemies/Skeleton/` 폴더에 아래 파일 존재 확인:
  - `Skeleton-Idle.anim`, `Skeleton-Walk.anim`
  - `Skeleton-Attack01.anim`, `Skeleton-Attack02.anim`
  - `Skeleton-Block.anim`, `Skeleton-Hurt.anim`, `Skeleton-Death.anim`
  - `SkeletonAnimator.controller`

  Console에 `[EnemyAnimatorGenerator] 'Skeleton' 생성 완료` 출력 확인.
  Console에 에러/경고 없음 확인.

- [ ] **Step 3: 클립 내용 확인**

  `Skeleton-Idle.anim` 더블클릭 → Animation 창에서 스프라이트 프레임 키프레임이 있는지 확인.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Animations/Enemies/
git commit -m "feat: Skeleton AnimationClip + AnimatorController 생성"
```

---

## Task 4: EnemyAnimator.cs 작성

**Files:**
- Create: `Assets/Scripts/Enemy/EnemyAnimator.cs`

**주의:** `IDamageable` 인터페이스에는 이벤트가 없다. `DummyEnemy`가 `OnHpChanged`/`OnDeath`를 직접 노출하므로 `DummyEnemy`를 사용한다. `PlayerAnimator`와 코드를 공유하거나 상속하지 않는다.

- [ ] **Step 1: 파일 생성**

```csharp
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    static readonly int SpeedHash   = Animator.StringToHash("Speed");
    static readonly int HurtHash    = Animator.StringToHash("HurtTrigger");
    static readonly int IsDeadHash  = Animator.StringToHash("IsDead");

    Animator   _animator;
    DummyEnemy _enemy;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _enemy    = GetComponent<DummyEnemy>();
    }

    void OnEnable()
    {
        if (_enemy == null) return;
        _enemy.OnHpChanged += OnHpChanged;
        _enemy.OnDeath     += OnDeath;
    }

    void OnDisable()
    {
        if (_enemy == null) return;
        _enemy.OnHpChanged -= OnHpChanged;
        _enemy.OnDeath     -= OnDeath;
    }

    void Update()
    {
        if (_animator == null) return;
        _animator.SetFloat(SpeedHash, 0f); // AI 연결 전 고정
    }

    void OnHpChanged(float ratio)
    {
        if (ratio > 0f && _animator != null)
            _animator.SetTrigger(HurtHash);
    }

    void OnDeath()
    {
        if (_animator != null)
            _animator.SetBool(IsDeadHash, true);
    }
}
```

- [ ] **Step 2: 컴파일 확인**

  Unity Console에 에러 없음 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Enemy/EnemyAnimator.cs
git commit -m "feat: EnemyAnimator 컴포넌트 추가 — Hurt/Death 이벤트 구동"
```

---

## Task 5: 씬 설정 — DummyEnemy에 Animator 연결

**Files:**
- Modify: `Assets/Scenes/GameScene.unity` (에디터 작업)

- [ ] **Step 1: DummyEnemy 계층 확인**

  Hierarchy에서 DummyEnemy GameObject 선택.
  Inspector에서 `SpriteRenderer`가 DummyEnemy 자신 또는 자식에 있는지 확인.
  `SpriteRenderer`가 있는 GameObject 이름을 메모.

- [ ] **Step 2: Animator 컴포넌트 추가**

  `SpriteRenderer`가 있는 GameObject 선택 →
  Inspector `Add Component → Animator` 추가.
  - `Controller` 필드: `Assets/Animations/Enemies/Skeleton/SkeletonAnimator.controller` 드래그 앤 드롭
  - `Apply Root Motion` 체크 해제

- [ ] **Step 3: EnemyAnimator 컴포넌트 추가**

  DummyEnemy (루트) GameObject 선택 →
  Inspector `Add Component → EnemyAnimator` 추가.

- [ ] **Step 4: Play Mode 검증**

  Play Mode 진입 후 확인:
  1. DummyEnemy가 `Skeleton-Idle` 애니메이션을 재생하는지 확인
  2. 플레이어가 DummyEnemy를 공격 → Console에 `[Enemy] HP: ...` 출력 + Hurt 애니메이션 잠깐 재생 후 Idle 복귀 확인
  3. HP가 0이 되면 Death 애니메이션 재생 후 정지 확인
  4. 기존 PlayerAnimator / KnightAnimator 동작 이상 없음 확인

- [ ] **Step 5: Scene 저장 및 커밋**

  Unity: `File → Save` (또는 Ctrl+S).

```bash
git add Assets/Scenes/GameScene.unity
git commit -m "feat: DummyEnemy에 SkeletonAnimator + EnemyAnimator 연결"
```

---

## 완료 체크리스트

- [ ] `Tools → Animator Generator → Generate Enemy Animator` 실행 → Skeleton 클립 7개 + 컨트롤러 생성
- [ ] Play Mode: 피격 시 Hurt 애니메이션 재생 후 Idle 복귀
- [ ] Play Mode: 사망 시 Death 애니메이션 재생 후 정지
- [ ] Knight Animator / PlayerAnimator 기존 동작 영향 없음
- [ ] `EnemyAnimatorGenerator`가 다른 캐릭터 이름(예: `Orc`)으로도 동작하는지 수동 확인 (선택)
