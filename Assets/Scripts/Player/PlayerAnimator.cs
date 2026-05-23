using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    PlayerController _controller;
    SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (_spriteRenderer == null || _controller.MoveDirection.sqrMagnitude < 0.01f) return;

        if (_controller.MoveDirection.x < -0.01f)
            _spriteRenderer.flipX = true;
        else if (_controller.MoveDirection.x > 0.01f)
            _spriteRenderer.flipX = false;
    }
}
