using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ElementalReactionData
{
    public ElementType elementType;
    public AudioClip hitSound; // เสียงสำหรับธาตุนี้
    public List<string> hitAnimationTriggerNames; // รายชื่ออนิเมชันสำหรับธาตุนี้ (3-4 ชุด)
}
