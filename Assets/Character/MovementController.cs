using UnityEngine;

/// <summary>
/// НЁУГТЖ¶ЇїШЦЖЖч - ґ¦АнWASDТЖ¶ЇЎўЕЬІЅЎўПа»ъёъЛжЈЁїЙУГУЪНжјТЎўNPCµИИОєОЅЗЙ«Ј©
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
    [Header("=== ТЖ¶ЇЙиЦГ ===")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;

    [Header("=== Па»ъіЇПтїШЦЖ ===")]
    public bool faceCamera = true;
    public Transform cameraTransform;

    [Header("=== µчКФЙиЦГ ===")]
    public bool showDebugInfo = false;

    // ЧйјюТэУГ
    private Rigidbody rb;
    private StateController stateController;
    private CharacterStateManager characterStateManager;

    // ТЖ¶ЇЧґМ¬
    private Vector3 lastMoveDirection = Vector3.forward;
    private bool isRunning = false;
    private float currentSpeed;

    // КВјю
    public System.Action<Vector3> OnMoveDirectionChanged;
    public System.Action<bool> OnRunningStateChanged;
    public System.Action<float> OnSpeedChanged;

    // КфРФ
    public bool IsMoving => rb.velocity.magnitude > 0.1f;
    public bool IsRunning => isRunning;
    public float CurrentSpeed => currentSpeed;
    public Vector3 LastMoveDirection => lastMoveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        stateController = GetComponent<StateController>();
        characterStateManager = GetComponent<CharacterStateManager>();

        currentSpeed = moveSpeed;
    }

    void Start()
    {
        SetupCameraFollow();
    }

    void Update()
    {
        // Ц»ФЪ·З¶Ф»°ЧґМ¬ПВґ¦АнТЖ¶ЇКдИл - 修正：移除括号
        if (stateController != null && !stateController.IsInDialogue)
        {
            HandleMovementInput();
            HandleRunInput();
        }
    }

    void FixedUpdate()
    {
        if (stateController != null && stateController.CanMove())
        {
            ApplyMovement();
        }
    }

    void LateUpdate()
    {
        HandleCameraFacing();
    }

    void SetupCameraFollow()
    {
        if (faceCamera && cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
        {
            CameraSystem cameraSystem = cameraTransform.GetComponent<CameraSystem>();
            if (cameraSystem == null)
            {
                cameraSystem = cameraTransform.gameObject.AddComponent<CameraSystem>();
            }
            cameraSystem.SetFollowTarget(this.transform);
        }
    }

    void HandleMovementInput()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        if (characterStateManager != null && characterStateManager.CanMove())
        {
            Vector3 dir = CalculateMovementDirection(moveX, moveZ);

            // ёьРВЅЗЙ«ЧґМ¬№ЬАнЖчµДТЖ¶Ї
            Vector2 moveDirection2D = new Vector2(dir.x, dir.z);
            characterStateManager.SetMovement(moveDirection2D, currentSpeed * moveDirection2D.magnitude);

            if (dir != Vector3.zero)
            {
                // ёьРВЧоєуТЖ¶Ї·ЅПт
                if (lastMoveDirection != dir)
                {
                    lastMoveDirection = dir;
                    OnMoveDirectionChanged?.Invoke(lastMoveDirection);
                }

                // ґ¦АнЧуУТ·­ЧЄ
                HandleCharacterFlipping(moveX);
            }
        }
    }

    Vector3 CalculateMovementDirection(float moveX, float moveZ)
    {
        Vector3 dir = Vector3.zero;

        if (cameraTransform != null)
        {
            // »щУЪПа»ъ·ЅПтµДПа¶ФТЖ¶Ї
            dir = cameraTransform.forward * moveZ + cameraTransform.right * moveX;
            dir.y = 0;
            dir.Normalize();
        }
        else
        {
            // КАЅзЧш±кТЖ¶Ї
            dir = new Vector3(moveX, 0, moveZ);
        }

        return dir;
    }

    void HandleCharacterFlipping(float moveX)
    {
        // ёщѕЭТЖ¶Ї·ЅПт·­ЧЄЅЗЙ«
        Vector3 ls = transform.localScale;
        if (moveX > 0)
            ls.x = -Mathf.Abs(ls.x);
        else if (moveX < 0)
            ls.x = Mathf.Abs(ls.x);
        transform.localScale = ls;
    }

    void HandleRunInput()
    {
        bool hasMoveInput = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        bool shouldRun = hasMoveInput && Input.GetKey(KeyCode.LeftShift);

        if (shouldRun != isRunning)
        {
            isRunning = shouldRun;
            currentSpeed = isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;

            OnRunningStateChanged?.Invoke(isRunning);
            OnSpeedChanged?.Invoke(currentSpeed);

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerMovement] ЕЬІЅЧґМ¬: {isRunning}, µ±З°ЛЩ¶И: {currentSpeed}");
            }
        }
    }

    void ApplyMovement()
    {
        if (characterStateManager == null) return;

        Vector2 moveDir = characterStateManager.movementDirection;
        if (moveDir.magnitude > 0.1f)
        {
            Vector3 moveDir3D = new Vector3(moveDir.x, 0, moveDir.y);
            rb.MovePosition(rb.position + moveDir3D * characterStateManager.movementSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleCameraFacing()
    {
        if (faceCamera && cameraTransform != null)
        {
            Vector3 dir = cameraTransform.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    // ========== №«№ІЅУїЪ ==========

    /// <summary>
    /// ЙиЦГТЖ¶ЇЛЩ¶И
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        if (!isRunning)
        {
            currentSpeed = moveSpeed;
            OnSpeedChanged?.Invoke(currentSpeed);
        }
    }

    /// <summary>
    /// ЙиЦГЕЬІЅ±¶Кэ
    /// </summary>
    public void SetRunSpeedMultiplier(float multiplier)
    {
        runSpeedMultiplier = multiplier;
        if (isRunning)
        {
            currentSpeed = moveSpeed * runSpeedMultiplier;
            OnSpeedChanged?.Invoke(currentSpeed);
        }
    }

    /// <summary>
    /// ЗїЦЖНЈЦ№ТЖ¶Ї
    /// </summary>
    public void ForceStopMovement()
    {
        if (characterStateManager != null)
        {
            characterStateManager.SetMovement(Vector2.zero);
        }
        rb.velocity = Vector3.zero;
    }

    /// <summary>
    /// ЙиЦГПа»ъёъЛжДї±к
    /// </summary>
    public void SetCameraTarget(Transform camera)
    {
        cameraTransform = camera;
        SetupCameraFollow();
    }

    /// <summary>
    /// ЖфУГ/ЅыУГПа»ъіЇПт
    /// </summary>
    public void SetFaceCameraEnabled(bool enabled)
    {
        faceCamera = enabled;
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // ПФКѕТЖ¶Ї·ЅПт
        if (lastMoveDirection != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, lastMoveDirection * 2f);
        }

        // ПФКѕЛЩ¶ИЧґМ¬
        Gizmos.color = isRunning ? Color.red : Color.blue;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
    }
}