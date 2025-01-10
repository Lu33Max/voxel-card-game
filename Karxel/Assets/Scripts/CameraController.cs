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

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
        ClampPosition();
    }

    private void UpdateFocusPoint()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Prüfe, ob der Raycast einen Punkt auf dem Spielbrett trifft
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, focusLayer))
        {
            _focusPoint = hit.point;
        }
        else
        {
            // Berechne den Schnittpunkt des Ray mit der Ebene y = 0
            float rayDirectionY = ray.direction.y;
            if (Mathf.Abs(rayDirectionY) > Mathf.Epsilon) // Sicherstellen, dass keine Division durch 0 passiert
            {
                float t = -ray.origin.y / rayDirectionY; // Parameter t für den Schnittpunkt
                _focusPoint = ray.origin + ray.direction * t; // Schnittpunkt berechnen
            }
            else
            {
                // Falls der Ray parallel zur Ebene ist (extrem unwahrscheinlich), Standardwert nehmen
                _focusPoint = ray.origin + ray.direction * 2f; // Beliebige Default-Distanz
                _focusPoint.y = 0;
            }
        }
    }

    private void HandleMovement()
    {
        // Bewegung auf der Ebene
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        if (horizontal == 0f && vertical == 0f)
            return;
        
        var forwardVec = transform.forward;
        var rightVec = transform.right;

        Vector3 direction = forwardVec * vertical + rightVec * horizontal;
        direction = new Vector3(direction.x, 0, direction.z);
        
        transform.position += direction.normalized * (moveSpeed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        // Rotation um den Fokuspunkt
        if (Input.GetKey(KeyCode.Q))
            RotateAroundFocus(1);
        else if (Input.GetKey(KeyCode.E))
            RotateAroundFocus(-1);
    }

    private void RotateAroundFocus(float direction)
    {
        UpdateFocusPoint();
        
        Vector3 directionToFocus = transform.position - _focusPoint;
        Quaternion rotation = Quaternion.AngleAxis(direction * rotationSpeed * Time.deltaTime, Vector3.up);
        directionToFocus = rotation * directionToFocus;

        transform.position = _focusPoint + directionToFocus;
        transform.LookAt(_focusPoint);
    }

    private void HandleZoom()
    {
        // Zoom via Mouse Wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            Vector3 direction = transform.forward * (scroll * zoomSpeed);
            Vector3 newPosition = transform.position + direction;

            var yPosition = newPosition.y;
            
            // Cap maximum/minimum zoom
            if (yPosition >= minZoom && yPosition <= maxZoom)
                transform.position = newPosition;
        }
    }

    private void ClampPosition()
    {
        var pos = transform.position;
        
        transform.position = new Vector3(
            Mathf.Clamp(pos.x, minPosition.x, maxPosition.x), Mathf.Clamp(pos.y, minPosition.y, maxPosition.y), 
            Math.Clamp(pos.z, minPosition.z, maxPosition.z)
        );
    }
}
