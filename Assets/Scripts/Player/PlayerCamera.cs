using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Transform _target;
    [SerializeField] float _height = 12f;
    [SerializeField] float _smoothSpeed = 8f;

    public Transform LockOnTarget { get; set; }

    void LateUpdate()
    {
        if (_target == null) return;
        var desired = new Vector3(_target.position.x, _height, _target.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
    }
}
