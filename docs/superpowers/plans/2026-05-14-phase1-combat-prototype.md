# Phase 1: 전투 프로토타입 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 단일 씬에서 자동 콤보 공격·회피(무적 프레임)·패링(치명타 Riposte)·락온으로 더미 적을 상대하는 전투 프로토타입 완성

**Architecture:** CharacterController 기반 이동, 수동 3인칭 카메라(Cinemachine 없음), New Input System을 ScriptableObject InputReader로 이벤트 래핑. 스태미나는 순수 C# 클래스로 분리해 Edit Mode 단위 테스트. 패링 판정은 적의 ParryReceiver 컴포넌트가 윈도우를 열고 닫는 방식으로 관리.

**Tech Stack:** Unity 6 URP 17.3, New Input System 1.18.0, Unity Test Framework 1.6.0, CharacterController, Physics.OverlapSphere

---

## 파일 구조

| 경로 | 역할 |
|------|------|
| `Assets/Scripts/Input/InputReader.cs` | ScriptableObject — InputSystem 이벤트 래퍼 |
| `Assets/Scripts/Combat/StaminaSystem.cs` | 순수 C# 스태미나 로직 (no MonoBehaviour) |
| `Assets/Scripts/Combat/StaminaController.cs` | StaminaSystem 래퍼 MonoBehaviour |
| `Assets/Scripts/Combat/IDamageable.cs` | 피해 수신 인터페이스 |
| `Assets/Scripts/Player/PlayerController.cs` | 이동 + 중력 (CharacterController) |
| `Assets/Scripts/Player/PlayerCamera.cs` | 3인칭 카메라 + 락온 모드 전환 |
| `Assets/Scripts/Player/PlayerHealth.cs` | 플레이어 HP + IDamageable |
| `Assets/Scripts/Player/PlayerCombat.cs` | 콤보 공격 + 패링 + Riposte |
| `Assets/Scripts/Player/PlayerDodge.cs` | 회피 + 무적 프레임 |
| `Assets/Scripts/Player/LockOnSystem.cs` | 타겟 탐색 + 카메라 락온 전환 |
| `Assets/Scripts/Enemy/DummyEnemy.cs` | HP + 그로기 상태 + IDamageable |
| `Assets/Scripts/Enemy/ParryReceiver.cs` | 적 패링 가능 윈도우 관리 |
| `Assets/Scripts/Enemy/DummyEnemyAttack.cs` | 공격 패턴 코루틴 |
| `Assets/Scripts/UI/CombatHUD.cs` | HP바 + 스태미나바 (UI Slider) |
| `Assets/Tests/EditMode/EditModeTests.asmdef` | Edit Mode 테스트 어셈블리 정의 |
| `Assets/Tests/EditMode/StaminaSystemTests.cs` | StaminaSystem 단위 테스트 7개 |

---

## Task 1: 테스트 환경 설정 + StaminaSystem (TDD)

**Files:**
- Create: `Assets/Tests/EditMode/EditModeTests.asmdef`
- Create: `Assets/Scripts/Combat/StaminaSystem.cs`
- Create: `Assets/Scripts/Combat/StaminaController.cs`
- Test: `Assets/Tests/EditMode/StaminaSystemTests.cs`

- [ ] **Step 1: 폴더 구조 생성**

Unity Project 창에서 우클릭 > Create > Folder로 아래 폴더를 생성:
```
Assets/Scripts/Combat/
Assets/Scripts/Enemy/
Assets/Scripts/Input/
Assets/Scripts/Player/
Assets/Scripts/UI/
Assets/Tests/EditMode/
```

- [ ] **Step 2: Edit Mode 테스트 어셈블리 파일 생성**

`Assets/Tests/EditMode/EditModeTests.asmdef` 생성 (파일 내용 그대로 입력):
```json
{
    "name": "EditModeTests",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: 실패 테스트 작성**

`Assets/Tests/EditMode/StaminaSystemTests.cs`:
```csharp
using NUnit.Framework;

public class StaminaSystemTests
{
    [Test]
    public void Initial_CurrentEqualsMax()
    {
        var s = new StaminaSystem(100f);
        Assert.AreEqual(100f, s.Current, 0.001f);
    }

    [Test]
    public void TryConsume_ReducesStamina()
    {
        var s = new StaminaSystem(100f);
        bool ok = s.TryConsume(30f);
        Assert.IsTrue(ok);
        Assert.AreEqual(70f, s.Current, 0.001f);
    }

    [Test]
    public void TryConsume_ReturnsFalseWhenInsufficient()
    {
        var s = new StaminaSystem(100f);
        s.TryConsume(80f);
        bool ok = s.TryConsume(30f);
        Assert.IsFalse(ok);
        Assert.AreEqual(20f, s.Current, 0.001f);
    }

    [Test]
    public void TryConsume_NeverGoesBelowZero()
    {
        var s = new StaminaSystem(10f);
        s.TryConsume(9f);
        s.TryConsume(5f);
        Assert.AreEqual(1f, s.Current, 0.001f);
    }

    [Test]
    public void Recover_IncreasesStamina()
    {
        var s = new StaminaSystem(100f);
        s.TryConsume(50f);
        s.Recover(20f);
        Assert.AreEqual(70f, s.Current, 0.001f);
    }

    [Test]
    public void Recover_ClampsAtMax()
    {
        var s = new StaminaSystem(100f);
        s.TryConsume(10f);
        s.Recover(50f);
        Assert.AreEqual(100f, s.Current, 0.001f);
    }

    [Test]
    public void OnChanged_FiredWithCurrentValue()
    {
        var s = new StaminaSystem(100f);
        float received = -1f;
        s.OnChanged += v => received = v;
        s.TryConsume(30f);
        Assert.AreEqual(70f, received, 0.001f);
    }
}
```

- [ ] **Step 4: 테스트 실행 → FAIL 확인**

Unity Editor > Window > General > Test Runner > Edit Mode 탭 열기.  
`StaminaSystemTests`를 찾아 Run 클릭.  
Expected: `StaminaSystem` 클래스 없음으로 컴파일 에러 또는 전체 FAIL.

- [ ] **Step 5: StaminaSystem 구현**

`Assets/Scripts/Combat/StaminaSystem.cs`:
```csharp
using System;
using UnityEngine;

public class StaminaSystem
{
    public float Max { get; }
    public float Current { get; private set; }
    public event Action<float> OnChanged;

    public StaminaSystem(float max)
    {
        Max = max;
        Current = max;
    }

    public bool TryConsume(float amount)
    {
        if (Current < amount) return false;
        Current = Mathf.Max(0f, Current - amount);
        OnChanged?.Invoke(Current);
        return true;
    }

    public void Recover(float amount)
    {
        Current = Mathf.Min(Max, Current + amount);
        OnChanged?.Invoke(Current);
    }
}
```

- [ ] **Step 6: 테스트 실행 → PASS 확인**

Test Runner에서 Run 클릭.  
Expected: 7개 테스트 모두 녹색 PASS.

- [ ] **Step 7: StaminaController 작성**

`Assets/Scripts/Combat/StaminaController.cs`:
```csharp
using UnityEngine;

public class StaminaController : MonoBehaviour
{
    [SerializeField] float _maxStamina = 100f;
    [SerializeField] float _recoveryRate = 20f;
    [SerializeField] float _recoveryDelay = 1f;

    float _timeSinceLastConsume;

    public StaminaSystem Stamina { get; private set; }

    void Awake() => Stamina = new StaminaSystem(_maxStamina);

    void Update()
    {
        _timeSinceLastConsume += Time.deltaTime;
        if (_timeSinceLastConsume >= _recoveryDelay)
            Stamina.Recover(_recoveryRate * Time.deltaTime);
    }

    public bool TryConsume(float amount)
    {
        bool success = Stamina.TryConsume(amount);
        if (success) _timeSinceLastConsume = 0f;
        return success;
    }
}
```

- [ ] **Step 8: 커밋**

```bash
git add Assets/Scripts/Combat/ Assets/Tests/
git commit -m "feat: StaminaSystem TDD 단위 테스트 7개 PASS + StaminaController"
```

---

## Task 2: IDamageable + InputReader

**Files:**
- Create: `Assets/Scripts/Combat/IDamageable.cs`
- Create: `Assets/Scripts/Input/InputReader.cs`

- [ ] **Step 1: IDamageable 인터페이스 작성**

`Assets/Scripts/Combat/IDamageable.cs`:
```csharp
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, GameObject source);
    bool IsInvincible { get; }
}
```

- [ ] **Step 2: InputReader 작성**

기존 `Assets/InputSystem_Actions.inputactions`에서 확인된 액션:
- `Player/Move` (Vector2) → 이동
- `Player/Look` (Vector2) → 카메라
- `Player/Attack` (Button) → LMB 공격
- `Player/Jump` (Button) → Space 회피
- `Player/Crouch` (Button) → Task 3에서 RMB로 리바인딩 → 패링
- `Player/Next` (Button) → Task 3에서 Middle Mouse로 리바인딩 → 락온

`Assets/Scripts/Input/InputReader.cs`:
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

    InputSystem_Actions _actions;

    void OnEnable()
    {
        if (_actions == null) _actions = new InputSystem_Actions();

        _actions.Player.Move.performed += ctx => MoveEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Move.canceled += _ => MoveEvent?.Invoke(Vector2.zero);
        _actions.Player.Look.performed += ctx => LookEvent?.Invoke(ctx.ReadValue<Vector2>());
        _actions.Player.Look.canceled += _ => LookEvent?.Invoke(Vector2.zero);
        _actions.Player.Attack.started += _ => AttackStarted?.Invoke();
        _actions.Player.Jump.performed += _ => DodgePerformed?.Invoke();
        _actions.Player.Crouch.performed += _ => ParryPerformed?.Invoke();
        _actions.Player.Next.performed += _ => LockOnPerformed?.Invoke();

        _actions.Enable();
    }

    void OnDisable() => _actions?.Disable();
}
```

- [ ] **Step 3: InputReader 에셋 생성**

Unity Editor > Project 창 > `Assets/Settings/` 폴더 선택.  
우클릭 > Create > Souls > InputReader.  
이름: `SoulsInputReader`.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Combat/IDamageable.cs Assets/Scripts/Input/InputReader.cs
git commit -m "feat: IDamageable 인터페이스 + InputReader ScriptableObject"
```

---

## Task 3: InputSystem_Actions 바인딩 수정 (Unity Editor 작업)

이 Task는 코드가 아닌 Unity Editor 조작입니다.

- [ ] **Step 1: Input Actions 에디터 열기**

Project 창에서 `Assets/InputSystem_Actions.inputactions` 더블클릭.  
Input Actions 에디터 창이 열립니다.

- [ ] **Step 2: Crouch 바인딩을 RMB로 교체**

좌측 Action Maps > `Player` 선택.  
Action 목록 > `Crouch` 선택.  
우측 Bindings에서 기존 `C [Keyboard]` 바인딩 선택 후 우클릭 > Delete.  
`+` 버튼 > Add Binding 클릭.  
Path 검색창에 `Right Button` 입력 → `Mouse / Right Button` 선택.

- [ ] **Step 3: Next 바인딩을 Middle Mouse로 교체**

Action 목록 > `Next` 선택.  
기존 바인딩 삭제.  
`+` > Add Binding > Path: `Mouse / Middle Button` 선택.

- [ ] **Step 4: 저장**

`Save Asset` 버튼 클릭 (또는 `Ctrl+S`).  
Unity가 `InputSystem_Actions.cs`를 자동 재생성합니다.

- [ ] **Step 5: 커밋**

```bash
git add Assets/InputSystem_Actions.inputactions
git commit -m "config: Parry를 RMB, LockOn을 Middle Mouse Button에 바인딩"
```

---

## Task 4: PlayerController (이동)

**Files:**
- Create: `Assets/Scripts/Player/PlayerController.cs`

- [ ] **Step 1: PlayerController 작성**

`Assets/Scripts/Player/PlayerController.cs`:
```csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _rotationSpeed = 10f;
    [SerializeField] float _gravity = -20f;

    CharacterController _cc;
    Vector2 _moveInput;
    float _verticalVelocity;
    Transform _cameraTransform;

    public Vector3 MoveDirection { get; private set; }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cameraTransform = Camera.main.transform;
        _input.MoveEvent += v => _moveInput = v;
    }

    void Update()
    {
        var camForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
        var camRight = _cameraTransform.right;
        MoveDirection = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        if (_cc.isGrounded)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += _gravity * Time.deltaTime;

        _cc.Move((MoveDirection * _moveSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);

        if (MoveDirection.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(MoveDirection),
                _rotationSpeed * Time.deltaTime);
    }
}
```

- [ ] **Step 2: 씬에 플레이어 오브젝트 생성**

Hierarchy 우클릭 > 3D Object > Capsule. 이름: `Player`.  
Inspector 상단 Tag 드롭다운 > `Player` 선택 (없으면 Add Tag로 추가).  
컴포넌트 추가:
- `CharacterController` 추가 (RequireComponent로 자동 추가될 수 있음)
- `PlayerController` 추가
- `PlayerController.Input` 필드에 `SoulsInputReader` 에셋 드래그

플로어 생성: 3D Object > Plane. Position `(0, -1, 0)`.

- [ ] **Step 3: Play Mode에서 이동 확인**

Play Mode 진입 (`Ctrl+P`). WASD 입력 시 Capsule이 이동하는지 확인.  
Expected: 카메라 방향 기준 상대 이동 (처음엔 카메라 미설정으로 World 기준 이동 — Task 5 이후 정상화됨).

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Player/PlayerController.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: PlayerController — CharacterController 기반 카메라 상대 이동"
```

---

## Task 5: PlayerCamera (3인칭 카메라)

**Files:**
- Create: `Assets/Scripts/Player/PlayerCamera.cs`

- [ ] **Step 1: PlayerCamera 작성**

`Assets/Scripts/Player/PlayerCamera.cs`:
```csharp
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] Transform _target;
    [SerializeField] float _distance = 4f;
    [SerializeField] float _height = 1.5f;
    [SerializeField] float _sensitivity = 2f;
    [SerializeField] float _pitchMin = -30f;
    [SerializeField] float _pitchMax = 60f;

    float _yaw;
    float _pitch = 15f;

    public Transform LockOnTarget { get; set; }

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _input.LookEvent += delta =>
        {
            _yaw += delta.x * _sensitivity;
            _pitch -= delta.y * _sensitivity;
            _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);
        };
    }

    void LateUpdate()
    {
        if (LockOnTarget != null)
        {
            var toTarget = Vector3.ProjectOnPlane(
                LockOnTarget.position - _target.position, Vector3.up);
            if (toTarget.sqrMagnitude > 0.01f)
                _yaw = Quaternion.LookRotation(toTarget).eulerAngles.y;
        }

        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _target.position + rotation * new Vector3(0f, _height, -_distance);
        transform.LookAt(_target.position + Vector3.up * (_height * 0.5f));
    }
}
```

- [ ] **Step 2: Main Camera에 PlayerCamera 설정**

Hierarchy에서 `Main Camera` 선택.  
컴포넌트 추가: `PlayerCamera`.  
필드 설정:
- `Input` → `SoulsInputReader`
- `Target` → `Player` 오브젝트

- [ ] **Step 3: Play Mode에서 카메라 확인**

Play Mode 진입. 마우스 이동으로 카메라가 Player 주위를 궤도 회전하는지 확인.  
WASD 이동이 카메라 방향 기준으로 동작하는지 확인.  
Expected: 마우스 우측 이동 → 카메라가 Player 왼쪽으로 이동. WASD가 자연스럽게 카메라 기준으로 이동.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Player/PlayerCamera.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: PlayerCamera — 3인칭 마우스 궤도 카메라, 락온 모드 지원"
```

---

## Task 6: PlayerHealth + PlayerCombat (콤보 공격)

**Files:**
- Create: `Assets/Scripts/Player/PlayerHealth.cs`
- Create: `Assets/Scripts/Player/PlayerCombat.cs`

- [ ] **Step 1: PlayerHealth 작성**

`Assets/Scripts/Player/PlayerHealth.cs`:
```csharp
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] float _maxHp = 100f;

    PlayerDodge _dodge;

    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsInvincible => _dodge != null && _dodge.IsInvincible;

    public event Action<float> OnHpChanged;

    void Awake()
    {
        CurrentHp = _maxHp;
        _dodge = GetComponent<PlayerDodge>();
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsInvincible) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        OnHpChanged?.Invoke(CurrentHp / _maxHp);
        Debug.Log($"[Player] HP: {CurrentHp:F0}/{_maxHp}");
        if (CurrentHp <= 0f) Debug.Log("[Player] 사망");
    }
}
```

- [ ] **Step 2: PlayerCombat 작성 (이번 Task는 콤보 공격만, 패링은 Task 9에서 추가)**

`Assets/Scripts/Player/PlayerCombat.cs`:
```csharp
using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _attackRange = 1.2f;
    [SerializeField] float _attackRadius = 0.8f;
    [SerializeField] float _attackDamage = 20f;
    [SerializeField] float _riposteDamage = 60f;
    [SerializeField] float _comboWindow = 0.6f;
    [SerializeField] LayerMask _enemyLayer;

    int _comboStep;
    float _comboTimer;
    bool _isAttacking;

    // 패링/리포스트 상태 — Task 9에서 채워짐
    public bool IsParrying { get; private set; }
    public bool RiposteReady { get; private set; }
    public DummyEnemy RiposteTarget { get; private set; }
    float _riposteTimer;
    const float RiposteTimeLimit = 2f;

    static readonly float[] ComboMultipliers = { 1f, 1f, 2f };

    void Awake()
    {
        _input.AttackStarted += OnAttackInput;
    }

    void Update()
    {
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
        if (TryGetComponent<PlayerDodge>(out var dodge) && dodge.IsDodging) return;

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
        float damage = _attackDamage * ComboMultipliers[_comboStep % 3];
        _comboStep = (_comboStep % 3) + 1;
        _comboTimer = _comboWindow;

        yield return new WaitForSeconds(0.1f);
        DealDamage(damage);
        yield return new WaitForSeconds(0.4f);
        _isAttacking = false;
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

    void DealDamage(float damage)
    {
        var origin = transform.position + transform.forward * _attackRange + Vector3.up * 0.8f;
        var hits = Physics.OverlapSphere(origin, _attackRadius, _enemyLayer);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(damage, gameObject);
        }
    }

    public void SetRiposteTarget(DummyEnemy enemy)
    {
        RiposteReady = true;
        RiposteTarget = enemy;
        _riposteTimer = RiposteTimeLimit;
    }

    public void SetParrying(bool value) => IsParrying = value;

    void CancelRiposte()
    {
        RiposteReady = false;
        RiposteTarget?.SetGroggy(false);
        RiposteTarget = null;
    }
}
```

- [ ] **Step 3: Enemy 레이어 추가**

Unity Editor > Edit > Project Settings > Tags and Layers.  
Layers의 빈 슬롯(User Layer 8 등)에 `Enemy` 입력.  
Player 레이어도 필요: 빈 슬롯에 `Player` 입력.

- [ ] **Step 4: Player 오브젝트에 컴포넌트 추가**

씬의 `Player` 오브젝트 선택 후 추가:
- `StaminaController` (MaxStamina: 100, RecoveryRate: 20, RecoveryDelay: 1)
- `PlayerHealth`
- `PlayerCombat`

Inspector 설정:
- `PlayerCombat.Input` → `SoulsInputReader`
- `PlayerCombat.Enemy Layer` → `Enemy` 레이어 체크박스 선택

Player 오브젝트 자신의 Layer를 `Player`로 설정.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Scripts/Player/PlayerHealth.cs Assets/Scripts/Player/PlayerCombat.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: PlayerHealth + PlayerCombat 3-hit 자동 콤보 공격 (1x, 1x, 2x)"
```

---

## Task 7: PlayerDodge (회피 + 무적 프레임)

**Files:**
- Create: `Assets/Scripts/Player/PlayerDodge.cs`

- [ ] **Step 1: PlayerDodge 작성**

`Assets/Scripts/Player/PlayerDodge.cs`:
```csharp
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaController))]
public class PlayerDodge : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _dodgeDistance = 4f;
    [SerializeField] float _dodgeDuration = 0.3f;
    [SerializeField] float _iFrameDuration = 0.2f;
    [SerializeField] float _staminaCost = 25f;
    [SerializeField] float _cooldown = 0.5f;

    CharacterController _cc;
    StaminaController _stamina;
    PlayerController _controller;

    public bool IsDodging { get; private set; }
    public bool IsInvincible { get; private set; }

    bool _onCooldown;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _stamina = GetComponent<StaminaController>();
        _controller = GetComponent<PlayerController>();
        _input.DodgePerformed += OnDodgeInput;
    }

    void OnDodgeInput()
    {
        if (IsDodging || _onCooldown) return;
        if (!_stamina.TryConsume(_staminaCost)) return;

        var dir = (_controller != null && _controller.MoveDirection.sqrMagnitude > 0.01f)
            ? _controller.MoveDirection
            : transform.forward;

        StartCoroutine(DodgeCoroutine(dir));
    }

    IEnumerator DodgeCoroutine(Vector3 direction)
    {
        IsDodging = true;
        _onCooldown = true;
        IsInvincible = true;

        float elapsed = 0f;
        float speed = _dodgeDistance / _dodgeDuration;

        while (elapsed < _dodgeDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= _iFrameDuration) IsInvincible = false;
            _cc.Move(direction * speed * Time.deltaTime);
            yield return null;
        }

        IsInvincible = false;
        IsDodging = false;

        yield return new WaitForSeconds(_cooldown);
        _onCooldown = false;
    }
}
```

- [ ] **Step 2: Player 오브젝트에 PlayerDodge 추가**

씬의 `Player` 오브젝트에 `PlayerDodge` 컴포넌트 추가.  
`Input` 필드에 `SoulsInputReader` 드래그.

- [ ] **Step 3: Play Mode에서 회피 확인**

Play Mode 진입. Space 누르면 이동 방향으로 구르기 이동.  
스태미나 4번 소모(100 ÷ 25) 후 Space 입력 → 회피 불가 확인.  
1초 대기 후 스태미나 회복, 다시 회피 가능 확인.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Player/PlayerDodge.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: PlayerDodge — 구르기 + 0.2s 무적 프레임, 스태미나 25 소모"
```

---

## Task 8: DummyEnemy + ParryReceiver + DummyEnemyAttack

**Files:**
- Create: `Assets/Scripts/Enemy/DummyEnemy.cs`
- Create: `Assets/Scripts/Enemy/ParryReceiver.cs`
- Create: `Assets/Scripts/Enemy/DummyEnemyAttack.cs`

- [ ] **Step 1: DummyEnemy 작성**

`Assets/Scripts/Enemy/DummyEnemy.cs`:
```csharp
using System;
using UnityEngine;

public class DummyEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] float _maxHp = 200f;

    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsGroggy { get; private set; }
    public bool IsInvincible { get; private set; } = false;

    public event Action<float> OnHpChanged;
    public event Action OnDeath;

    Renderer _renderer;
    Color _defaultColor;

    void Awake()
    {
        CurrentHp = _maxHp;
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _defaultColor = _renderer.material.color;
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsInvincible) return;
        float actual = IsGroggy ? amount * 2f : amount;
        CurrentHp = Mathf.Max(0f, CurrentHp - actual);
        OnHpChanged?.Invoke(CurrentHp / _maxHp);
        Debug.Log($"[Enemy] HP: {CurrentHp:F0}/{_maxHp} (데미지: {actual:F0})");
        if (CurrentHp <= 0f) OnDeath?.Invoke();
    }

    public void SetGroggy(bool value)
    {
        IsGroggy = value;
        if (_renderer != null)
            _renderer.material.color = value ? Color.yellow : _defaultColor;
    }
}
```

- [ ] **Step 2: ParryReceiver 작성**

`Assets/Scripts/Enemy/ParryReceiver.cs`:
```csharp
using System.Collections;
using UnityEngine;

public class ParryReceiver : MonoBehaviour
{
    public bool IsParryable { get; private set; }

    public void OpenWindow(float duration) => StartCoroutine(WindowRoutine(duration));

    IEnumerator WindowRoutine(float duration)
    {
        IsParryable = true;
        yield return new WaitForSeconds(duration);
        IsParryable = false;
    }
}
```

- [ ] **Step 3: DummyEnemyAttack 작성**

`Assets/Scripts/Enemy/DummyEnemyAttack.cs`:
```csharp
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParryReceiver))]
public class DummyEnemyAttack : MonoBehaviour
{
    [SerializeField] float _attackInterval = 3f;
    [SerializeField] float _windupTime = 0.6f;
    [SerializeField] float _parryWindowDuration = 0.4f;
    [SerializeField] float _attackDamage = 25f;
    [SerializeField] float _attackRange = 2f;
    [SerializeField] LayerMask _playerLayer;

    ParryReceiver _parryReceiver;
    Transform _player;

    void Awake()
    {
        _parryReceiver = GetComponent<ParryReceiver>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Start() => StartCoroutine(AttackLoop());

    IEnumerator AttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_attackInterval);
            yield return AttackOnce();
        }
    }

    IEnumerator AttackOnce()
    {
        Debug.Log("[Enemy] ⚠ 공격 예고!");
        yield return new WaitForSeconds(_windupTime);

        _parryReceiver.OpenWindow(_parryWindowDuration);

        // 패링 윈도우 절반 시점에 타격 판정
        yield return new WaitForSeconds(_parryWindowDuration * 0.5f);

        if (_player != null && Vector3.Distance(transform.position, _player.position) <= _attackRange)
        {
            if (_player.TryGetComponent<IDamageable>(out var dmg) && !dmg.IsInvincible)
            {
                dmg.TakeDamage(_attackDamage, gameObject);
                Debug.Log("[Enemy] 타격!");
            }
        }

        yield return new WaitForSeconds(_parryWindowDuration * 0.5f);
    }
}
```

- [ ] **Step 4: 씬에 더미 적 배치**

Hierarchy 우클릭 > 3D Object > Capsule. 이름: `DummyEnemy`.  
Layer: `Enemy` 선택.  
Position: `(0, 0, 3)` (Player 앞 3m).  
컴포넌트 추가:
- `DummyEnemy`
- `ParryReceiver`
- `DummyEnemyAttack`

`DummyEnemyAttack.Player Layer` 필드: `Player` 레이어 체크.

- [ ] **Step 5: Play Mode에서 적 공격 흐름 확인**

Play Mode 진입. 3초마다 Console:
1. `[Enemy] ⚠ 공격 예고!`
2. Player가 범위 내면: `[Enemy] 타격!` + `[Player] HP: 75/100`

회피(Space) 후 적 공격 범위에서 벗어나면 타격 없음 확인.

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Enemy/
git commit -m "feat: DummyEnemy HP/그로기 + ParryReceiver 패링 윈도우 + DummyEnemyAttack 공격 패턴"
```

---

## Task 9: PlayerCombat 확장 — 패링 + Riposte

**Files:**
- Modify: `Assets/Scripts/Player/PlayerCombat.cs`

- [ ] **Step 1: PlayerCombat.Awake에 패링 입력 구독 추가**

`Assets/Scripts/Player/PlayerCombat.cs`의 `Awake` 메서드 수정:

기존:
```csharp
void Awake()
{
    _input.AttackStarted += OnAttackInput;
}
```

변경 후:
```csharp
void Awake()
{
    _input.AttackStarted += OnAttackInput;
    _input.ParryPerformed += OnParryInput;
}
```

- [ ] **Step 2: 패링 관련 필드 추가**

클래스 상단 기존 필드 아래에 추가:
```csharp
[SerializeField] float _parryActiveDuration = 0.35f;
[SerializeField] float _parryCooldown = 0.8f;
[SerializeField] float _parryDetectionRadius = 2f;
bool _isParryCooldown;
```

- [ ] **Step 3: 패링 메서드 추가**

클래스 마지막에 추가:
```csharp
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
        var hits = Physics.OverlapSphere(
            transform.position + Vector3.up * 0.8f,
            _parryDetectionRadius,
            _enemyLayer);

        foreach (var h in hits)
        {
            if (h.TryGetComponent<ParryReceiver>(out var pr) && pr.IsParryable)
            {
                if (h.TryGetComponent<DummyEnemy>(out var enemy))
                {
                    success = true;
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
```

- [ ] **Step 4: Play Mode에서 패링 흐름 확인**

Play Mode. 적 앞에서 대기.  
`[Enemy] ⚠ 공격 예고!` 출력 후 0.6초 windup 중 RMB 클릭.  
Expected:
- `[Parry] 성공!` 출력
- 적 오브젝트 노란색으로 변함 (Groggy)
- LMB 클릭 → `[Enemy] HP: 80/200 (데미지: 120.0)` (60 * 2배 Groggy)
- 적 원래 색으로 복귀

- [ ] **Step 5: 패링 실패 확인**

적 공격 예고 없이 RMB 클릭.  
Expected: `[Parry] 실패` 출력, 적 상태 변화 없음.

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/Player/PlayerCombat.cs
git commit -m "feat: 패링 + Riposte — 적 공격 윈도우 중 패링 성공 시 그로기, 2초 내 치명타"
```

---

## Task 10: LockOnSystem

**Files:**
- Create: `Assets/Scripts/Player/LockOnSystem.cs`

- [ ] **Step 1: LockOnSystem 작성**

`Assets/Scripts/Player/LockOnSystem.cs`:
```csharp
using UnityEngine;

public class LockOnSystem : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] PlayerCamera _playerCamera;
    [SerializeField] float _range = 15f;
    [SerializeField] float _maxAngle = 60f;
    [SerializeField] LayerMask _enemyLayer;

    Transform _currentTarget;

    void Awake() => _input.LockOnPerformed += ToggleLockOn;

    void Update()
    {
        if (_currentTarget == null) return;
        if (_currentTarget.TryGetComponent<DummyEnemy>(out var e) && e.CurrentHp <= 0f)
        {
            _currentTarget = null;
            _playerCamera.LockOnTarget = null;
        }
    }

    void ToggleLockOn()
    {
        if (_currentTarget != null)
        {
            _currentTarget = null;
            _playerCamera.LockOnTarget = null;
            Debug.Log("[LockOn] 해제");
            return;
        }

        var best = FindBestTarget();
        if (best != null)
        {
            _currentTarget = best;
            _playerCamera.LockOnTarget = best;
            Debug.Log($"[LockOn] 타겟 잠금: {best.name}");
        }
        else
        {
            Debug.Log("[LockOn] 범위 내 타겟 없음");
        }
    }

    Transform FindBestTarget()
    {
        var hits = Physics.OverlapSphere(transform.position, _range, _enemyLayer);
        Transform best = null;
        float bestScore = float.MaxValue;
        var camForward = Camera.main.transform.forward;

        foreach (var h in hits)
        {
            var toTarget = h.transform.position - transform.position;
            float angle = Vector3.Angle(camForward, toTarget);
            if (angle > _maxAngle) continue;

            float score = toTarget.magnitude + angle * 0.1f;
            if (score < bestScore) { bestScore = score; best = h.transform; }
        }

        return best;
    }
}
```

- [ ] **Step 2: Player 오브젝트에 LockOnSystem 추가**

씬의 `Player` 오브젝트에 `LockOnSystem` 컴포넌트 추가.  
Inspector 설정:
- `Input` → `SoulsInputReader`
- `Player Camera` → `Main Camera` 오브젝트 드래그 (PlayerCamera 컴포넌트가 있음)
- `Enemy Layer` → `Enemy` 체크

- [ ] **Step 3: Play Mode에서 락온 확인**

Play Mode 진입. 적과 같은 화면에 두고 Middle Mouse 클릭.  
Expected:
- `[LockOn] 타겟 잠금: DummyEnemy`
- 카메라가 적을 향해 yaw 고정
- WASD 이동 중 카메라가 적 방향 유지
- Middle Mouse 재클릭 → `[LockOn] 해제`

적 HP가 0이 되면 자동으로 락온 해제되는지 확인.

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Player/LockOnSystem.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: LockOnSystem — 카메라 전방 기준 가장 가까운 적 락온 + 사망 시 자동 해제"
```

---

## Task 11: CombatHUD + 씬 최종 구성 + 수동 검증

**Files:**
- Create: `Assets/Scripts/UI/CombatHUD.cs`

- [ ] **Step 1: CombatHUD 작성**

`Assets/Scripts/UI/CombatHUD.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    [SerializeField] Slider _hpSlider;
    [SerializeField] Slider _staminaSlider;
    [SerializeField] StaminaController _stamina;
    [SerializeField] PlayerHealth _playerHealth;

    void Awake()
    {
        _playerHealth.OnHpChanged += v => _hpSlider.value = v;
        _stamina.Stamina.OnChanged += v => _staminaSlider.value = v / _stamina.Stamina.Max;
    }

    void Start()
    {
        _hpSlider.value = 1f;
        _staminaSlider.value = 1f;
    }
}
```

- [ ] **Step 2: Canvas + UI Slider 생성**

Hierarchy 우클릭 > UI > Canvas.  
Canvas Inspector에서 Canvas Scaler > UI Scale Mode: `Scale With Screen Size`.

Canvas 하위에:
1. `HP_Slider` 생성 (UI > Slider):
   - Rect Transform: Anchor Presets → 좌측 상단 (top-left)
   - Pos X: 160, Pos Y: -30, Width: 300, Height: 20
   - Slider > Fill Rect > Fill의 색을 빨간색으로 변경
2. `Stamina_Slider` 생성 (UI > Slider):
   - Pos X: 160, Pos Y: -60, Width: 300, Height: 20
   - Fill 색을 노란색으로 변경

- [ ] **Step 3: CombatHUD 컴포넌트 연결**

Canvas 오브젝트 선택, `CombatHUD` 컴포넌트 추가.  
필드 설정:
- `Hp Slider` → `HP_Slider`
- `Stamina Slider` → `Stamina_Slider`
- `Stamina` → `Player`의 StaminaController
- `Player Health` → `Player`의 PlayerHealth

- [ ] **Step 4: 씬 전체 컴포넌트 최종 점검**

`Player` 오브젝트에 아래 컴포넌트 전부 확인:
- CharacterController
- PlayerController (Input: SoulsInputReader)
- StaminaController
- PlayerHealth
- PlayerCombat (Input: SoulsInputReader, Enemy Layer: Enemy)
- PlayerDodge (Input: SoulsInputReader)
- LockOnSystem (Input: SoulsInputReader, PlayerCamera: Main Camera, Enemy Layer: Enemy)

`Main Camera` 오브젝트:
- PlayerCamera (Input: SoulsInputReader, Target: Player)

`DummyEnemy` 오브젝트:
- DummyEnemy
- ParryReceiver
- DummyEnemyAttack (Player Layer: Player)

- [ ] **Step 5: 수동 검증 체크리스트**

Play Mode에서 순서대로 확인:

| 항목 | 확인 내용 | 결과 |
|------|-----------|------|
| 이동 | WASD로 이동, 카메라 기준 방향 | ☐ |
| 카메라 | 마우스로 플레이어 주위 궤도 회전 | ☐ |
| 공격 | LMB 3번 클릭 → 3번째 데미지 2배 | ☐ |
| 스태미나 바 | 회피 시 감소, 1초 후 자동 회복 | ☐ |
| 회피 | Space → 구르기, 무적 시간 중 피격 없음 | ☐ |
| 패링 | 공격 예고 직후 RMB → 성공/실패 로그 | ☐ |
| 치명타 | 패링 성공 후 LMB → 120 데미지 | ☐ |
| 락온 | Middle Mouse → 카메라 고정, 재클릭 해제 | ☐ |
| HP 바 | 적 공격 받을 때 슬라이더 감소 | ☐ |
| 적 사망 | 적 HP 0 → 락온 자동 해제 | ☐ |

- [ ] **Step 6: 최종 커밋**

```bash
git add Assets/Scripts/UI/CombatHUD.cs Assets/Scenes/SampleScene.unity
git commit -m "feat: Phase 1 전투 프로토타입 완성 — 공격·회피·패링·락온·HUD"
```
