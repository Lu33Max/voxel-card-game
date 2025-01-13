using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotationDesign : MonoBehaviour
{
    [SerializeField]
    float rotationSpeed = 8f;

    void Update()
    {
        // Drehung um die globale Y-Achse
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
    }
}