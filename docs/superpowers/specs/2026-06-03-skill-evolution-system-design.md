# 스킬 진화 시스템 설계

## 개요

로그라이크 런 내에서 공능서(스킬북) 픽업 시 스킬이 진화하는 시스템.
스킬은 트리/그래프 구조로 연결되며, 진화할 때마다 현재 스킬의 `nextSkillIds`에서
최대 3개를 랜덤 제시하고 플레이어가 선택한다.
런 종료 시 모든 진화 상태는 초기화된다.

---

## 데이터 레이어

### SkillDef.cs

`coeffPerLevel` 제거. `nextSkillIds` 추가.

```cs
[Serializable]
public class SkillDef
{
    public string   skillId;
    public string   effectId;
    public float    baseCoefficient;
    public string[] nextSkillIds;   // 다음 진화 후보 skillId 목록
}
```

### SkillTable.json 구조

각 스킬은 독립 항목. 진화형도 개별 항목으로 등록.

```json
{
  "entries": [
    {
      "skillId": "knight_attack01",
      "effectId": "slash_light",
      "baseCoefficient": 1.0,
      "nextSkillIds": ["knight_attack01_fire", "knight_attack01_ice", "knight_attack01_multi"]
    },
    {
      "skillId": "knight_attack01_fire",
      "effectId": "slash_fire",
      "baseCoefficient": 1.3,
      "nextSkillIds": ["knight_attack01_fire_large", "knight_attack01_fire_dot"]
    },
    {
      "skillId": "knight_attack01_fire_large",
      "effectId": "slash_fire_large",
      "baseCoefficient": 1.5,
      "nextSkillIds": []
    }
  ]
}
```

`nextSkillIds`가 빈 배열이면 최대 진화 상태.

### EffectTable.json

변경 없음. 진화형 스킬마다 대응하는 effectId 항목을 추가하는 방식으로 확장.

---

## 상태 레이어

### PlayerSkillState.cs (신규 MonoBehaviour)

Player GameObject에 추가. 런 시작 시 자동 초기화.

```
책임:
  - 슬롯별 현재 skillId 관리
  - GetCurrentId(baseSkillId) → 현재 진화된 skillId 반환
  - Evolve(baseSkillId, chosenSkillId) → 현재 skillId 교체
  - 런 시작 시 초기화 (씬 로드로 자동 처리)

내부 구조:
  Dictionary<string baseSkillId, string currentSkillId>
  초기값: currentSkillId = baseSkillId (진화 없음)
```

baseSkillId는 `PlayerCombat._comboSkillIds` 배열의 원본 ID.
진화 후에도 슬롯 키는 baseSkillId로 고정.

---

## 픽업 흐름

### SkillBook.cs (신규 MonoBehaviour)

씬에 배치되는 픽업 오브젝트.

```
필드: string baseSkillId

OnTriggerEnter2D("Player" 태그):
  1. PlayerSkillState.GetCurrentId(baseSkillId)로 현재 ID 조회
  2. SkillTable.Get(currentId).nextSkillIds 가져오기
  3. nextSkillIds가 비어있으면 → Destroy (최대 진화, 픽업 무시)
  4. nextSkillIds에서 랜덤 min(3, 개수)개 선택
  5. SkillUpgradeUI.Show(baseSkillId, 후보 SkillDef[]) 호출
  6. Destroy(gameObject)
```

### SkillUpgradeUI.cs (신규)

Canvas 위에 카드 3장을 표시하는 UI. Time.timeScale을 사용해 게임 일시정지.
씬에 싱글톤으로 배치. `SkillBook`은 `SkillUpgradeUI.Instance`로 참조.

```
Show(baseSkillId, SkillDef[] options):
  - Time.timeScale = 0
  - 카드 N장 생성 (최대 3)
  - 각 카드: 스킬 이름 + baseCoefficient 표시

OnCardSelected(baseSkillId, chosenSkillId):
  - PlayerSkillState.Evolve(baseSkillId, chosenSkillId)
  - Time.timeScale = 1
  - UI 닫기
```

카드 비주얼은 텍스트 전용으로 시작. 추후 아이콘/이펙트 추가 가능.

---

## PlayerCombat 연동

`SpawnEffects()`에서 `SkillTable.Get(skillId)` 직접 호출 대신
`PlayerSkillState.GetCurrentId()`를 거쳐 현재 진화 ID 조회.

```cs
void SpawnEffects(string baseSkillId)
{
    var id     = _skillState != null ? _skillState.GetCurrentId(baseSkillId) : baseSkillId;
    var skill  = SkillTable.Get(id);
    if (skill == null) return;
    var effect = EffectTable.Get(skill.effectId);
    if (effect == null) return;

    float damage = _baseDamage * skill.baseCoefficient;
    foreach (var entry in effect.hitboxes)
        StartCoroutine(SpawnHitboxAt(entry, damage));
}
```

`_skillState`가 없으면 baseSkillId를 그대로 사용 (하위 호환).

---

## 데미지 공식

```
finalDamage = _baseDamage * skill.baseCoefficient
```

- `_baseDamage`: PlayerCombat 직렬화 필드 (현재 20f)
- `skill.baseCoefficient`: 현재 진화된 스킬의 계수
- 추후 장비/스탯 시스템 도입 시 `_baseDamage`를 `PlayerStat`에서 읽는 방식으로 교체

---

## 파일 변경 목록

| 파일 | 변경 유형 | 내용 |
|------|-----------|------|
| `Assets/Scripts/Data/SkillDef.cs` | 수정 | `coeffPerLevel` 제거, `nextSkillIds` 추가 |
| `Assets/Resources/Data/SkillTable.json` | 수정 | 각 스킬에 `nextSkillIds` 추가, 진화형 항목 추가 |
| `Assets/Scripts/Player/PlayerSkillState.cs` | 신규 | 슬롯별 현재 skillId 관리 |
| `Assets/Scripts/Combat/SkillBook.cs` | 신규 | 픽업 트리거, UI 호출 |
| `Assets/Scripts/UI/SkillUpgradeUI.cs` | 신규 | 카드 선택 UI, timeScale 제어 |
| `Assets/Scripts/Player/PlayerCombat.cs` | 수정 | SpawnEffects에서 PlayerSkillState 경유 |

---

## 범위 외 (이번 구현에서 제외)

- 진화형 스킬의 EffectTable 항목 (slash_fire 등) — 데이터 작성은 별도 작업
- 카드 UI 비주얼 (아이콘, 이펙트) — 텍스트 전용으로 시작
- PlayerStat 연동 — 추후
- 최대 진화 시 대체 보상 — 추후
