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
        Debug.Assert(_hpSlider != null, "CombatHUD: HP Slider not assigned");
        Debug.Assert(_staminaSlider != null, "CombatHUD: Stamina Slider not assigned");
        Debug.Assert(_stamina != null, "CombatHUD: StaminaController not assigned");
        Debug.Assert(_playerHealth != null, "CombatHUD: PlayerHealth not assigned");

        _playerHealth.OnHpChanged += OnHpChanged;
        _stamina.Stamina.OnChanged += OnStaminaChanged;
    }

    void OnDestroy()
    {
        if (_playerHealth != null) _playerHealth.OnHpChanged -= OnHpChanged;
        if (_stamina != null) _stamina.Stamina.OnChanged -= OnStaminaChanged;
    }

    void Start()
    {
        _hpSlider.value = 1f;
        _staminaSlider.value = 1f;
    }

    void OnHpChanged(float normalizedHp) => _hpSlider.value = normalizedHp;
    void OnStaminaChanged(float current) => _staminaSlider.value = current / _stamina.Stamina.Max;
}
