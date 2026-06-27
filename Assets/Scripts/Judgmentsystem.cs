using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// JudgmentSystem — Nhận input từ bàn phím và phán đoán độ chính xác.
/// Đây là nơi quyết định: bấm này là Perfect, Good hay Miss?
///
/// PHÍM BẤM MẶC ĐỊNH:
/// Làn 0 → D
/// Làn 1 → F
/// Làn 2 → J
/// Làn 3 → K
///
/// CỬA SỔ PHÁN ĐOÁN (Judgment Windows):
/// Perfect: ±50ms  → nhân 100% điểm
/// Good:    ±100ms → nhân 50% điểm
/// Miss:    > 100ms → mất combo
/// </summary>
public class JudgmentSystem : MonoBehaviour
{
    // ===================== CẤU HÌNH TỪ INSPECTOR =====================

    [Header("Cửa sổ phán đoán (giây)")]
    // Nếu bấm trong vòng ±perfectWindow giây → Perfect
    public float perfectWindow = 0.050f; // 50ms

    // Nếu bấm trong vòng ±goodWindow giây → Good
    public float goodWindow = 0.100f;    // 100ms

    [Header("Điểm số")]
    // Điểm cộng cho mỗi loại phán đoán
    public int perfectScore = 100;
    public int goodScore = 50;

    [Header("4 làn note — kéo các NoteColumn vào đây")]
    // Mỗi phần tử là một "cột" chứa các note đang rơi ở làn đó
    // NoteSpawner sẽ đặt note vào đây để JudgmentSystem tìm
    // (Hiện tại dùng Physics2D.OverlapPoint để tìm note gần nhất)

    // ===================== BIẾN NỘI BỘ =====================

    // Điểm hiện tại
    private int _score = 0;

    // Combo hiện tại (số Perfect/Good liên tiếp không có Miss)
    private int _combo = 0;

    // Combo lớn nhất đạt được
    private int _maxCombo = 0;

    // Số Perfect, Good, Miss trong màn chơi
    private int _perfectCount = 0;
    private int _goodCount = 0;
    private int _missCount = 0;

    // Compile Buffer (0.0 → 1.0) — đầy thì kích hoạt Syntax Strike
    private float _compileBuffer = 0f;

    // Đang trong Syntax Strike (Overclock mode) không
    private bool _isSyntaxStrikeActive = false;

    // ===================== UNITY LIFECYCLE =====================

    void Update()
    {
        // Kiểm tra input cho từng làn
        // Dùng Keyboard.current thay cho Input.GetKeyDown (Unity New Input System)
        var kb = Keyboard.current;
        if (kb == null) return; // Không có bàn phím → bỏ qua

        // Làn 0: phím D
        if (kb.dKey.wasPressedThisFrame) TryHit(0);

        // Làn 1: phím F
        if (kb.fKey.wasPressedThisFrame) TryHit(1);

        // Làn 2: phím J
        if (kb.jKey.wasPressedThisFrame) TryHit(2);

        // Làn 3: phím K
        if (kb.kKey.wasPressedThisFrame) TryHit(3);
    }

    // ===================== LOGIC PHÁN ĐOÁN =====================

    /// <summary>
    /// Xử lý khi người chơi bấm phím ở làn chỉ định.
    /// Tìm note gần Judgment Line nhất ở làn đó và phán đoán.
    /// </summary>
    void TryHit(int lane)
    {
        // Tìm note gần nhất ở làn này còn chưa bị xử lý
        NoteController targetNote = FindNearestNote(lane);

        if (targetNote == null)
        {
            // Bấm nhưng không có note → không làm gì (tránh phạt oan)
            Debug.Log($"[Judgment] Làn {lane}: bấm nhưng không có note.");
            return;
        }

        // Tính offset: âm = bấm sớm, dương = bấm trễ
        float offset = targetNote.GetCurrentOffset();
        float absOffset = Mathf.Abs(offset);

        // Phán đoán dựa theo offset
        if (absOffset <= perfectWindow)
        {
            OnPerfect(targetNote, offset);
        }
        else if (absOffset <= goodWindow)
        {
            OnGood(targetNote, offset);
        }
        else
        {
            // Bấm quá sớm/muộn so với goodWindow → không tính
            Debug.Log($"[Judgment] Làn {lane}: bấm lệch {offset * 1000:F0}ms — quá sớm/muộn");
        }
    }

    /// <summary>
    /// Tìm NoteController gần Judgment Line nhất ở một làn cụ thể.
    /// Dùng cách đơn giản: tìm trong tất cả NoteController đang tồn tại.
    /// </summary>
    NoteController FindNearestNote(int lane)
    {
        // Lấy tất cả NoteController đang có trong scene
        NoteController[] allNotes = FindObjectsByType<NoteController>(FindObjectsSortMode.None);

        NoteController nearest = null;
        float smallestOffset = float.MaxValue;

        foreach (var note in allNotes)
        {
            // Chỉ xét note đúng làn
            if (note.lane != lane) continue;

            // Tính khoảng cách thời gian đến hitTime
            float offset = Mathf.Abs(note.GetCurrentOffset());

            // Chỉ xét note trong vòng goodWindow (tránh pick note quá xa)
            if (offset < goodWindow && offset < smallestOffset)
            {
                smallestOffset = offset;
                nearest = note;
            }
        }

        return nearest;
    }

    // ===================== XỬ LÝ KẾT QUẢ =====================

    void OnPerfect(NoteController note, float offset)
    {
        _perfectCount++;
        _combo++;
        if (_combo > _maxCombo) _maxCombo = _combo;

        // Tăng Compile Buffer nhiều hơn khi Perfect
        _compileBuffer = Mathf.Clamp01(_compileBuffer + 0.1f);

        // Tính điểm, nhân đôi nếu đang Syntax Strike
        int points = _isSyntaxStrikeActive ? perfectScore * 2 : perfectScore;
        _score += points;

        note.OnHit();

        Debug.Log($"[Judgment] ★ PERFECT! Offset: {offset * 1000:F1}ms | Score: {_score} | Combo: {_combo} | Buffer: {_compileBuffer:P0}");

        // Kiểm tra có đủ Compile Buffer để kích hoạt Syntax Strike không
        CheckSyntaxStrike();
    }

    void OnGood(NoteController note, float offset)
    {
        _goodCount++;
        _combo++;
        if (_combo > _maxCombo) _maxCombo = _combo;

        // Tăng Compile Buffer ít hơn khi Good
        _compileBuffer = Mathf.Clamp01(_compileBuffer + 0.05f);

        int points = _isSyntaxStrikeActive ? goodScore * 2 : goodScore;
        _score += points;

        note.OnHit();

        Debug.Log($"[Judgment] ○ GOOD. Offset: {offset * 1000:F1}ms | Score: {_score} | Combo: {_combo}");
    }

    /// <summary>
    /// Gọi khi note bị miss (từ NoteController.Miss() → callback về đây)
    /// </summary>
    public void OnMiss()
    {
        _missCount++;
        _combo = 0; // Reset combo

        // Giảm Compile Buffer khi miss
        _compileBuffer = Mathf.Clamp01(_compileBuffer - 0.2f);

        // Tắt Syntax Strike nếu đang bật
        if (_isSyntaxStrikeActive)
        {
            DeactivateSyntaxStrike();
        }

        Debug.Log($"[Judgment] ✗ MISS! Combo reset. Buffer: {_compileBuffer:P0}");
    }

    // ===================== SYNTAX STRIKE (OVERCLOCK MODE) =====================

    void CheckSyntaxStrike()
    {
        // Kích hoạt Syntax Strike khi Compile Buffer đầy (100%)
        if (_compileBuffer >= 1f && !_isSyntaxStrikeActive)
        {
            ActivateSyntaxStrike();
        }
    }

    void ActivateSyntaxStrike()
    {
        _isSyntaxStrikeActive = true;
        Debug.Log("[Judgment] ⚡ SYNTAX STRIKE ACTIVATED — OVERCLOCK MODE!");
        // TODO: Gọi hiệu ứng đổi màu UI sang CRT amber
        // TODO: Tăng tốc độ note rơi
        // TODO: Phát âm thanh kích hoạt
    }

    void DeactivateSyntaxStrike()
    {
        _isSyntaxStrikeActive = false;
        _compileBuffer = 0f;
        Debug.Log("[Judgment] Syntax Strike kết thúc.");
        // TODO: Khôi phục UI về màu mặc định
    }

    // ===================== GETTER ĐỂ UI ĐỌC =====================

    public int Score => _score;
    public int Combo => _combo;
    public int MaxCombo => _maxCombo;
    public float CompileBuffer => _compileBuffer;
    public bool IsSyntaxStrikeActive => _isSyntaxStrikeActive;

    /// <summary>
    /// Trả về thống kê cuối màn (dùng cho màn hình kết quả)
    /// </summary>
    public string GetResultSummary()
    {
        return $"Score: {_score}\nMax Combo: {_maxCombo}\nPerfect: {_perfectCount} | Good: {_goodCount} | Miss: {_missCount}";
    }
}