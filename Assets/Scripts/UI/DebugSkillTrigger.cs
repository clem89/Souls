using UnityEngine;
using UnityEngine.InputSystem;

public class DebugSkillTrigger : MonoBehaviour
{
    [SerializeField] LevelSystem _levelSystem;

    void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
            _levelSystem.AddXp(_levelSystem.XpToNext);
    }
}
