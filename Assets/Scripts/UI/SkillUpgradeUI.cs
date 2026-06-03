using UnityEngine;
using UnityEngine.UI;

public class SkillUpgradeUI : MonoBehaviour
{
    public static SkillUpgradeUI Instance { get; private set; }

    [SerializeField] GameObject _panel;
    [SerializeField] Button[]   _cardButtons;    // 길이 3
    [SerializeField] Text[]     _cardNameTexts;  // 길이 3
    [SerializeField] Text[]     _cardDescTexts;  // 길이 3
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
            _cardDescTexts[i].text  = opt.description;
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
