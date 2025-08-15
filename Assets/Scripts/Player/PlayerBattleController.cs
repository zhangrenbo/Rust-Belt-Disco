using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MovementController))]
[RequireComponent(typeof(CombatController))]
public class PlayerBattleController : MonoBehaviour
{
    [Header("=== Dodge Settings ===")]
    public float dodgeDistance = 3f;
    public float dodgeDuration = 0.3f;

    [Header("=== Auto Target Settings ===")]
    public float cameraTurnSpeed = 5f;
    public string enemyTag = "Enemy";

    private MovementController movement;
    private CombatController combat;
    private CameraSystem cameraSystem;

    private bool isDodging = false;
    private bool isBlocking = false;
    private bool autoTargetActive = false;
    private Transform targetEnemy;

    void Awake()
    {
        movement = GetComponent<MovementController>();
        combat = GetComponent<CombatController>();
        if (Camera.main != null)
            cameraSystem = Camera.main.GetComponent<CameraSystem>();
    }

    void Update()
    {
        HandleInput();
        if (autoTargetActive)
        {
            UpdateAutoTarget();
        }
    }

    void HandleInput()
    {
        if (!combat.enablePlayerInput || combat.characterType != CharacterType.Player)
            return;

        if (Input.GetMouseButtonDown(0) && combat.CanAttack)
        {
            combat.PerformAttack();
            AutoTarget();
        }

        if (Input.GetMouseButtonDown(1))
            StartBlock();
        if (Input.GetMouseButtonUp(1))
            EndBlock();

        if (Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(DoDodge());

        if (Input.GetKeyDown(KeyCode.Alpha1)) combat.CastSkill(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) combat.CastSkill(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) combat.CastSkill(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) combat.CastSkill(3);
    }

    void StartBlock()
    {
        isBlocking = true;
        // Additional block logic could be added here
    }

    void EndBlock()
    {
        isBlocking = false;
    }

    IEnumerator DoDodge()
    {
        if (isDodging) yield break;
        isDodging = true;
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * dodgeDistance;
        float timer = 0f;
        while (timer < dodgeDuration)
        {
            float t = timer / dodgeDuration;
            Vector3 pos = Vector3.Lerp(start, end, t);
            movement.ForceStopMovement();
            transform.position = pos;
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = end;
        isDodging = false;
    }

    void AutoTarget()
    {
        targetEnemy = FindNearestEnemy();
        if (targetEnemy == null || cameraSystem == null)
            return;
        autoTargetActive = true;
        cameraSystem.enableMouseRotation = false;
    }

    void UpdateAutoTarget()
    {
        if (Input.GetMouseButton(2))
        {
            CancelAutoTarget();
            return;
        }
        if (targetEnemy == null)
        {
            CancelAutoTarget();
            return;
        }
        cameraSystem.RotateTowards(targetEnemy.position, cameraTurnSpeed);
        Vector3 screenPos = Camera.main.WorldToViewportPoint(targetEnemy.position);
        if (Mathf.Abs(screenPos.x - 0.5f) < 0.05f && screenPos.y > 0.9f)
        {
            CancelAutoTarget();
        }
    }

    void CancelAutoTarget()
    {
        autoTargetActive = false;
        if (cameraSystem != null)
            cameraSystem.enableMouseRotation = true;
    }

    Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = e.transform;
            }
        }
        return nearest;
    }
}