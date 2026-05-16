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
