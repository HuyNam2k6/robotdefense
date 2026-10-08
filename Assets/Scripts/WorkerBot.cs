using UnityEngine;

/// <summary>
/// WorkerBot - Base class tương thích với các hệ thống trong dự án (GameEconomy, Bullet, v.v.)
/// </summary>
public class WorkerBot : MonoBehaviour
{
    [Header("Cài Đặt Mục Tiêu & Khai Thác (Base)")]
    public Transform targetRock;
    public float mineInterval = 1.6f;
    public int stoneMinedCount = 0;
}
