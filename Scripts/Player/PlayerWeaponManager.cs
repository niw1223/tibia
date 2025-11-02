using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    public WeaponItem currentWeapon;
    // อาจจะมี SpriteRenderer สำหรับแสดงอาวุธในมือผู้เล่น (ตามที่คุณมีอยู่แล้ว)
    // public SpriteRenderer weaponSpriteRenderer; 

    // เพิ่มฟังก์ชัน Start เพื่อให้แน่ใจว่า Attacker และสคริปต์อื่นสามารถเข้าถึง currentWeapon ได้ตั้งแต่ต้น
    void Start()
    {
        // ตรวจสอบว่ามีอาวุธเริ่มต้นถูกกำหนดไว้ใน Inspector แล้ว
        if (currentWeapon != null)
        {
            Debug.Log("Initial weapon equipped: " + currentWeapon.weaponName);
            // อัปเดต Sprite อาวุธที่ผู้เล่นถือ (ถ้ามี)
            // if (weaponSpriteRenderer != null) weaponSpriteRenderer.sprite = currentWeapon.weaponSprite;
        }
    }

    // ฟังก์ชันสำหรับสวมใส่อาวุธใหม่ (เรียกใช้จาก UI หรือโค้ดอื่น)
    public void EquipWeapon(WeaponItem newWeapon)
    {
        if (newWeapon != null)
        {
            currentWeapon = newWeapon;
            Debug.Log("Equipped: " + newWeapon.weaponName);
            // อัปเดต Sprite อาวุธที่ผู้เล่นถือ (ถ้ามี)
            // if (weaponSpriteRenderer != null) weaponSpriteRenderer.sprite = newWeapon.weaponSprite;

            // อาจจะแจ้งเตือน Attacker.cs ว่ามีการเปลี่ยนอาวุธแล้ว (ถ้าจำเป็น)
            // GetComponent<Attacker>()?.UpdateWeapon(); 
        }
    }
}
