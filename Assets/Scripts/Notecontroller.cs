using UnityEngine;

/// <summary>
/// NoteController — Gắn vào mỗi Note Prefab.
/// Chịu trách nhiệm di chuyển note từ trên xuống vạch phán đoán (Judgment Line).
/// Mỗi note được spawn ra sẽ có script này, tự biết mình phải rơi về đâu.
/// </summary>
public class NoteController : MonoBehaviour
{
    // ===================== DỮ LIỆU CỦA NOTE NÀY =====================

    // Thời điểm note này phải được bấm (giây từ đầu bài)
    [HideInInspector] public float hitTime;

    // Làn của note này (0-3)
    [HideInInspector] public int lane;

    // Loại note
    [HideInInspector] public NoteType noteType;

    // Nhãn hiển thị (0xFF, 0x4F, v.v.)
    [HideInInspector] public string label;

    // ===================== CẤU HÌNH CHUYỂN ĐỘNG =====================

    [Header("Vị trí spawn và vạch phán đoán")]
    // Y của điểm spawn (trên màn hình) — thường là 5f (trên cùng)
    public float spawnY = 5f;

    // Y của vạch phán đoán (Judgment Line) — thường là -3.5f (gần dưới)
    public float judgmentY = -3.5f;

    [Header("Thời gian rơi (giây)")]
    // Note mất bao nhiêu giây để rơi từ spawn đến judgment line
    // Giá trị này phải GIỐNG NHAU với NoteSpawner.fallDuration
    public float fallDuration = 2f;

    // ===================== BIẾN NỘI BỘ =====================

    // Thời điểm note được spawn (theo SongTime)
    // Note xuất hiện sớm hơn hitTime đúng một khoảng fallDuration
    private float _spawnTime;

    // Trạng thái: note đã bị xử lý (bấm trúng/miss) chưa
    private bool _isProcessed = false;

    // Tham chiếu đến component Text để hiển thị label (nếu dùng TextMeshPro)
    // private TMPro.TextMeshPro _labelText;

    void Start()
    {
        // Tính thời điểm spawn: note xuất hiện trước hitTime đúng fallDuration
        // Ví dụ: hitTime = 3.0, fallDuration = 2.0 → spawnTime = 1.0
        _spawnTime = hitTime - fallDuration;

        // Đặt note ở vị trí spawn ban đầu (trên màn hình)
        Vector3 startPos = transform.position;
        startPos.y = spawnY;
        transform.position = startPos;
    }

    void Update()
    {
        // Nếu note đã được xử lý rồi thì không làm gì nữa
        if (_isProcessed) return;

        // Lấy thời gian hiện tại trong bài nhạc từ AudioManager
        double currentSongTime = AudioManager.Instance.SongTime;

        // ---- TÍNH VỊ TRÍ NOTE ----
        // Lerp từ spawnY → judgmentY dựa theo tiến độ thời gian
        // progress = 0.0 → note ở trên cùng (spawn)
        // progress = 1.0 → note ở vạch phán đoán (cần bấm)
        float progress = (float)((currentSongTime - _spawnTime) / fallDuration);

        // Clamp để note không vượt quá vạch phán đoán khi lerp
        progress = Mathf.Clamp01(progress);

        // Tính Y hiện tại bằng nội suy tuyến tính
        float currentY = Mathf.Lerp(spawnY, judgmentY, progress);

        // Cập nhật vị trí note (giữ nguyên X và Z)
        transform.position = new Vector3(transform.position.x, currentY, transform.position.z);

        // ---- XỬ LÝ KHI NOTE VƯỢT QUÁ JUDGMENT LINE (MISS) ----
        // Nếu thời gian hiện tại vượt quá hitTime + window miss → tự xóa
        float missWindow = 0.15f; // 150ms — sau đó tính là Miss
        if ((float)currentSongTime > hitTime + missWindow && !_isProcessed)
        {
            Miss();
        }
    }

    /// <summary>
    /// Được gọi khi người chơi bấm trúng note này.
    /// JudgmentSystem sẽ gọi hàm này sau khi xác nhận input.
    /// </summary>
    public void OnHit()
    {
        _isProcessed = true;
        Debug.Log($"[Note] Hit! Lane {lane} | Time {hitTime:F3}s");

        // TODO: Phát hiệu ứng particle khi bấm trúng
        // TODO: Phát hitsound

        // Xóa note khỏi scene
        Destroy(gameObject);
    }

    /// <summary>
    /// Được gọi khi người chơi bỏ lỡ note (không bấm kịp).
    /// </summary>
    public void Miss()
    {
        _isProcessed = true;
        Debug.Log($"[Note] Miss! Lane {lane} | Time {hitTime:F3}s");

        // TODO: Hiệu ứng note bị miss (đổi màu đỏ, fade out)

        // Xóa note khỏi scene
        Destroy(gameObject);
    }

    /// <summary>
    /// Trả về offset (độ lệch giờ bấm so với lúc cần bấm).
    /// Giá trị âm: bấm sớm | Giá trị dương: bấm trễ
    /// Được JudgmentSystem dùng để phán đoán Perfect/Good/Miss
    /// </summary>
    public float GetCurrentOffset()
    {
        return (float)AudioManager.Instance.SongTime - hitTime;
    }
}