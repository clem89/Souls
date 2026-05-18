# 플레이어 캐릭터 모델 + 애니메이션 설계

**작성일:** 2026-05-19
**상태:** 🚧 진행 중 (에셋 다운로드 완료, 설계 미완)

---

## 1. 결정 사항

### 방향
- 플레이어 캐릭터에 3D 모델(메시) + 애니메이션 연동
- 스타일: 빠른 프로토타입 우선 → 비주얼 polish는 나중에
- 에셋: Mixamo 무료 (Y Bot + 애니메이션)

### Mixamo 다운로드 목록 (완료)
| 항목 | Mixamo 설정 | 용도 |
|------|------------|------|
| Y Bot 캐릭터 | With Skin, FBX for Unity | 플레이어 메시 |
| Idle | Without Skin | 정지 상태 |
| Running | Without Skin | 이동 (Idle ↔ Run 2단계) |
| Sword And Shield Slash ×3 | Without Skin | 콤보 1·2·3타 |
| Dodge Roll | Without Skin | 회피 |
| Blocking | Without Skin | 패링 |
| Stabbing | Without Skin | 리포스트 (패링 후 치명타) |
| Dying | Without Skin | 사망 |

### Unity 임포트 설정
- Y Bot FBX + 모든 애니메이션 FBX → Rig 탭 → `Animation Type: Humanoid`
- 별도 리타게팅 불필요

---

## 2. 미완 — 다음 세션에서 설계할 것

- [ ] Animator Controller 스테이트 머신 설계 (파라미터, 전환 조건)
- [ ] `PlayerAnimator` 스크립트 설계 (기존 스크립트 ↔ Animator 연결)
- [ ] `PlayerCombat`에 노출 필요한 프로퍼티 (IsAttacking, ComboStep 등)
- [ ] `PlayerHealth`에 OnDeath 이벤트 추가 여부
- [ ] 씬 세팅 절차

---

## 3. 기존 코드 현황 (참고)

연동 대상 스크립트:
- `PlayerController` — `MoveDirection` (public) → Speed 파라미터
- `PlayerDodge` — `IsDodging` (public) → Dodge 트리거
- `PlayerCombat` — `IsParrying` (public), `RiposteReady` (public), `_isAttacking` (private, 노출 필요), `_comboStep` (private, 노출 필요)
- `PlayerHealth` — 사망 시 이벤트 없음 (추가 필요)
