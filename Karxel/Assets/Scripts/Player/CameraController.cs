using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;

    [SerializeField] private Vector3 minPosition;
    [SerializeField] private Vector3 maxPosition;
    
    private Vector3 _focusPoint;
    private bool _disabled;

    private Vector2 _moveInput = Vector2.zero;
    private float _zoomInput;
    private float _rotateInput;

    private float _scaleFactor = 1f;

    private void OnEnable()
    {
        InputManager.Instance.OnMove += OnMovePressed;
        InputManager.Instance.OnRotate += OnRotatePressed;
        InputManager.Instance.OnZoom += HandleZoom;

        _scaleFactor = GridManager.Instance.TileSize.x;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnMove -= OnMovePressed;
        InputManager.Instance.OnRotate -= OnRotatePressed;
        InputManager.Instance.OnZoom -= HandleZoom;
    }

    private void OnMovePressed(Vector2 dir) => _moveInput = dir;
    private void OnRotatePressed(float val) => _rotateInput = val;

    public void DisableMovement(bool shouldDisable)
    {
        _disabled = shouldDisable;
    }

    private void FixedUpdate()
    {
        if(_disabled) return;
        
        var newPosition = HandleMovement();
        var newFocusPoint = CalculateNewFocusPoint(newPosition);
        ClampPosition(newPosition, newFocusPoint);
        
        RotateAroundFocus();
    }

    private Vector3 CalculateNewFocusPoint(Vector3 potentialNewPosition)
    {
        var ray = new Ray(potentialNewPosition, transform.forward);
        var rayDirectionY = ray.direction.y;
        
        // Check that the camera isn't oriented parallel to the ground
        if (Mathf.Abs(rayDirectionY) > Mathf.Epsilon) 
        {
            var t = -ray.origin.y / rayDirectionY;
            return ray.origin + ray.direction * t;
        }

        // Default point in case the ground was never intersected
        var defaultPoint = ray.origin + ray.direction * 2f;
        defaultPoint.y = 0;

        return defaultPoint;
    }

    private Vector3 HandleMovement()
    {
        var cameraTransform = transform;
        
        if (_moveInput == Vector2.zero)
            return cameraTransform.position;
        
        var forwardVec = cameraTransform.forward;
        var rightVec = cameraTransform.right;

        var direction = forwardVec * _moveInput.y + rightVec * _moveInput.x;
        direction = new Vector3(direction.x, 0, direction.z);
        
        return cameraTransform.position + direction.normalized * (moveSpeed * Time.deltaTime * _scaleFactor);
    }

    private void RotateAroundFocus()
    {
        if(_rotateInput == 0) return;
        
        var directionToFocus = transform.position - _focusPoint;
        var rotation = Quaternion.AngleAxis(_rotateInput * rotationSpeed * Time.deltaTime, Vector3.up);
        directionToFocus = rotation * directionToFocus;

        transform.position = _focusPoint + directionToFocus;
        transform.LookAt(_focusPoint);
    }

    private void HandleZoom(float scroll)
    {
        if (scroll == 0f) return;
        
        var cameraTransform = transform;
        
        var direction = cameraTransform.forward * (scroll * zoomSpeed);
        var newPosition = cameraTransform.position + direction * _scaleFactor;

        var yPosition = newPosition.y;
            
        // Cap maximum/minimum zoom
        if (yPosition >= minZoom && yPosition <= maxZoom)
            transform.position = newPosition;
    }

    private void ClampPosition(Vector3 newPosition, Vector3 newFocusPoint)
    {
        // If the newly calculated focus point is within the boundaries, all movements can be applied
        if (newFocusPoint.x >= minPosition.x && newFocusPoint.x <= maxPosition.x && newFocusPoint.z >= minPosition.z &&
            newFocusPoint.z <= maxPosition.z)
        {
            _focusPoint = newFocusPoint;
            transform.position = newPosition;
            return;
        }
        
        var cameraTransform = transform;
        var forward = cameraTransform.forward;

        _focusPoint = new Vector3(
            Mathf.Clamp(newFocusPoint.x, minPosition.x, maxPosition.x),
            newFocusPoint.y,
            Math.Clamp(newFocusPoint.z, minPosition.z, maxPosition.z));

        var relativeHeight = transform.position.y - _focusPoint.y;
        cameraTransform.position = _focusPoint + -forward * (relativeHeight / -forward.y);
    }
}
