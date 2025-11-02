using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target; // ลาก Player มาใส่ตรงนี้ใน Inspector
    public float smoothSpeed = 5f; // ความเร็วในการตาม (ปรับความเนียนได้)
    public Vector3 offset; // ระยะห่างจากตัวละคร (เช่น Vector3(0, 0, -10))

    // ตัวแปรสำหรับจัดการการตามแบบ Grid-based / TibiaMe Style
    private Vector3 desiredPosition;

    void Start()
    {
        if (target != null)
        {
            // ตั้งตำแหน่งเริ่มต้นของกล้องให้ตรงกับเป้าหมายทันที
            desiredPosition = target.position + offset;
            transform.position = desiredPosition;
        }
    }

    // ใช้อัปเดตหลังการเคลื่อนที่ของผู้เล่น
    void LateUpdate()
    {
        if (target != null)
        {
            desiredPosition = target.position + offset;

            // วิธีที่ 1: ติดตามทันที (Snap follow - เหมือนเกม TibiaME ดั้งเดิมที่สุด)
            // transform.position = desiredPosition;

            // วิธีที่ 2: ติดตามแบบนุ่มนวล (Smooth follow - เนียนตามากขึ้น)
            // ใช้ Mathf.Lerp หรือ Vector3.Lerp เพื่อเคลื่อนที่อย่างนุ่มนวล
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}
