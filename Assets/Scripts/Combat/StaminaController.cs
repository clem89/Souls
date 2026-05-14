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
