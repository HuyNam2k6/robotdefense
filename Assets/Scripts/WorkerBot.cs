using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class WorkerBot : MonoBehaviour
{
    private Animator animator;
    private CharacterController cc;

    [Header("Cài đặt Mục Tiêu & Di Chuyển")]
    public Transform targetRock;
    public float moveSpeed = 2.5f;
    [Tooltip("Khoảng cách đến sát cục đá mới bắt đầu đào (0.8m - 1.0m là vừa sát đẹp)")]
    public float stopDistance = 0.9f;
    [Tooltip("Thời gian nghỉ giữa các nhát bổ tiếp theo")]
    public float mineInterval = 1.8f;

    private float mineTimer = 0f;
    private bool isMining = false;
    private Vector3 velocity; // Trọng lực

    void Start()
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();

        // Đảm bảo vừa vào game TUYỆT ĐỐI không tự động Attack
        if (animator != null)
        {
            animator.ResetTrigger("mine");
            animator.SetBool("isMoving", false);
        }
        isMining = false;
        mineTimer = 0f;
    }

    void Update()
    {
        // ================= 1. TRỌNG LỰC =================
        if (cc.isGrounded)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += Physics.gravity.y * Time.deltaTime;
        }
        cc.Move(velocity * Time.deltaTime);

        // Nếu không có mục tiêu -> đứng yên
        if (targetRock == null)
        {
            if (animator != null) animator.SetBool("isMoving", false);
            return;
        }

        // ================= 2. TÍNH KHOẢNG CÁCH TRÊN MẶT ĐẤT =================
        Vector3 targetPosXZ = new Vector3(targetRock.position.x, transform.position.y, targetRock.position.z);
        float sqrDistance = (transform.position - targetPosXZ).sqrMagnitude;
        float sqrStopDistance = stopDistance * stopDistance;

        // ================= 3. NẾU Ở XA -> CHẠY TỚI ĐÁ, TUYỆT ĐỐI KHÔNG ATTACK =================
        if (sqrDistance > sqrStopDistance)
        {
            isMining = false;
            mineTimer = 0f;

            // Di chuyển tới cục đá
            Vector3 direction = (targetPosXZ - transform.position).normalized;
            cc.Move(direction * moveSpeed * Time.deltaTime);

            // Xoay mặt nhìn về phía cục đá
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
            }

            if (animator != null)
            {
                animator.SetBool("isMoving", true);
            }
        }
        else
        {
            // ================= 4. KHI VỪA TỚI TẦM -> DỪNG BƯỚC VÀ ATTACK NGAY LẬP TỨC =================
            // Xoay mặt nhìn thẳng vào đá
            Vector3 lookDir = (targetPosXZ - transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // Dừng bước chân tức thì
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
            }

            // Nếu vừa chạm tới tầm đánh lần đầu: ATTACK NGAY LẬP TỨC (Không bước thêm, không chờ delay)
            if (!isMining)
            {
                isMining = true;
                mineTimer = 0f;

                if (animator != null)
                {
                    animator.SetTrigger("mine");
                }
                Debug.Log("<color=yellow>⛏️ [WorkerBot] Vừa tới sát mép đá là ĐẬP NGAY LẬP TỨC!</color>");
            }
            else
            {
                // Các nhát đập tiếp theo tính theo chu kỳ
                mineTimer += Time.deltaTime;
                if (mineTimer >= mineInterval)
                {
                    mineTimer = 0f;
                    if (animator != null)
                    {
                        animator.SetTrigger("mine");
                    }
                    Debug.Log("<color=yellow>⛏️ [WorkerBot] Bổ tiếp 1 nhát đá!</color>");
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
