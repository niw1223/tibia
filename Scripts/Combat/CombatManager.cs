using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public WeaponItem equippedWeapon; // อาวุธที่สวมใส่ในปัจจุบัน
    private Animator anim;
    private AudioSource audioSource;
    // ... อาจมี AudioClips สำหรับเสียงธาตุต่างๆ (เหมือนคำตอบที่แล้ว)

    void Start()
    {
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    // ฟังก์ชันนี้จะถูกเรียกเมื่อผู้เล่นกดโจมตี
    public void Attack()
    {
        if (equippedWeapon != null)
        {
            Debug.Log("Attacking with " + equippedWeapon.weaponName + " (" + equippedWeapon.elementType + " damage)");
            // สั่งเล่นแอนิเมชันโจมตีตามปกติ
            anim.SetTrigger("Attack");

            // ***เราจะใช้ Animation Event เพื่อเรียกใช้เอฟเฟกต์ที่นี่ในจังหวะที่เหมาะสม***
            // ซึ่งจะทำในขั้นตอนที่ 4
        }
        else
        {
            Debug.Log("No weapon equipped!");
        }
    }

    // ฟังก์ชันสำหรับสวมใส่อาวุธใหม่ (อาจถูกเรียกจาก Inventory UI)
    public void EquipWeapon(WeaponItem newWeapon)
    {
        equippedWeapon = newWeapon;
        Debug.Log("Equipped: " + newWeapon.weaponName);
        // อาจจะต้องเปลี่ยน sprite อาวุธที่ถือ หรือเปลี่ยน animation controller ที่นี่
    }
}
