using UnityEngine;

/// <summary>
/// Handles player combat input and delegates actions to CombatController.
/// </summary>
[RequireComponent(typeof(CombatController))]
public class CombatInputHandler : MonoBehaviour
{
    private CombatController combat;
    private StateController stateController;

    void Awake()
    {
        combat = GetComponent<CombatController>();
        stateController = GetComponent<StateController>();
    }

    void Update()
    {
        if (combat == null || !combat.enablePlayerInput || combat.characterType != CharacterType.Player)
            return;

        if (stateController != null && stateController.IsInDialogue)
            return;

        if (Input.GetMouseButtonDown(0) && combat.CanAttack)
            combat.PerformAttack();

        if (Input.GetKeyDown(KeyCode.Alpha1)) combat.CastSkill(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) combat.CastSkill(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) combat.CastSkill(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) combat.CastSkill(3);
    }
}
