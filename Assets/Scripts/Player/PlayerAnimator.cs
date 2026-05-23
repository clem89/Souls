using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
}
