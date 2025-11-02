using UnityEngine;
using System.Collections.Generic;
using System.Linq; // เพิ่ม using System.Linq

public class CharacterStats : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public Dictionary<ElementType, float> resistances = new Dictionary<ElementType, float>();

    private GridMovementSystem playerMovement;
    private MonsterAI_GridOnly monsterAI;

    void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<GridMovementSystem>();
        monsterAI = GetComponent<MonsterAI_GridOnly>();

        if (resistances.Count == 0)
        {
            foreach (ElementType element in System.Enum.GetValues(typeof(ElementType)))
            {
                resistances.Add(element, 1f);
            }
        }
    }

    public void TakeDamage(int baseDamage, ElementType damageType, GameObject attackerObject = null)
    {
        Debug.Log(gameObject.name + " was hit! Damage type: " + damageType.ToString());
        float damageMultiplier = 1f;
        if (resistances.ContainsKey(damageType))
        {
            damageMultiplier = resistances[damageType];
        }

        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
        currentHealth -= finalDamage;

        if (playerMovement != null)
        {
            Debug.Log("Calling player's hit animation logic.");
            playerMovement.TakeDamage(damageType, attackerObject);
        }
        else if (monsterAI != null)
        {
            Debug.Log("Calling monster's hit animation logic.");
            monsterAI.TakeDamage(damageType, attackerObject);
        }

        Debug.Log(gameObject.name + " took " + finalDamage + " " + damageType.ToString() + " damage.");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died!");
    }
}
