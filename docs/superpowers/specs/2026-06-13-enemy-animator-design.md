# Enemy Animator System Design

**Date:** 2026-06-13
**Scope:** 적 캐릭터 애니메이션 시스템 — Generator + EnemyAnimator 컴포넌트. 적 AI는 포함하지 않음.

---

## Goal

어떤 적 캐릭터든 이름 하나로 AnimationClip + AnimatorController를 자동 생성하고, `EnemyAnimator` 컴포넌트가 기존 `IDamageable` 이벤트를 구독해 Hurt/Death 애니메이션을 구동한다. Walk/Attack은 파라미터만 준비해두고 추후 AI 시스템에서 구동한다.

---

## Architecture

### 1. EnemyAnimatorGenerator (Editor 스크립트)

- 경로: `Assets/Editor/EnemyAnimatorGenerator.cs`
- `KnightAnimatorGenerator`와 완전히 독립된 별개 클래스
- Unity 메뉴: `Tools → Animator Generator → Generate Enemy Animator`
- 캐릭터 이름 목록을 하드코딩(`Skeleton`, `Orc`, `Slime` 등) 또는 `EditorGUILayout.TextField`로 입력
- 스프라이트 탐색 경로: `Assets/Resources/Characters(100x100)/{Name}/{Name}/{Name}-{State}.png`
- 지원 상태 목록 (순서대로 시도, 없으면 스킵):
  - `Idle` (loop), `Walk` (loop), `Attack01` (no-loop), `Attack02` (no-loop), `Block` (loop), `Hurt` (no-loop), `Death` (no-loop)
- 클립 출력: `Assets/Animations/Enemies/{Name}/{Name}-{State}.anim`
- 컨트롤러 출력: `Assets/Animations/Enemies/{Name}/{Name}Animator.controller`

### 2. 공통 AnimatorController 파라미터

모든 적이 동일한 파라미터 이름 사용. 클립이 없는 상태는 컨트롤러에 추가하지 않음.

| 파라미터 | 타입 | 용도 |
|----------|------|------|
| `Speed` | Float | ≥ 0.01 → Walk, < 0.01 → Idle |
| `AttackTrigger` | Trigger | Attack01/02 전환 |
| `HurtTrigger` | Trigger | Hurt 클립 재생 |
| `IsDead` | Bool | Death 클립, AnyState 최우선 전환 |

전환 규칙:
- AnyState → Dead (IsDead == true, 최우선)
- AnyState → Hurt (HurtTrigger, exitTime 없음)
- Hurt → Idle (exitTime = 1.0)
- Idle ↔ Walk (Speed 임계값 0.01)
- AnyState → Attack01 (AttackTrigger, Attack02 클립 존재 시 Attack01 exitTime에서 Attack02로)

### 3. EnemyAnimator (런타임 컴포넌트)

- 경로: `Assets/Scripts/Enemy/EnemyAnimator.cs`
- `PlayerAnimator`와 완전히 독립된 별개 클래스
- 의존: `IDamageable` 인터페이스만 (Unity 엔진 외 외부 의존 없음)

```
EnemyAnimator
  ├── Awake: GetComponentInChildren<Animator>, GetComponent<IDamageable 구현체>
  ├── OnEnable: OnHpChanged, OnDeath 이벤트 구독
  ├── OnDisable: 이벤트 구독 해제
  ├── Update: Speed 파라미터 갱신 (현재는 0f 고정, AI 연결 시 교체)
  ├── OnHpChanged(ratio): ratio > 0 → HurtTrigger 세트
  └── OnDeath: IsDead = true
```

- `flipX`: `IDamageable`에 방향 정보가 없으므로 AI 붙을 때까지 보류
- IFrame 깜빡임: 적에게 불필요 — 미구현

---

## File Map

| 경로 | 유형 | 역할 |
|------|------|------|
| `Assets/Editor/EnemyAnimatorGenerator.cs` | 신규 | 클립 + 컨트롤러 자동 생성 |
| `Assets/Scripts/Enemy/EnemyAnimator.cs` | 신규 | 이벤트 기반 애니메이션 구동 |
| `Assets/Animations/Enemies/Skeleton/*.anim` | 생성물 | Skeleton 클립들 |
| `Assets/Animations/Enemies/Skeleton/SkeletonAnimator.controller` | 생성물 | Skeleton 컨트롤러 |

---

## 격리 원칙

- `EnemyAnimatorGenerator`는 `KnightAnimatorGenerator` 코드를 참조하거나 상속하지 않는다.
- `EnemyAnimator`는 `PlayerAnimator` 코드를 참조하거나 상속하지 않는다.
- 적 애니메이션 생성물은 `Assets/Animations/Enemies/` 하위에만 저장한다.

---

## 성공 기준

1. `Tools → Animator Generator → Generate Enemy Animator` 실행 → `Assets/Animations/Enemies/Skeleton/` 에 클립 + 컨트롤러 생성
2. DummyEnemy GameObject에 `EnemyAnimator` 추가 + Animator 컴포넌트에 `SkeletonAnimator.controller` 할당
3. Play Mode: 피격 시 Hurt 애니메이션 재생 후 Idle 복귀, 사망 시 Death 애니메이션 정지
4. 기존 PlayerAnimator / KnightAnimatorGenerator 동작에 영향 없음

---

## 보류 항목 (AI 단계에서 구현)

- `Speed` 파라미터 실제 구동 (이동 AI)
- `AttackTrigger` 실제 구동 (공격 AI)
- `flipX` 방향 전환
