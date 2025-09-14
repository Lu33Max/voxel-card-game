using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    private PlayerControls? _controls;
    
    public event Action<Vector2>? OnMove;
    public event Action<float>? OnRotate;
    public event Action<float>? OnZoom;
    public event Action? OnInteract; 

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        _controls = new PlayerControls();
    }

    private void OnEnable()
    {
        _controls!.Enable();

        _controls.Player.Move.performed += OnMovePerformed;
        _controls.Player.Move.canceled  += OnMoveCanceled;

        _controls.Player.Rotate.performed += OnRotatePerformed;
        _controls.Player.Rotate.canceled += OnRotateCanceled;
        
        _controls.Player.Zoom.performed += OnZoomPerformed;
        _controls.Player.Zoom.performed += OnZoomCanceled;
        
        _controls.Player.Interact.performed += OnInteractPerformed;
    }
    
    private void OnDisable()
    {
        if(_controls == null) return;
        
        _controls.Player.Move.performed -= OnMovePerformed;
        _controls.Player.Move.canceled  -= OnMoveCanceled;
        
        _controls.Player.Rotate.performed -= OnRotatePerformed;
        _controls.Player.Rotate.performed -= OnRotateCanceled;
        
        _controls.Player.Zoom.performed -= OnZoomPerformed;
        _controls.Player.Zoom.performed -= OnZoomCanceled;
        
        _controls.Player.Interact.performed -= OnInteractPerformed;
        
        _controls.Disable();
    }
    
    private void OnMovePerformed(InputAction.CallbackContext ctx) 
        => OnMove?.Invoke(ctx.ReadValue<Vector2>());
    private void OnMoveCanceled(InputAction.CallbackContext _) 
        => OnMove?.Invoke(Vector2.zero);
    
    private void OnRotatePerformed(InputAction.CallbackContext ctx) 
        => OnRotate?.Invoke(ctx.ReadValue<float>());
    private void OnRotateCanceled(InputAction.CallbackContext _) 
        => OnRotate?.Invoke(0f);
    
    private void OnZoomPerformed(InputAction.CallbackContext ctx) 
        => OnZoom?.Invoke(Mathf.Clamp(ctx.ReadValue<float>(), -1f, 1f));
    private void OnZoomCanceled(InputAction.CallbackContext _) 
        => OnZoom?.Invoke(0f);
    
    private void OnInteractPerformed(InputAction.CallbackContext _) 
        => OnInteract?.Invoke();
}
