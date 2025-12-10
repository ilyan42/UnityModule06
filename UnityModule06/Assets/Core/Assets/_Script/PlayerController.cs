using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    public float walkSpeed = 3.5f;
    public float rotationSpeed = 10f;
    
    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    
    private float cameraVerticalRotation = 0f;

    [Header("View Points")]
    public Vector3 fpsViewOffset = new Vector3(0f, 1.25f, 1f);
    public Vector3 tpsViewPointOffset = new Vector3(0f, 1.64f, -2.35f);
    private bool isFPSView = false;
    private Transform tpsOriginalParent;
    private Vector3 tpsOriginalLocalPos;
    private Quaternion tpsOriginalLocalRot;

    [Header("Camera Collision")]
    public float cameraCollisionRadius = 0.2f;
    public LayerMask collisionMask;  // Set this to "Default" or your environment layers


    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("Initial TPS View Point Offset: " + tpsViewPointOffset);

        if (cameraTransform != null)
        {
            tpsOriginalParent = cameraTransform.parent;
            tpsOriginalLocalPos = cameraTransform.localPosition;
            tpsOriginalLocalRot = cameraTransform.localRotation;
        }

    }

    void Update()
    {
        HandleCameraRotation();
        HandleMovement();
        HandleCameraCollision();
    }
    
    void HandleCameraRotation()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        
        Vector2 mouseDelta = mouse.delta.ReadValue();
        float horizontalRotation = mouseDelta.x * mouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);
        
        cameraVerticalRotation -= mouseDelta.y * mouseSensitivity;
        cameraVerticalRotation = Mathf.Clamp(cameraVerticalRotation, minVerticalAngle, maxVerticalAngle);
        
        if (cameraTransform != null)
        {
            cameraTransform.localEulerAngles = new Vector3(cameraVerticalRotation, 0, 0);
        }
        
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    void HandleMovement()
    {
        var keyboard = Keyboard.current;
        Vector3 input = Vector3.zero;
        
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input += transform.forward;
            if (keyboard.cKey.wasPressedThisFrame && !isFPSView)
            {
                Debug.Log("Switching to FPS view");
                isFPSView = true;
                HandleView_FPS();
            }
            else if (keyboard.cKey.wasPressedThisFrame && isFPSView)
            {
                Debug.Log("Switching to TPS view");
                isFPSView = false;
                HandleView_TPS();
            }
            // debug 
            if (keyboard.rKey.wasPressedThisFrame)
            {
                Debug.Log("Picked up a key!");

                // Find all doors in the scene
                OpenDoor[] doors = FindObjectsOfType<OpenDoor>();
                
                foreach (OpenDoor door in doors)
                {
                    // You can add a public method in OpenDoor to handle key pickup
                    door.AddKey();
                    Debug.Log("Key added to door!");
                }
            }
        }

        if (input.sqrMagnitude > 0.01f)
        {
            input = input.normalized;
            transform.Translate(input * walkSpeed * Time.deltaTime, Space.World);
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
    }

    void HandleView_FPS()
    {
        if (cameraTransform == null) return;
        isFPSView = true;
        cameraTransform.SetParent(transform);
        cameraTransform.localPosition = fpsViewOffset;
        cameraTransform.localRotation = Quaternion.identity;
    }

    void HandleView_TPS()
    {
        if (cameraTransform == null) return;
        isFPSView = false;
        cameraTransform.SetParent(tpsOriginalParent, false);
        cameraTransform.localPosition = tpsOriginalLocalPos;
        cameraTransform.localRotation = tpsOriginalLocalRot;
    }

    void HandleCameraCollision()
    {
        if (cameraTransform == null || isFPSView) return; // No collision in FPS mode

        Vector3 targetPosition = tpsOriginalParent.TransformPoint(tpsViewPointOffset);
        Vector3 direction = targetPosition - transform.position;
        
        if (Physics.SphereCast(transform.position, cameraCollisionRadius, direction.normalized, 
            out RaycastHit hit, tpsViewPointOffset.magnitude, collisionMask))
        {
            // Move camera slightly away from wall
            cameraTransform.position = hit.point + hit.normal * cameraCollisionRadius;
        }
        else
        {
            cameraTransform.position = targetPosition;
        }
    }


}