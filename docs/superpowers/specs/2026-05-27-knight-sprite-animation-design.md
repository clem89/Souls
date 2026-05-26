# Knight 스프라이트 애니메이션 시스템 설계

**날짜:** 2026-05-27  
**상태:** 승인됨

---

## 배경

현재 `PlayerAnimator.cs`는 프로토타입 수준으로 SpriteRenderer의 색상만 바꾸는 방식으로 상태를 표시한다. `Assets/Resources/Characters(100x100)/Knight/` 아래 Idle/Walk/Attack01-03/Block/Hurt/Death 스프라이트시트가 완비되어 있으므로, 이를 Unity Animator 기반 실제 애니메이션으로 교체한다.

---

## 생성 산출물

```
Assets/
  Animations/
    Knight/
      Knight-Idle.anim
      Knight-Walk.anim
      Knight-Attack01.anim
      Knight-Attack02.anim
      Knight-Attack03.anim
      Knight-Block.anim        ← Parry + Riposte 공용
      Knight-Hurt.anim
      Knight-Death.anim
    KnightAnimator.controller
```

---

## Animator State Machine

```
[Idle] ←→ [Walk]  (Speed > 0)
  |
  ├─ AttackTrigger → [Attack1] → [Attack2] → [Attack3]
  ├─ ParryTrigger  → [Parry]   → [Riposte]
  ├─ DodgeTrigger  → [Walk 재사용 + IFrame alpha 플래시]
  |
  Any State → [Hurt]  → [Idle] 복귀
  Any State → [Death]          (최우선)
```

- **Dodge 전용 스프라이트 없음** → Walk 애니메이션 재사용. IFrame 중 alpha 깜빡임은 코드로 처리.
- **Riposte** → Block(Knight-Block) 애니메이션 재사용.

---

## Animator 파라미터

| 파라미터 | 타입 | 용도 |
|---|---|---|
| `Speed` | float | 0=Idle, >0=Walk |
| `AttackTrigger` | Trigger | 공격 시작 |
| `ComboStep` | int | 1 / 2 / 3 |
| `ParryTrigger` | Trigger | 패리 시작 |
| `RiposteTrigger` | Trigger | 리포스트 시작 |
| `HurtTrigger` | Trigger | 피격 |
| `DodgeTrigger` | Trigger | 회피 시작 |
| `IsDead` | bool | 사망 (최우선 전환) |

---

## 애니메이션 설정

| 클립 | Loop | FPS | 비고 |
|---|---|---|---|
| Knight-Idle | true | 8 | |
| Knight-Walk | true | 8 | Dodge 중에도 재사용 |
| Knight-Attack01 | false | 10 | |
| Knight-Attack02 | false | 10 | |
| Knight-Attack03 | false | 10 | |
| Knight-Block | true | 8 | Parry + Riposte 공용 |
| Knight-Hurt | false | 10 | 종료 후 Idle 복귀 |
| Knight-Death | false | 10 | 마지막 프레임 정지 |

---

## PlayerAnimator.cs 변경 사항

- `Animator` 컴포넌트 참조 추가 (`GetComponent<Animator>`)
- `ResolveColor()` 제거 → `UpdateAnimator()` 로 교체
  - 매 LateUpdate마다 파라미터 갱신 (기존 폴링 방식 유지)
- IFrame 감지 시 `SpriteRenderer.color.a` 를 코드에서 0.5 ↔ 1.0 깜빡임 유지
- `PlayerHealth.OnHpChanged` 이벤트 구독 → HurtTrigger 발동
- Hurt 상태 추가 (PlayerAnimator에 현재 없는 상태 — Hurt 애니메이션 재생 후 자동 복귀)

---

## 구현 범위 제외 (이번 이터레이션)

- Split Effects (Attack 이펙트 별도 레이어) — 다음 이터레이션
- Shadow sprites 처리
- 다른 캐릭터 (Knight Templar, Soldier 등)

---

## 스프라이트 소스 경로

```
Assets/Resources/Characters(100x100)/Knight/Knight/
  Knight-Idle.png      (sliced: 100x100, n frames)
  Knight-Walk.png
  Knight-Attack01.png
  Knight-Attack02.png
  Knight-Attack03.png
  Knight-Block.png
  Knight-Hurt.png
  Knight-Death.png
```

슬라이싱은 `SpriteSheetSlicer` Editor 스크립트로 완료 (Tools → Sprite Slicer → Slice Character Sprites).
