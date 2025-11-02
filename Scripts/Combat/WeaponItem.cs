using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Item/Weapon")]
public class WeaponItem : ScriptableObject
{
    public string weaponName;
    public ElementType elementType; // ประเภทธาตุของอาวุธ
    public float damage;
    public Sprite weaponSprite; // รูปอาวุธสำหรับ UI หรือถือในมือ
    // public AnimationClip attackAnimation; 
}
