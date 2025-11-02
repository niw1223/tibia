using UnityEngine;

public class Attacker : MonoBehaviour
{
    public float attackCooldown = 1f;
    private float nextAttackTime = 0f;
    public float attackRange = 1.1f;
    public LayerMask targetLayer;

    private PlayerWeaponManager weaponManager;

    void Start()
    {
        weaponManager = GetComponent<PlayerWeaponManager>();
    }

    public void PerformAttack(CharacterStats target)
    {
        if (Time.time >= nextAttackTime)
        {
            if (weaponManager != null && weaponManager.currentWeapon != null)
            {
                int damage = Mathf.RoundToInt(weaponManager.currentWeapon.damage);
                ElementType element = weaponManager.currentWeapon.elementType;
                target.TakeDamage(damage, element, this.gameObject);
                nextAttackTime = Time.time + attackCooldown;
            }
            // Logic มอนสเตอร์ (ถ้ามี)
        }
    }

    public void CheckForTargetsAndAttack()
    {
        if (Time.time >= nextAttackTime)
        {
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(transform.position, attackRange, targetLayer);
            foreach (Collider2D targetCollider in hitTargets)
            {
                CharacterStats targetStats = targetCollider.GetComponent<CharacterStats>();
                if (targetStats != null)
                {
                    PerformAttack(targetStats);
                    break;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
