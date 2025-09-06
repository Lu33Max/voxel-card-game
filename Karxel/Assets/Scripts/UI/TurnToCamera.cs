using UnityEngine;

/// <summary> Helper script for 2D world elements to always face the camera </summary>
public class TurnToCamera : MonoBehaviour
{
    private Camera? _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if(_camera != null)
            transform.LookAt(_camera.transform);
    }
}
