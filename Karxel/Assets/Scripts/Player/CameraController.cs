using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private LayerMask focusLayer;

    [SerializeField] private Vector3 minPosition;
    [SerializeField] private Vector3 maxPosition;
    
    private Vector3 _focusPoint;
    private bool _disabled;

    private void Update()
    {
        if(_disabled) return;
        
        var newPosition = HandleMovement();
        var newFocusPoint = CalculateNewFocusPoint(newPosition);
        
        ClampPosition(newPosition, newFocusPoint);
        HandleRotation();
        HandleZoom();
    }

    public void DisableMovement(bool shouldDisable)
    {
        _disabled = shouldDisable;
    }

    private Vector3 CalculateNewFocusPoint(Vector3 potentialNewPosition)
    {
        var ray = new Ray(potentialNewPosition, transform.forward);

        // Draw a line from the camera to the ground and check, if it collides with the map
        // if (Physics.Raycast(ray, out var hit, Mathf.Infinity, focusLayer))
        //     return hit.point;
        
        // If no collision with the ground was made, instead look for an interception with the plane y = 0
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
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        
        if (horizontal == 0f && vertical == 0f)
            return cameraTransform.position;
        
        var forwardVec = cameraTransform.forward;
        var rightVec = cameraTransform.right;

        var direction = forwardVec * vertical + rightVec * horizontal;
        direction = new Vector3(direction.x, 0, direction.z);
        
        return cameraTransform.position + direction.normalized * (moveSpeed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
            RotateAroundFocus(1);
        else if (Input.GetKey(KeyCode.E))
            RotateAroundFocus(-1);
    }

    private void RotateAroundFocus(float direction)
    {
        var directionToFocus = transform.position - _focusPoint;
        var rotation = Quaternion.AngleAxis(direction * rotationSpeed * Time.deltaTime, Vector3.up);
        directionToFocus = rotation * directionToFocus;

        transform.position = _focusPoint + directionToFocus;
        transform.LookAt(_focusPoint);
    }

    private void HandleZoom()
    {
        // Zoom via Mouse Wheel
        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;
        
        var direction = transform.forward * (scroll * zoomSpeed);
        var newPosition = transform.position + direction;

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

        _focusPoint = new Vector3(
            Mathf.Clamp(newFocusPoint.x, minPosition.x, maxPosition.x),
            newFocusPoint.y,
            Math.Clamp(newFocusPoint.z, minPosition.z, maxPosition.z));

        var relativeHeight = transform.position.y - _focusPoint.y;
        transform.position = _focusPoint + (-transform.forward * (relativeHeight / -transform.forward.y));
    }
}
