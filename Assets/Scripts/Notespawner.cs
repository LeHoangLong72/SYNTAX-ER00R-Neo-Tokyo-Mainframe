using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NoteSpawner — Đọc file beatmap JSON và spawn note đúng thời điểm.
/// Đây là "nhạc trưởng" của gameplay: biết khi nào cần tạo note nào, ở làn nào.
///
/// CÁCH HOẠT ĐỘNG:
/// 1. Load file JSON từ folder Charts/
/// 2. Mỗi frame, kiểm tra SongTime
/// 3. Nếu đến lúc spawn note tiếp theo → Instantiate note đó ra scene
/// </summary>
public class NoteSpawner : MonoBehaviour
{
    // ===================== CẤU HÌNH TỪ INSPECTOR =====================

    [Header("Prefab của note (kéo từ folder Prefabs vào)")]
    // GameObject mẫu của note — Unity sẽ clone nó ra mỗi lần spawn
    public GameObject notePrefab;

    [Header("Vị trí X của 4 làn (từ trái sang phải)")]
    // Mỗi làn có 1 tọa độ X cố định trên màn hình
    // Ví dụ: làn 0 ở x=-3, làn 1 ở x=-1, làn 2 ở x=1, làn 3 ở x=3
    public float[] lanePositions = { -3f, -1f, 1f, 3f };

    [Header("Thời gian note rơi (giây) — phải khớp với NoteController")]
    // Note xuất hiện trước hitTime bao nhiêu giây
    // Giá trị càng nhỏ → note rơi nhanh hơn (khó hơn)
    public float fallDuration = 2f;

    [Header("Tên file beatmap trong Resources/Charts/ (không cần .json)")]
    // File JSON đặt trong Assets/Resources/Charts/
    // Ví dụ: "song1" → Unity tìm file Assets/Resources/Charts/song1.json
    public string chartFileName = "song1";

    // ===================== BIẾN NỘI BỘ =====================

    // Dữ liệu beatmap đã parse từ JSON
    private ChartData _chartData;

    // Index của note tiếp theo cần spawn trong danh sách
    // Tăng dần từ 0 đến cuối danh sách
    private int _nextNoteIndex = 0;

    // Danh sách tất cả note (shortcut từ _chartData.notes)
    private List<NoteData> _notes;

    // Đã bắt đầu game chưa (nhạc đang phát)
    private bool _isRunning = false;

    // ===================== UNITY LIFECYCLE =====================

    void Start()
    {
        LoadChart();
    }

    void Update()
    {
        // Chỉ chạy khi game đang chạy và còn note chưa spawn
        if (!_isRunning || _notes == null) return;
        if (_nextNoteIndex >= _notes.Count) return;

        // Lấy thời gian hiện tại trong bài nhạc
        double songTime = AudioManager.Instance.SongTime;

        // Kiểm tra note tiếp theo: có nên spawn chưa?
        // Note cần spawn sớm hơn hitTime đúng fallDuration
        // Ví dụ: hitTime = 3.0, fallDuration = 2.0 → spawn lúc songTime = 1.0
        NoteData nextNote = _notes[_nextNoteIndex];
        double spawnTime = nextNote.time - fallDuration + _chartData.offset;

        if (songTime >= spawnTime)
        {
            SpawnNote(nextNote);
            _nextNoteIndex++;
        }
    }

    // ===================== LOAD BEATMAP =====================

    /// <summary>
    /// Load file JSON từ Resources/Charts/ và parse thành ChartData.
    /// Sau đó tự động bắt đầu phát nhạc.
    /// </summary>
    void LoadChart()
    {
        // TextAsset: cách Unity đọc file text (JSON, CSV...) từ Resources/
        // File phải nằm trong Assets/Resources/Charts/song1.json
        TextAsset jsonFile = Resources.Load<TextAsset>($"Charts/{chartFileName}");

        if (jsonFile == null)
        {
            Debug.LogError($"[NoteSpawner] Không tìm thấy file: Resources/Charts/{chartFileName}.json");
            Debug.LogError("Hãy đặt file JSON vào Assets/Resources/Charts/");
            return;
        }

        // Parse JSON string → ChartData object
        _chartData = JsonUtility.FromJson<ChartData>(jsonFile.text);
        _notes = _chartData.notes;

        Debug.Log($"[NoteSpawner] Đã load beatmap: {_chartData.title}");
        Debug.Log($"[NoteSpawner] BPM: {_chartData.bpm} | Tổng note: {_notes.Count}");

        // Bắt đầu phát nhạc (delay 1 giây để chuẩn bị)
        // TODO: Load AudioClip đúng bài từ Resources/Audio/
        // Hiện tại AudioManager tự phát testClip từ Inspector
        _isRunning = true;
    }

    // ===================== SPAWN NOTE =====================

    /// <summary>
    /// Tạo một note trong scene tại đúng vị trí và làn.
    /// </summary>
    void SpawnNote(NoteData data)
    {
        // Kiểm tra làn hợp lệ (0-3)
        if (data.lane < 0 || data.lane >= lanePositions.Length)
        {
            Debug.LogWarning($"[NoteSpawner] Làn {data.lane} không hợp lệ, bỏ qua note này.");
            return;
        }

        // Tính vị trí spawn: đúng làn, trên đỉnh màn hình
        float xPos = lanePositions[data.lane];
        Vector3 spawnPos = new Vector3(xPos, 6f, 0f); // Y=6 là trên màn hình

        // Clone prefab note ra scene
        GameObject noteObj = Instantiate(notePrefab, spawnPos, Quaternion.identity);

        // Lấy NoteController từ prefab và truyền dữ liệu vào
        NoteController controller = noteObj.GetComponent<NoteController>();
        if (controller != null)
        {
            controller.hitTime = data.time;        // Lúc nào phải bấm
            controller.lane = data.lane;            // Làn bao nhiêu
            controller.noteType = data.GetNoteType(); // Tap hay Hold
            controller.label = data.label;          // Nhãn hiển thị
            controller.fallDuration = fallDuration; // Thời gian rơi
        }

        Debug.Log($"[NoteSpawner] Spawn note | Lane {data.lane} | Hit at {data.time:F3}s | Label: {data.label}");
    }

    // ===================== PUBLIC METHODS =====================

    /// <summary>
    /// Bắt đầu/tiếp tục spawn note (gọi từ GameManager)
    /// </summary>
    public void StartSpawning()
    {
        _isRunning = true;
    }

    /// <summary>
    /// Dừng spawn note (ví dụ khi pause game)
    /// </summary>
    public void StopSpawning()
    {
        _isRunning = false;
    }
}