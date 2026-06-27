using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NoteType — Loại note trong game.
/// Tap: bấm một lần (nốt đơn)
/// Hold: giữ phím (nốt giữ)
/// </summary>
public enum NoteType
{
    Tap,
    Hold
}

/// <summary>
/// NoteData — Dữ liệu của MỘT note trong beatmap.
/// Mỗi note cần biết: xuất hiện lúc nào, ở làn nào, loại gì.
/// Class này map 1-1 với từng object trong mảng "notes" của file JSON.
/// </summary>
[Serializable]
public class NoteData
{
    // Thời điểm note phải được bấm (tính bằng giây từ đầu bài)
    // Ví dụ: time = 1.371 nghĩa là phải bấm đúng lúc giây 1.371
    public float time;

    // Làn số (0, 1, 2, 3) — tương ứng 4 cột từ trái sang phải
    public int lane;

    // Loại note: "tap" hoặc "hold"
    public string type;

    // Thời gian giữ (chỉ dùng khi type == "hold"), tính bằng giây
    public float duration;

    // Nhãn hiển thị trên note (hex code như 0xFF, hoặc ký tự Kanji)
    public string label;

    /// <summary>
    /// Chuyển string "tap"/"hold" sang enum NoteType cho tiện dùng trong code
    /// </summary>
    public NoteType GetNoteType()
    {
        return type == "hold" ? NoteType.Hold : NoteType.Tap;
    }
}

/// <summary>
/// ChartData — Toàn bộ dữ liệu của một beatmap (file nhạc + lịch note).
/// Được load từ file JSON trong folder Charts/.
///
/// Cấu trúc file JSON tương ứng:
/// {
///   "title": "SYNTAX_ER00R_01",
///   "bpm": 175,
///   "offset": 0.012,
///   "notes": [ { "time": 1.371, "lane": 0, "type": "tap", "label": "0xFF" }, ... ]
/// }
/// </summary>
[Serializable]
public class ChartData
{
    // Tên bài nhạc
    public string title;

    // BPM (nhịp mỗi phút) — dùng để tính toán visual sau này
    public float bpm;

    // Offset (giây) — bù trừ nếu nhạc bị lệch so với beatmap
    // Giá trị dương: note xuất hiện muộn hơn | Âm: sớm hơn
    public float offset;

    // Danh sách tất cả note trong bài
    public List<NoteData> notes;
}