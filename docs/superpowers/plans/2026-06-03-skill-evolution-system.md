# 스킬 진화 시스템 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 공능서(스킬북) 픽업 시 스킬이 진화 트리를 따라 카드 선택 UI로 업그레이드되는 로그라이크 시스템 구현

**Architecture:** SkillDef에 nextSkillIds 배열로 진화 후보를 정의. PlayerSkillState가 런 중 슬롯별 현재 skillId를 보관. SkillBook 픽업 시 현재 스킬의 nextSkillIds에서 랜덤 3개를 SkillUpgradeUI 카드로 제시, 선택하면 PlayerSkillState에 기록. PlayerCombat은 SpawnEffects 시 PlayerSkillState를 경유해 현재 진화된 skillId를 조회.

**Tech Stack:** Unity 2D, C#, UnityEngine.UI (Button/Text), JsonUtility, Resources.Load

---

## 파일 목록

| 파일 | 유형 | 역할 |
|------|------|------|
| `Assets/Scripts/Data/SkillDef.cs` | 수정 | `coeffPerLevel` 제거, `nextSkillIds` 추가 |
| `Assets/Resources/Data/SkillTable.json` | 수정 | `nextSkillIds` 추가, 진화형 스킬 항목 등록 |
| `Assets/Scripts/Player/PlayerSkillState.cs` | 신규 | 슬롯별 현재 skillId 관리 |
| `Assets/Scripts/Player/PlayerCombat.cs` | 수정 | SpawnEffects에서 PlayerSkillState 경유 |
| `Assets/Scripts/UI/SkillUpgradeUI.cs` | 신규 | 카드 선택 UI, singleton, timeScale 제어 |
| `Assets/Scripts/Combat/SkillBook.cs` | 신규 | 픽업 트리거, 업그레이드 후보 샘플링 |

---

## Task 1: SkillDef 모델 업데이트

**Files:**
- Modify: `Assets/Scripts/Data/SkillDef.cs`

- [ ] **Step 1: SkillDef.cs 수정**

`coeffPerLevel` 필드를 제거하고 `nextSkillIds` 배열을 추가한다. `string[]`은 JsonUtility가 직렬화/역직렬화 가능.

```cs
using System;

[Serializable]
public class SkillDef
{
    public string   skillId;
    public string   effectId;
    public float    baseCoefficient;
    public string[] nextSkillIds;
}
```

- [ ] **Step 2: 컴파일 확인**

Unity 에디터에서 콘솔 에러 없이 컴파일되는지 확인.
`PlayerCombat.cs`에서 `skill.coeffPerLevel` 참조가 있으면 삭제 (현재 없음 — SpawnEffects는 `skill.baseCoefficient`만 사용).

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Data/SkillDef.cs
git commit -m "feat: SkillDef에 nextSkillIds 추가, coeffPerLevel 제거"
```

---

## Task 2: SkillTable.json 업데이트

**Files:**
- Modify: `Assets/Resources/Data/SkillTable.json`

- [ ] **Step 1: 기존 스킬에 nextSkillIds 추가, 진화형 항목 등록**

기존 3개 스킬에 `nextSkillIds` 추가. 진화형 스킬은 우선 기존 effectId를 재사용 (비주얼 구분은 별도 작업).

```json
{
  "entries": [
    {
      "skillId": "knight_attack01",
      "effectId": "slash_light",
      "baseCoefficient": 1.0,
      "nextSkillIds": ["knight_attack01_fire", "knight_attack01_heavy", "knight_attack01_multi"]
    },
    {
      "skillId": "knight_attack02",
      "effectId": "slash_mid",
      "baseCoefficient": 1.0,
      "nextSkillIds": ["knight_attack02_fire", "knight_attack02_heavy"]
    },
    {
      "skillId": "knight_attack03",
      "effectId": "slash_heavy",
      "baseCoefficient": 2.0,
      "nextSkillIds": ["knight_attack03_fire", "knight_attack03_execute"]
    },
    {
      "skillId": "knight_attack01_fire",
      "effectId": "slash_light",
      "baseCoefficient": 1.3,
      "nextSkillIds": ["knight_attack01_fire_large", "knight_attack01_fire_dot"]
    },
    {
      "skillId": "knight_attack01_heavy",
      "effectId": "slash_light",
      "baseCoefficient": 1.5,
      "nextSkillIds": []
    },
    {
      "skillId": "knight_attack01_multi",
      "effectId": "slash_light",
      "baseCoefficient": 1.2,
      "nextSkillIds": []
    },
    {
      "skillId": "knight_attack01_fire_large",
      "effectId": "slash_light",
      "baseCoefficient": 1.6,
      "nextSkillIds": []
    },
    {
      "skillId": "knight_attack01_fire_dot",
      "effectId": "slash_light",
      "baseCoefficient": 1.4,
      "nextSkillIds": []
    },
    {
      "skillId": "knight_attack02_fire",
      "effectId": "slash_mid",
      "baseCoefficient": 1.3,
      "nextSkillIds": []
    },
    {
      "skillId": "knight_attack02_heavy",
      "effectId": "slash_mid",
      "baseCoefficient": 1.6,
      "nextSkillIds": []
    },
    {
      "skillId": "knight_attack03_fire",
      "effectId": "slash_heavy",
      "baseCoefficient": 2.3,
      "nextSkillIds": []
    },
    {
      "skillId": "knight_attack03_execute",
      "effectId": "slash_heavy",
      "baseCoefficient": 2.5,
      "nextSkillIds": []
    }
  ]
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/Resources/Data/SkillTable.json
git commit -m "feat: SkillTable에 진화 트리 데이터 추가"
```

---

## Task 3: PlayerSkillState 신규 컴포넌트

**Files:**
- Create: `Assets/Scripts/Player/PlayerSkillState.cs`

- [ ] **Step 1: PlayerSkillState.cs 작성**

슬롯 키는 `baseSkillId`(원본 ID)로 고정. 진화해도 키는 바뀌지 않는다.
`GetCurrentId`는 진화 기록이 없으면 `baseSkillId`를 그대로 반환한다.

```cs
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillState : MonoBehaviour
{
    readonly Dictionary<string, string> _current = new();

    public string GetCurrentId(string baseSkillId)
    {
        return _current.TryGetValue(baseSkillId, out var id) ? id : baseSkillId;
    }

    public void Evolve(string baseSkillId, string chosenSkillId)
    {
        _current[baseSkillId] = chosenSkillId;
        Debug.Log($"[SkillState] {baseSkillId} → {chosenSkillId}");
    }
}
```

- [ ] **Step 2: Play Mode 수동 검증 준비**

Player GameObject에 `PlayerSkillState` 컴포넌트를 추가해두어야 Task 4에서 Inspector 연결 가능.
(Unity 에디터에서: Player 선택 → Add Component → PlayerSkillState)

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/Player/PlayerSkillState.cs
git commit -m "feat: PlayerSkillState 컴포넌트 추가"
```

---

## Task 4: PlayerCombat 연동

**Files:**
- Modify: `Assets/Scripts/Player/PlayerCombat.cs`

- [ ] **Step 1: _skillState 필드 추가 및 SpawnEffects 수정**

`PlayerCombat.cs`의 기존 필드 블록에 `_skillState`를 추가하고, `SpawnEffects`에서 현재 진화 ID를 조회하도록 변경한다.

```cs
// 기존 필드 블록에 추가 (HitboxPool _pool; 아래에)
[SerializeField] PlayerSkillState _skillState;
```

`SpawnEffects` 메서드 전체를 아래로 교체한다:

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

- [ ] **Step 2: Inspector 연결**

Unity 에디터에서 Player GameObject 선택 → PlayerCombat 컴포넌트 → `Skill State` 필드에 같은 GameObject의 `PlayerSkillState` 컴포넌트를 드래그해서 연결.

- [ ] **Step 3: Play Mode로 기존 공격 동작 확인**

Play Mode 진입 → 공격 입력 → 콘솔에 에러 없음, 히트박스 정상 스폰 확인.
(PlayerSkillState가 없어도 baseSkillId 그대로 사용하므로 이전과 동일하게 동작해야 함)

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Player/PlayerCombat.cs
git commit -m "feat: PlayerCombat이 PlayerSkillState 경유해 현재 스킬 조회"
```

---

## Task 5: SkillUpgradeUI 신규

**Files:**
- Create: `Assets/Scripts/UI/SkillUpgradeUI.cs`

- [ ] **Step 1: SkillUpgradeUI.cs 작성**

카드 3장(Button)과 각 카드의 이름/계수 텍스트를 SerializeField로 받는다.
`Show()` 호출 시 Time.timeScale = 0으로 게임 일시정지. 카드 선택 시 복구.

```cs
using UnityEngine;
using UnityEngine.UI;

public class SkillUpgradeUI : MonoBehaviour
{
    public static SkillUpgradeUI Instance { get; private set; }

    [SerializeField] GameObject _panel;
    [SerializeField] Button[]   _cardButtons;    // 길이 3
    [SerializeField] Text[]     _cardNameTexts;  // 길이 3
    [SerializeField] Text[]     _cardCoeffTexts; // 길이 3

    string           _pendingBaseSkillId;
    PlayerSkillState _pendingState;

    void Awake()
    {
        Instance = this;
        _panel.SetActive(false);
    }

    public void Show(string baseSkillId, SkillDef[] options, PlayerSkillState skillState)
    {
        _pendingBaseSkillId = baseSkillId;
        _pendingState       = skillState;
        Time.timeScale      = 0f;
        _panel.SetActive(true);

        for (int i = 0; i < _cardButtons.Length; i++)
        {
            bool active = i < options.Length;
            _cardButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            var opt = options[i];
            _cardNameTexts[i].text  = opt.skillId;
            _cardCoeffTexts[i].text = $"계수 x{opt.baseCoefficient:F1}";

            int captured = i;
            _cardButtons[i].onClick.RemoveAllListeners();
            _cardButtons[i].onClick.AddListener(() => OnCardSelected(options[captured].skillId));
        }
    }

    void OnCardSelected(string chosenSkillId)
    {
        _pendingState?.Evolve(_pendingBaseSkillId, chosenSkillId);
        _panel.SetActive(false);
        Time.timeScale = 1f;
    }
}
```

- [ ] **Step 2: Unity 에디터에서 Canvas 계층 구성**

Unity 에디터 → Hierarchy → Create → UI → Canvas 생성. 이름: `SkillUpgradeUI`.
Canvas 컴포넌트: Render Mode = Screen Space - Overlay.

Canvas 오브젝트 선택 → Add Component → `SkillUpgradeUI`.

Canvas 하위에 다음 계층 생성:

```
Canvas (SkillUpgradeUI 컴포넌트)
  └── Panel               (Image, Color: 000000 투명도 180/255)
       └── CardContainer  (HorizontalLayoutGroup, Spacing 20, Child Force Expand 해제)
            ├── Card0     (Button)
            │    ├── NameText  (Text, 글씨 중앙 정렬)
            │    └── CoeffText (Text, 글씨 중앙 정렬)
            ├── Card1     (Button, Card0과 동일 구조)
            └── Card2     (Button, Card0과 동일 구조)
```

각 카드 RectTransform 크기: Width 200, Height 250.

- [ ] **Step 3: Inspector 필드 연결**

SkillUpgradeUI 컴포넌트 인스펙터:
- `Panel` → Panel 오브젝트 드래그
- `Card Buttons` → Size 3, Card0/Card1/Card2 드래그
- `Card Name Texts` → Size 3, 각 카드의 NameText 드래그
- `Card Coeff Texts` → Size 3, 각 카드의 CoeffText 드래그

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/UI/SkillUpgradeUI.cs
git commit -m "feat: SkillUpgradeUI 카드 선택 UI 추가"
```

---

## Task 6: SkillBook 신규

**Files:**
- Create: `Assets/Scripts/Combat/SkillBook.cs`

- [ ] **Step 1: SkillBook.cs 작성**

픽업 후 즉시 `Destroy(gameObject)`. 데이터는 이미 UI에 넘겼으므로 오브젝트 파괴 타이밍 무방.
Fisher-Yates 셔플로 nextSkillIds에서 최대 3개 랜덤 선택.

```cs
using System.Collections.Generic;
using UnityEngine;

public class SkillBook : MonoBehaviour
{
    [SerializeField] string _baseSkillId;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerSkillState>(out var state)) return;

        var currentId = state.GetCurrentId(_baseSkillId);
        var def       = SkillTable.Get(currentId);

        if (def == null || def.nextSkillIds == null || def.nextSkillIds.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        var pool = new List<string>(def.nextSkillIds);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var options = new System.Collections.Generic.List<SkillDef>();
        for (int i = 0; i < pool.Count && options.Count < 3; i++)
        {
            var d = SkillTable.Get(pool[i]);
            if (d != null) options.Add(d);
        }

        if (options.Count == 0) { Destroy(gameObject); return; }
        SkillUpgradeUI.Instance.Show(_baseSkillId, options.ToArray(), state);
        Destroy(gameObject);
    }
}
```

- [ ] **Step 2: 테스트용 SkillBook 오브젝트 씬에 배치**

Unity 에디터 → Hierarchy → Create Empty → 이름 `SkillBook_Test`.
- `SkillBook` 컴포넌트 추가
- `Base Skill Id` 필드: `knight_attack01`
- `CircleCollider2D` 추가, `Is Trigger` 체크, Radius: 0.5
- Transform 위치: Player 근처 빈 공간

- [ ] **Step 3: Play Mode 전체 흐름 검증**

Play Mode 진입 → Player를 SkillBook_Test 위치로 이동 → 충돌 시:
1. SkillUpgradeUI Panel이 화면에 나타남
2. 카드 1~3장에 `knight_attack01_fire`, `knight_attack01_heavy`, `knight_attack01_multi` 중 일부 표시
3. 게임이 일시정지됨 (캐릭터 입력 없음)
4. 카드 클릭 → Panel 사라짐, 게임 재개
5. 콘솔: `[SkillState] knight_attack01 → knight_attack01_fire` (선택한 것) 출력 확인
6. 공격 입력 → `SpawnEffects`가 진화된 skillId (`knight_attack01_fire`)로 조회하는지 확인
   (baseCoefficient가 1.3이므로 damage = 20 * 1.3 = 26이어야 함)

- [ ] **Step 4: 커밋**

```bash
git add Assets/Scripts/Combat/SkillBook.cs
git commit -m "feat: SkillBook 픽업 컴포넌트 추가 — 스킬 진화 UI 트리거"
```

---

## 완료 체크리스트

- [ ] Play Mode에서 SkillBook 픽업 → 카드 UI 표시 → 선택 → 진화 확인
- [ ] 진화 후 공격 데미지가 선택한 스킬의 `baseCoefficient`를 반영하는지 확인
- [ ] `nextSkillIds`가 빈 스킬(최대 진화)의 SkillBook을 픽업하면 UI 없이 Destroy되는지 확인
- [ ] Play Mode 재시작 시 진화 상태가 초기화(모두 baseSkillId로 복귀)되는지 확인
