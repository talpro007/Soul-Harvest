using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAbility : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerMeter playerMeter;
    [SerializeField] private Pumpkin pumpkinPrefab;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0.8f, 0f);
    [SerializeField] private int pumpkinCost = 1;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private int healCost = 1;
    [SerializeField] private int healAmount = 1;

    public void OnAbility(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (playerMovement.state == PlayerState.Death) return;
        if (!playerMeter.TrySpend(pumpkinCost)) return;

        int dir = playerMovement.direction >= 0 ? 1 : -1; // 0 (not moved yet) counts as right

        Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x * dir, spawnOffset.y, 0f);
        Pumpkin pumpkin = Instantiate(pumpkinPrefab, spawnPos, Quaternion.identity);
        pumpkin.Launch(dir);
    }

    public void OnHeal(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (playerMovement.state == PlayerState.Death) return;
        if (!playerHealth.CanHeal) return;
        if (!playerMeter.TrySpend(healCost)) return;

        playerHealth.Heal(healAmount);
    }
}
