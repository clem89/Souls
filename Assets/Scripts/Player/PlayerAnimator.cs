using UnityEngine;

// 스프라이트 없는 프로토타입용 — 상태별 색상 피드백
public class PlayerAnimator : MonoBehaviour
{
    static readonly Color ColorIdle        = Color.white;
    static readonly Color ColorMove        = new Color(0.6f, 1f, 0.6f);   // 연두
    static readonly Color ColorDodge       = new Color(0.4f, 0.8f, 1f);   // 하늘
    static readonly Color ColorIFrame      = new Color(0f,   0.8f, 1f);   // 청록 (무적)
    static readonly Color ColorAttack      = new Color(1f,   0.5f, 0.1f); // 주황
    static readonly Color ColorParry       = new Color(1f,   1f,   0.2f); // 노랑
    static readonly Color ColorRiposte     = new Color(0.8f, 0.2f, 1f);   // 보라
    static readonly Color ColorDead        = new Color(0.3f, 0.3f, 0.3f); // 회색

    SpriteRenderer _sr;
    PlayerController _controller;
    PlayerCombat _combat;
    PlayerDodge _dodge;
    PlayerHealth _health;

    void Awake()
    {
        _sr         = GetComponentInChildren<SpriteRenderer>();
        _controller = GetComponent<PlayerController>();
        _combat     = GetComponent<PlayerCombat>();
        _dodge      = GetComponent<PlayerDodge>();
        _health     = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (_sr == null) return;
        _sr.color = ResolveColor();
    }

    Color ResolveColor()
    {
        if (_health != null && _health.CurrentHp <= 0f)   return ColorDead;
        if (_combat != null && _combat.RiposteReady)       return ColorRiposte;
        if (_combat != null && _combat.IsAttacking)        return ColorAttack;
        if (_combat != null && _combat.IsParrying)         return ColorParry;
        if (_dodge  != null && _dodge.IsDodging)
            return _dodge.IsInvincible ? ColorIFrame : ColorDodge;
        if (_controller != null && _controller.MoveDirection.sqrMagnitude > 0.01f)
            return ColorMove;
        return ColorIdle;
    }
}
