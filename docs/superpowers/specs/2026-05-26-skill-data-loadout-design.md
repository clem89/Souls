# SkillData & PlayerLoadout 확장 설계

## 개요

`SkillEffectData`를 `SkillData`로 완전 대체하고, `PlayerLoadout`에 스킬 슬롯 3개를 추가한다.
약공격(lightAttack)과 스킬(1~3번 슬롯) 모두 단일 `SkillData` 클래스로 통일한다.

---

## SkillData (ScriptableObject)

```csharp
public enum SkillType { Instant, Projectile, Area }

[CreateAssetMenu(menuName = "Souls/Combat/Skill Data")]
public class SkillData : ScriptableObject
{
    // 식별
    public string skillName;

    // 이펙트
    public GameObject effectPrefab;
    [Min(1)] public int poolSize = 4;

    // 실행 타입
    public SkillType type = SkillType.Instant;

    // 전투
    public float damage = 20f;
    public float range = 1.2f;       // 플레이어에서 스폰 거리
    public float lifetime = 0.12f;   // 히트박스 유지 시간

    // Projectile 전용
    public float projectileSpeed = 8f;  // type == Projectile일 때만 사용

    // 비용
    public float cooldown = 0f;      // lightAttack은 0
    public float staminaCost = 0f;   // lightAttack은 0
}
```

### SkillType 별 실행 방식

| Type | 히트박스 동작 |
|------|--------------|
| Instant | 플레이어 위치 + forward * range에 스폰, lifetime 후 만료 |
| Projectile | 스폰 후 forward 방향으로 projectileSpeed 속도로 이동, 충돌 또는 lifetime 만료 시 반환 |
| Area | 플레이어 위치 중심으로 스폰, lifetime 동안 유지 (범위 지속 피해) — 이번 구현에서는 데이터 정의만, 실행 로직은 추후 |

---

## PlayerLoadout (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "Souls/Combat/Player Loadout")]
public class PlayerLoadout : ScriptableObject
{
    public SkillData lightAttack;
    public SkillData[] skills = new SkillData[3];  // 슬롯 1, 2, 3
}
```

---

## SkillEffectPoolManager 변경

- `SkillEffectData` 참조 → `SkillData` 참조로 교체
- `Awake`에서 `lightAttack` + `skills[0~2]` null 체크 후 각각 풀 등록
- `GetPool(SkillData)` 시그니처 유지 (타입만 변경)

---

## PlayerCombat 변경

- `lightAttack` 실행 로직 유지 (SkillData 타입만 교체)
- `UseSkill(int slot)` 메서드 추가:
  - `skills[slot]` null 체크
  - 쿨타임/스태미나 검사
  - 풀에서 히트박스 꺼내 Fire
- 슬롯별 쿨타임 `float[]` 배열로 관리

---

## InputReader 변경

- `Skill1Performed`, `Skill2Performed`, `Skill3Performed` 이벤트 추가
- 키바인딩: `1`, `2`, `3` 키 (InputSystem_Actions에 Action 추가)

---

## AttackHitbox 변경 (Projectile 지원)

- `Fire()` 호출 시 `SkillType`과 `projectileSpeed` 전달
- `type == Projectile`이면 `FixedUpdate`에서 `Rigidbody2D.MovePosition` 으로 전진
- 충돌 또는 lifetime 만료 시 `Expire()` 호출

---

## 변경 파일 요약

| 파일 | 작업 |
|------|------|
| `Assets/Scripts/Combat/SkillData.cs` | 신규 생성 |
| `Assets/Scripts/Combat/SkillEffectData.cs` | 삭제 |
| `Assets/Scripts/Combat/PlayerLoadout.cs` | skills 슬롯 3개 추가 |
| `Assets/Scripts/Combat/AttackHitbox.cs` | Projectile 이동 지원 추가 |
| `Assets/Scripts/Combat/SkillEffectPool.cs` | SkillData 타입으로 교체 |
| `Assets/Scripts/Combat/SkillEffectPoolManager.cs` | SkillData 기반으로 수정 |
| `Assets/Scripts/Player/PlayerCombat.cs` | UseSkill + 쿨타임 관리 추가 |
| `Assets/Scripts/Input/InputReader.cs` | Skill1/2/3 이벤트 추가 |
| `Assets/Data/Skills/LightSlash.asset` | SkillData 기반으로 재생성 |

---

## 마이그레이션 참고

- `LightSlash.asset` 재생성 시 값: skillName="LightSlash", type=Instant, damage=20, range=1.2, lifetime=0.12, poolSize=4, cooldown=0, staminaCost=0
- `DefaultLoadout.asset`의 `lightAttack` 필드를 새 asset으로 재연결
