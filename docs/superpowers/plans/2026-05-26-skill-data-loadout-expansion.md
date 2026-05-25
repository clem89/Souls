# SkillData & PlayerLoadout 확장 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `SkillEffectData`를 `SkillData`로 완전 대체하고, PlayerLoadout에 스킬 슬롯 3개와 쿨타임·스태미나 비용을 추가한다.

**Architecture:** `SkillData` ScriptableObject 하나로 lightAttack과 skill[1~3]을 통일 관리. `SkillCooldownTracker`(순수 C# 클래스)가 쿨타임을 관리해 EditMode 단위 테스트를 가능하게 함. `AttackHitbox`는 Instant/Projectile 두 타입 실행을 지원하도록 확장.

**Tech Stack:** Unity 6 URP, New Input System, Unity Test Framework (EditMode)

---

## 파일 구조

| 파일 | 작업 |
|------|------|
| `Assets/Scripts/Combat/SkillData.cs` | 신규 — SkillType enum + SkillData SO |
| `Assets/Scripts/Combat/SkillCooldownTracker.cs` | 신규 — 순수 C# 쿨타임 추적 |
| `Assets/Tests/EditMode/SkillCooldownTrackerTests.cs` | 신규 — EditMode 단위 테스트 |
| `Assets/Scripts/Combat/SkillEffectData.cs` | 삭제 |
| `Assets/Scripts/Combat/PlayerLoadout.cs` | 수정 — skills[3] 추가 |
| `Assets/Scripts/Combat/SkillEffectPool.cs` | 수정 — SkillEffectData → SkillData |
| `Assets/Scripts/Combat/SkillEffectPoolManager.cs` | 수정 — skills 풀 등록 추가 |
| `Assets/Scripts/Combat/AttackHitbox.cs` | 수정 — Projectile 이동 + Fire 시그니처 변경 |
| `Assets/Scripts/Player/PlayerCombat.cs` | 수정 — UseSkill + SkillCooldownTracker 통합 |
| `Assets/Scripts/Input/InputReader.cs` | 수정 — Skill1/2/3 이벤트 추가 |
| `Assets/InputSystem_Actions.inputactions` | 수정 — Skill1/2/3 Action 추가 (Unity Editor) |
| `Assets/Data/Skills/LightSlash.asset` | 재생성 — SkillData 기반으로 교체 |

---

## Task 1: SkillData ScriptableObject 생성

**Files:**
- Create: `Assets/Scripts/Combat/SkillData.cs`

- [ ] **Step 1: SkillData.cs 작성**

`Assets/Scripts/Combat/SkillData.cs`:
```csharp
using UnityEngine;

public enum SkillType { Instant, Projectile, Area }

[CreateAssetMenu(menuName = "Souls/Combat/Skill Data", fileName = "SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;

    public GameObject effectPrefab;
    [Min(1)] public int poolSize = 4;

    public SkillType type = SkillType.Instant;

    public float damage = 20f;
    public float range = 1.2f;
    public float lifetime = 0.12f;

    public float projectileSpeed = 8f;

    public float cooldown = 0f;
    public float staminaCost = 0f;
}
```

- [ ] **Step 2: Unity 컴파일 확인**

Unity Editor 하단 상태바에 에러 없음 확인. Console 창에 컴파일 에러 없어야 함.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Combat/SkillData.cs
git commit -m "feat: SkillData ScriptableObject + SkillType enum 추가"
```

---

## Task 2: SkillCooldownTracker (TDD)

**Files:**
- Create: `Assets/Scripts/Combat/SkillCooldownTracker.cs`
- Test: `Assets/Tests/EditMode/SkillCooldownTrackerTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

`Assets/Tests/EditMode/SkillCooldownTrackerTests.cs`:
```csharp
using NUnit.Framework;

public class SkillCooldownTrackerTests
{
    [Test]
    public void InitialState_AllSlotsReady()
    {
        var tracker = new SkillCooldownTracker(3);
        for (int i = 0; i < 3; i++)
            Assert.IsTrue(tracker.IsReady(i));
    }

    [Test]
    public void StartCooldown_SlotNotReady()
    {
        var tracker = new SkillCooldownTracker(3);
        tracker.StartCooldown(1, 2f);
        Assert.IsFalse(tracker.IsReady(1));
    }

    [Test]
    public void StartCooldown_OtherSlotsUnaffected()
    {
        var tracker = new SkillCooldownTracker(3);
        tracker.StartCooldown(0, 5f);
        Assert.IsTrue(tracker.IsReady(1));
        Assert.IsTrue(tracker.IsReady(2));
    }

    [Test]
    public void Tick_ReducesRemainingUntilReady()
    {
        var tracker = new SkillCooldownTracker(3);
        tracker.StartCooldown(0, 2f);
        tracker.Tick(1f);
        Assert.IsFalse(tracker.IsReady(0));
        tracker.Tick(1f);
        Assert.IsTrue(tracker.IsReady(0));
    }

    [Test]
    public void Tick_DoesNotGoBelowZero()
    {
        var tracker = new SkillCooldownTracker(3);
        tracker.StartCooldown(2, 1f);
        tracker.Tick(10f);
        Assert.IsTrue(tracker.IsReady(2));
    }
}
```

- [ ] **Step 2: 테스트 실행 → FAIL 확인**

Unity Editor > Window > General > Test Runner > EditMode 탭.
`SkillCooldownTrackerTests` Run → 컴파일 에러 또는 전체 FAIL 확인.

- [ ] **Step 3: SkillCooldownTracker 구현**

`Assets/Scripts/Combat/SkillCooldownTracker.cs`:
```csharp
public class SkillCooldownTracker
{
    readonly float[] _remaining;

    public SkillCooldownTracker(int slotCount)
    {
        _remaining = new float[slotCount];
    }

    public bool IsReady(int slot) => _remaining[slot] <= 0f;

    public void StartCooldown(int slot, float duration)
    {
        _remaining[slot] = duration;
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < _remaining.Length; i++)
            if (_remaining[i] > 0f) _remaining[i] -= deltaTime;
    }
}
```

- [ ] **Step 4: 테스트 실행 → PASS 확인**

Test Runner에서 Run. 5개 테스트 모두 녹색 PASS 확인.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Combat/SkillCooldownTracker.cs Assets/Tests/EditMode/SkillCooldownTrackerTests.cs
git commit -m "feat: SkillCooldownTracker TDD — 5개 단위 테스트 PASS"
```

---

## Task 3: SkillEffectPool + SkillEffectPoolManager → SkillData 교체

**Files:**
- Modify: `Assets/Scripts/Combat/SkillEffectPool.cs`
- Modify: `Assets/Scripts/Combat/SkillEffectPoolManager.cs`

- [ ] **Step 1: SkillEffectPool.cs 수정**

`Assets/Scripts/Combat/SkillEffectPool.cs` 전체 교체:
```csharp
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectPool
{
    readonly Queue<AttackHitbox> _pool = new();
    readonly SkillData _data;
    readonly Transform _parent;

    public SkillEffectPool(SkillData data, Transform parent)
    {
        _data = data;
        _parent = parent;
        for (int i = 0; i < data.poolSize; i++)
            _pool.Enqueue(Spawn());
    }

    public AttackHitbox Get()
    {
        var hb = _pool.Count > 0 ? _pool.Dequeue() : Spawn();
        hb.OnExpired += Return;
        return hb;
    }

    void Return(AttackHitbox hb)
    {
        hb.OnExpired -= Return;
        _pool.Enqueue(hb);
    }

    AttackHitbox Spawn()
    {
        var go = Object.Instantiate(_data.effectPrefab, _parent);
        go.SetActive(false);
        var hb = go.GetComponent<AttackHitbox>();
        Debug.Assert(hb != null, $"[SkillEffectPool] '{_data.effectPrefab.name}'에 AttackHitbox 컴포넌트 없음");
        return hb;
    }
}
```

- [ ] **Step 2: SkillEffectPoolManager.cs 수정**

`Assets/Scripts/Combat/SkillEffectPoolManager.cs` 전체 교체:
```csharp
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectPoolManager : MonoBehaviour
{
    [SerializeField] PlayerLoadout _loadout;

    readonly Dictionary<SkillData, SkillEffectPool> _pools = new();

    public PlayerLoadout Loadout => _loadout;

    void Awake()
    {
        Debug.Assert(_loadout != null, "SkillEffectPoolManager: PlayerLoadout이 할당되지 않음");
        Register(_loadout.lightAttack);
        foreach (var skill in _loadout.skills)
            Register(skill);
    }

    public SkillEffectPool GetPool(SkillData data)
    {
        _pools.TryGetValue(data, out var pool);
        return pool;
    }

    void Register(SkillData data)
    {
        if (data == null || _pools.ContainsKey(data)) return;
        var container = new GameObject($"Pool_{data.name}");
        container.transform.SetParent(transform);
        _pools[data] = new SkillEffectPool(data, container.transform);
    }
}
```

- [ ] **Step 3: 컴파일 확인**

Unity Console에 에러 없음 확인. (SkillEffectData를 아직 삭제하지 않았으므로 컴파일 정상)

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Combat/SkillEffectPool.cs Assets/Scripts/Combat/SkillEffectPoolManager.cs
git commit -m "refactor: SkillEffectPool/Manager SkillEffectData → SkillData 타입 교체"
```

---

## Task 4: PlayerLoadout 확장 + SkillEffectData 삭제

**Files:**
- Modify: `Assets/Scripts/Combat/PlayerLoadout.cs`
- Delete: `Assets/Scripts/Combat/SkillEffectData.cs`

- [ ] **Step 1: PlayerLoadout.cs 수정**

`Assets/Scripts/Combat/PlayerLoadout.cs` 전체 교체:
```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Souls/Combat/Player Loadout", fileName = "PlayerLoadout")]
public class PlayerLoadout : ScriptableObject
{
    public SkillData lightAttack;
    public SkillData[] skills = new SkillData[3];
}
```

- [ ] **Step 2: SkillEffectData.cs 삭제**

```bash
rm "Assets/Scripts/Combat/SkillEffectData.cs"
rm "Assets/Scripts/Combat/SkillEffectData.cs.meta"
```

- [ ] **Step 3: Unity 컴파일 확인**

Unity Editor에서 컴파일 에러 없음 확인. (SkillEffectData 참조가 모두 제거됐으므로 에러 없어야 함)

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Combat/PlayerLoadout.cs
git rm Assets/Scripts/Combat/SkillEffectData.cs Assets/Scripts/Combat/SkillEffectData.cs.meta
git commit -m "refactor: PlayerLoadout skills[3] 추가, SkillEffectData 삭제"
```

---

## Task 5: AttackHitbox Projectile 이동 지원

**Files:**
- Modify: `Assets/Scripts/Combat/AttackHitbox.cs`

- [ ] **Step 1: AttackHitbox.cs 전체 교체**

`Assets/Scripts/Combat/AttackHitbox.cs`:
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class AttackHitbox : MonoBehaviour
{
    public event Action<AttackHitbox> OnExpired;

    float _damage;
    GameObject _source;
    LayerMask _enemyLayer;
    readonly HashSet<Collider2D> _hit = new();

    SkillType _type;
    Vector2 _velocity;

    void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Fire(Vector2 position, Vector2 direction, float damage, GameObject source,
        LayerMask enemyLayer, SkillType type, float projectileSpeed, float lifetime)
    {
        transform.position = position;
        _damage = damage;
        _source = source;
        _enemyLayer = enemyLayer;
        _type = type;
        _velocity = type == SkillType.Projectile
            ? direction.normalized * projectileSpeed
            : Vector2.zero;
        _hit.Clear();
        gameObject.SetActive(true);
        StartCoroutine(ExpireAfter(lifetime));
    }

    void FixedUpdate()
    {
        if (_type == SkillType.Projectile)
            transform.Translate(_velocity * Time.fixedDeltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((_enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (!_hit.Add(other)) return;
        if (other.TryGetComponent<IDamageable>(out var d))
            d.TakeDamage(_damage, _source);
    }

    IEnumerator ExpireAfter(float t)
    {
        yield return new WaitForSeconds(t);
        Expire();
    }

    public void Expire()
    {
        StopAllCoroutines();
        _velocity = Vector2.zero;
        gameObject.SetActive(false);
        _hit.Clear();
        OnExpired?.Invoke(this);
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity Console에 에러 없음 확인. PlayerCombat.cs가 아직 구 Fire() 시그니처를 호출하므로 여기서 에러가 나면 정상 — Task 6에서 수정함.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Combat/AttackHitbox.cs
git commit -m "feat: AttackHitbox Projectile 이동 지원 + Fire 시그니처 확장"
```

---

## Task 6: PlayerCombat UseSkill + SkillCooldownTracker 통합

**Files:**
- Modify: `Assets/Scripts/Player/PlayerCombat.cs`

- [ ] **Step 1: PlayerCombat.cs 전체 교체**

`Assets/Scripts/Player/PlayerCombat.cs`:
```csharp
using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _riposteDamage = 60f;
    [SerializeField] float _comboWindow = 0.6f;
    [SerializeField] LayerMask _enemyLayer;

    [SerializeField] float _parryActiveDuration = 0.35f;
    [SerializeField] float _parryCooldown = 0.8f;
    [SerializeField] float _parryDetectionRadius = 2f;
    bool _isParryCooldown;

    SkillEffectPoolManager _poolManager;
    StaminaController _stamina;
    SkillCooldownTracker _skillCooldown;

    public int ComboStep => _comboStep;
    public bool IsAttacking => _isAttacking;

    int _comboStep;
    float _comboTimer;
    bool _isAttacking;
    PlayerDodge _dodge;

    public bool IsParrying { get; private set; }
    public bool RiposteReady { get; private set; }
    public DummyEnemy RiposteTarget { get; private set; }
    float _riposteTimer;
    const float RiposteTimeLimit = 2f;

    static readonly float[] ComboMultipliers = { 1f, 1f, 2f };

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerCombat: InputReader not assigned");
        _poolManager = GetComponent<SkillEffectPoolManager>();
        Debug.Assert(_poolManager != null, "PlayerCombat: SkillEffectPoolManager not found on Player");
        _stamina = GetComponent<StaminaController>();
        _dodge = GetComponent<PlayerDodge>();
        _skillCooldown = new SkillCooldownTracker(3);

        _input.AttackStarted += OnAttackInput;
        _input.ParryPerformed += OnParryInput;
        _input.Skill1Performed += () => UseSkill(0);
        _input.Skill2Performed += () => UseSkill(1);
        _input.Skill3Performed += () => UseSkill(2);
    }

    void OnDisable() => _isParryCooldown = false;

    void OnDestroy()
    {
        if (_input == null) return;
        _input.AttackStarted -= OnAttackInput;
        _input.ParryPerformed -= OnParryInput;
        _input.Skill1Performed -= () => UseSkill(0);
        _input.Skill2Performed -= () => UseSkill(1);
        _input.Skill3Performed -= () => UseSkill(2);
    }

    void Update()
    {
        _skillCooldown.Tick(Time.deltaTime);

        if (_comboStep > 0 && !_isAttacking)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f) _comboStep = 0;
        }

        if (RiposteReady)
        {
            _riposteTimer -= Time.deltaTime;
            if (_riposteTimer <= 0f) CancelRiposte();
        }
    }

    void OnAttackInput()
    {
        if (IsParrying) return;
        if (_dodge != null && _dodge.IsDodging) return;

        if (RiposteReady && RiposteTarget != null)
        {
            StartCoroutine(PerformRiposte());
            return;
        }

        if (!_isAttacking)
            StartCoroutine(PerformAttack());
    }

    IEnumerator PerformAttack()
    {
        _isAttacking = true;
        float damage = _poolManager.Loadout.lightAttack.damage * ComboMultipliers[_comboStep % 3];
        _comboStep = (_comboStep % 3) + 1;
        _comboTimer = _comboWindow;

        yield return new WaitForSeconds(0.1f);
        SpawnHitbox(_poolManager.Loadout.lightAttack, damage);
        yield return new WaitForSeconds(0.4f);
        _isAttacking = false;
    }

    void UseSkill(int slot)
    {
        if (_isAttacking || IsParrying) return;
        if (_dodge != null && _dodge.IsDodging) return;

        var data = _poolManager.Loadout.skills[slot];
        if (data == null) return;
        if (!_skillCooldown.IsReady(slot)) return;
        if (_stamina != null && data.staminaCost > 0f && !_stamina.TryConsume(data.staminaCost)) return;

        _skillCooldown.StartCooldown(slot, data.cooldown);
        StartCoroutine(PerformSkill(data));
    }

    IEnumerator PerformSkill(SkillData data)
    {
        _isAttacking = true;
        yield return new WaitForSeconds(0.1f);
        SpawnHitbox(data, data.damage);
        yield return new WaitForSeconds(0.4f);
        _isAttacking = false;
    }

    void SpawnHitbox(SkillData data, float damage)
    {
        var origin = (Vector2)transform.position + (Vector2)transform.up * data.range;
        _poolManager.GetPool(data).Get().Fire(
            origin,
            transform.up,
            damage,
            gameObject,
            _enemyLayer,
            data.type,
            data.projectileSpeed,
            data.lifetime);
    }

    IEnumerator PerformRiposte()
    {
        RiposteReady = false;
        _isAttacking = true;

        yield return new WaitForSeconds(0.15f);

        if (RiposteTarget != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, RiposteTarget.transform.position, 1.2f);
            RiposteTarget.TakeDamage(_riposteDamage, gameObject);
            RiposteTarget.SetGroggy(false);
            RiposteTarget = null;
        }

        yield return new WaitForSeconds(0.5f);
        _isAttacking = false;
    }

    public void SetRiposteTarget(DummyEnemy enemy)
    {
        RiposteReady = true;
        RiposteTarget = enemy;
        _riposteTimer = RiposteTimeLimit;
    }

    public void SetParrying(bool value) => IsParrying = value;

    public void CancelRiposte()
    {
        RiposteReady = false;
        RiposteTarget?.SetGroggy(false);
        RiposteTarget = null;
    }

    void OnParryInput()
    {
        if (_isParryCooldown || IsParrying || _isAttacking) return;
        StartCoroutine(ParryCoroutine());
    }

    IEnumerator ParryCoroutine()
    {
        SetParrying(true);
        _isParryCooldown = true;
        float elapsed = 0f;
        bool success = false;

        while (elapsed < _parryActiveDuration && !success)
        {
            elapsed += Time.deltaTime;
            var hits = Physics2D.OverlapCircleAll(
                (Vector2)transform.position,
                _parryDetectionRadius,
                _enemyLayer);

            foreach (var h in hits)
            {
                if (h.TryGetComponent<ParryReceiver>(out var pr) && pr.IsParryable)
                {
                    if (h.TryGetComponent<DummyEnemy>(out var enemy))
                    {
                        success = true;
                        pr.CloseWindow();
                        enemy.SetGroggy(true);
                        SetRiposteTarget(enemy);
                        Debug.Log("[Parry] 성공! LMB로 Riposte 입력 (2초 이내)");
                        break;
                    }
                }
            }
            yield return null;
        }

        if (!success) Debug.Log("[Parry] 실패");

        SetParrying(false);
        yield return new WaitForSeconds(_parryCooldown);
        _isParryCooldown = false;
    }
}
```

> **주의:** `OnDestroy`의 람다 언서브스크립션은 실제로는 동작하지 않는다(람다 인스턴스가 달라서). 현재 구현에서 PlayerCombat은 씬 수명과 같으므로 메모리 누수 없음. 추후 동적 스폰 시 캐싱된 delegate로 교체 필요.

- [ ] **Step 2: 컴파일 확인**

Unity Console에 에러 없음 확인. `Skill1Performed` 이벤트가 InputReader에 아직 없어서 에러가 나면 정상 — Task 7에서 추가함.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Player/PlayerCombat.cs
git commit -m "feat: PlayerCombat UseSkill + SkillCooldownTracker 통합, Skill1/2/3 슬롯 실행"
```

---

## Task 7: InputReader Skill1/2/3 이벤트 + InputActions 바인딩

**Files:**
- Modify: `Assets/Scripts/Input/InputReader.cs`
- Modify: `Assets/InputSystem_Actions.inputactions` (Unity Editor)

- [ ] **Step 1: InputSystem_Actions.inputactions에 Skill 액션 추가 (Unity Editor)**

Unity Editor > Project 창 > `Assets/InputSystem_Actions.inputactions` 더블클릭.  
Input Actions 에디터 열림 → 좌측 Action Maps > `Player` 선택.

`+` 버튼 클릭 3회로 아래 3개 Action 추가:

| Action 이름 | Type | Binding | Path |
|-------------|------|---------|------|
| `Skill1` | Button | Add Binding | `Keyboard / 1` |
| `Skill2` | Button | Add Binding | `Keyboard / 2` |
| `Skill3` | Button | Add Binding | `Keyboard / 3` |

`Save Asset` 클릭 (`Ctrl+S`). Unity가 `InputSystem_Actions.cs` 자동 재생성.

- [ ] **Step 2: InputReader.cs 수정**

`Assets/Scripts/Input/InputReader.cs` 전체 교체:
```csharp
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Souls/InputReader")]
public class InputReader : ScriptableObject
{
    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> LookEvent;
    public event Action AttackStarted;
    public event Action DodgePerformed;
    public event Action ParryPerformed;
    public event Action LockOnPerformed;
    public event Action Skill1Performed;
    public event Action Skill2Performed;
    public event Action Skill3Performed;

    InputSystem_Actions _actions;

    void OnEnable()
    {
        _actions = new InputSystem_Actions();
        _actions.Player.Move.performed += ctx => MoveEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Move.canceled += _ => MoveEvent?.Invoke(Vector2.zero);
        _actions.Player.Look.performed += ctx => LookEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Look.canceled += _ => LookEvent?.Invoke(Vector2.zero);
        _actions.Player.Attack.started += _ => AttackStarted?.Invoke();
        _actions.Player.Jump.performed += _ => DodgePerformed?.Invoke();
        _actions.Player.Crouch.performed += _ => ParryPerformed?.Invoke();
        _actions.Player.Next.performed += _ => LockOnPerformed?.Invoke();
        _actions.Player.Skill1.performed += _ => Skill1Performed?.Invoke();
        _actions.Player.Skill2.performed += _ => Skill2Performed?.Invoke();
        _actions.Player.Skill3.performed += _ => Skill3Performed?.Invoke();
        _actions.Enable();
    }

    void OnDisable() => _actions?.Disable();
}
```

- [ ] **Step 3: 컴파일 확인**

Unity Console 에러 없음 확인. `_actions.Player.Skill1`이 존재하지 않으면 Step 1의 InputActions 저장이 안 된 것 — Save Asset 재확인.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Input/InputReader.cs Assets/InputSystem_Actions.inputactions Assets/InputSystem_Actions.cs
git commit -m "feat: InputReader Skill1/2/3 이벤트 추가, InputActions 키바인딩 1/2/3"
```

---

## Task 8: LightSlash.asset 재생성 + DefaultLoadout 재연결 (Unity Editor)

**Files:**
- Recreate: `Assets/Data/Skills/LightSlash.asset`
- Update: `Assets/Data/Loadouts/DefaultLoadout.asset`

- [ ] **Step 1: 기존 LightSlash.asset 삭제**

Unity Editor > Project 창 > `Assets/Data/Skills/LightSlash.asset` 우클릭 > Delete.

- [ ] **Step 2: 새 LightSlash.asset 생성**

`Assets/Data/Skills/` 폴더 선택 > 우클릭 > Create > Souls > Combat > Skill Data.  
이름: `LightSlash`.

Inspector에서 값 설정:
```
Skill Name   : LightSlash
Effect Prefab: TestEffect (Assets/Prefabs/Combat/Effects/TestEffect.prefab)
Pool Size    : 4
Type         : Instant
Damage       : 20
Range        : 1.2
Lifetime     : 0.12
Projectile Speed: 8  (Instant이라 미사용이지만 기본값 유지)
Cooldown     : 0
Stamina Cost : 0
```

- [ ] **Step 3: DefaultLoadout.asset 재연결**

`Assets/Data/Loadouts/DefaultLoadout.asset` 선택.  
Inspector:
- `Light Attack` 필드 → 새로 만든 `LightSlash.asset` 드래그
- `Skills` 배열 크기 3 확인 (비어 있어도 OK — 슬롯만 준비)

- [ ] **Step 4: Play Mode에서 동작 확인**

Play Mode 진입. LMB 클릭 → 콘솔 `[Enemy] HP: 180/200 (데미지: 20)` 확인.  
1/2/3 키 입력 → 슬롯 비어있으면 아무 일 없음 (정상).

- [ ] **Step 5: 커밋**

```bash
git add Assets/Data/Skills/ Assets/Data/Loadouts/
git commit -m "feat: LightSlash.asset SkillData 기반으로 재생성, DefaultLoadout 재연결"
```

---

## 셀프 리뷰

**스펙 커버리지 확인:**
- SkillData 필드 (skillName, effectPrefab, poolSize, type, damage, range, lifetime, projectileSpeed, cooldown, staminaCost) → Task 1 ✅
- SkillType enum (Instant, Projectile, Area) → Task 1 ✅, Area는 데이터 정의만 (실행 로직 추후) ✅
- PlayerLoadout skills[3] → Task 4 ✅
- SkillEffectData 삭제 → Task 4 ✅
- SkillEffectPool/Manager SkillData 교체 → Task 3 ✅
- AttackHitbox Projectile 이동 → Task 5 ✅
- PlayerCombat UseSkill + 쿨타임 → Task 6 ✅
- InputReader Skill1/2/3 → Task 7 ✅
- LightSlash.asset 재생성 → Task 8 ✅

**타입 일관성:**
- `SkillEffectPool(SkillData, Transform)` → Task 3에서 정의, Task 3 SkillEffectPoolManager에서 호출 ✅
- `Fire(Vector2, Vector2, float, GameObject, LayerMask, SkillType, float, float)` → Task 5에서 정의, Task 6 SpawnHitbox에서 호출 ✅
- `SkillCooldownTracker(int)` → Task 2에서 정의, Task 6 Awake에서 생성 ✅
- `_input.Skill1Performed` → Task 6에서 구독, Task 7에서 이벤트 추가 ✅ (Task 7이 나중이라 Step 2에서 에러 → 정상 흐름)
