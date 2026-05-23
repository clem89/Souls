using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] InputReader _input;
    [SerializeField] float _moveSpeed = 5f;

    Rigidbody2D _rb;
    PlayerDodge _dodge;
    Vector2 _moveInput;

    public Vector2 MoveDirection { get; private set; }

    void Awake()
    {
        Debug.Assert(_input != null, "PlayerController: InputReader not assigned");
        _rb = GetComponent<Rigidbody2D>();
        _dodge = GetComponent<PlayerDodge>();
        _input.MoveEvent += OnMove;
    }

    void OnDestroy() => _input.MoveEvent -= OnMove;

    void OnMove(Vector2 v) => _moveInput = v;

    void FixedUpdate()
    {
        MoveDirection = _moveInput.normalized;

        if (_dodge == null || !_dodge.IsDodging)
            _rb.MovePosition(_rb.position + MoveDirection * _moveSpeed * Time.fixedDeltaTime);

        if (MoveDirection.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(MoveDirection.y, MoveDirection.x) * Mathf.Rad2Deg - 90f;
            _rb.rotation = angle;
        }
    }
}
