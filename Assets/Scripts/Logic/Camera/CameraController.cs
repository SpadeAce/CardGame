using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviourEx
{
    #region Link
    [SerializeField] private Camera _camera;
    #endregion Link

    [Header("Pan")]
    [SerializeField] private float _panSpeed = 0.01f;

    [Header("Rotation")]
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _initialYaw = 45f;

    [Header("Zoom")]
    [SerializeField] private float _minDistance = 3f;
    [SerializeField] private float _maxDistance = 30f;
    [SerializeField] private float _zoomSpeed = 2f;

    [Header("Camera Angle")]
    [SerializeField] private float _elevation = 45f;

    private Vector3 _pivot = Vector3.zero;
    private float _distance = 10f;
    private float _targetYaw;
    private float _currentYaw;

    private bool _isDragging;
    private Vector2 _dragStartScreenPos;
    private Vector3 _pivotAtDragStart;

    private void Awake()
    {
        _targetYaw = _initialYaw;
        _currentYaw = _initialYaw;
    }

    private void Update()
    {
        HandlePan();
        HandleRotation();
        HandleZoom();
        ApplyCameraTransform();
    }

    private void HandlePan()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            _isDragging = true;
            _dragStartScreenPos = Mouse.current.position.ReadValue();
            _pivotAtDragStart = _pivot;
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            _isDragging = false;
        }

        if (_isDragging && Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.position.ReadValue() - _dragStartScreenPos;
            Quaternion yRot = Quaternion.Euler(0, _currentYaw, 0);
            float speedFactor = _distance * _panSpeed;
            _pivot = _pivotAtDragStart
                   - yRot * Vector3.right * delta.x * speedFactor
                   - yRot * Vector3.forward * delta.y * speedFactor;
        }
    }

    private void HandleRotation()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
            _targetYaw += 90f;
        if (Keyboard.current.eKey.wasPressedThisFrame)
            _targetYaw -= 90f;

        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * _rotationSpeed);
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _distance = Mathf.Clamp(_distance - scroll * _zoomSpeed, _minDistance, _maxDistance);
        }
    }

    public void SetPivot(Vector3 pivot)
    {
        _pivot = pivot;
        _pivotAtDragStart = pivot;
    }

    private void ApplyCameraTransform()
    {
        transform.SetPositionAndRotation(_pivot, Quaternion.Euler(0, _currentYaw, 0));

        float sinEl = Mathf.Sin(_elevation * Mathf.Deg2Rad);
        float cosEl = Mathf.Cos(_elevation * Mathf.Deg2Rad);
        _camera.transform.SetLocalPositionAndRotation(
            new Vector3(0, _distance * sinEl, -_distance * cosEl),
            Quaternion.Euler(_elevation, 0, 0));
    }
}
