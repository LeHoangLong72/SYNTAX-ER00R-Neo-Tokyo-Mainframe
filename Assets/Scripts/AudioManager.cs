using UnityEngine;

/// <summary>
/// AudioManager — Trái tim của toàn bộ game.
/// Chịu trách nhiệm phát nhạc và cung cấp thời gian chính xác (SongTime)
/// cho tất cả các class khác dùng để tính toán note rơi.
///
/// CÁCH DÙNG: AudioManager.Instance.SongTime
/// </summary>
public class AudioManager : MonoBehaviour
{
    // Singleton — chỉ tồn tại 1 instance duy nhất trong toàn game
    // Class khác gọi bằng: AudioManager.Instance.SongTime
    public static AudioManager Instance;

    [Header("Audio Components")]
    // AudioSource là component Unity phát âm thanh — gắn từ Inspector
    public AudioSource musicSource;

    [Header("Test (kéo file nhạc vào đây để test)")]
    // Clip nhạc để test — sau này sẽ load từ JSON beatmap
    public AudioClip testClip;

    // Thời điểm bài nhạc bắt đầu tính theo dspTime
    // dspTime là đồng hồ của audio thread, KHÔNG bị ảnh hưởng bởi FPS
    private double _songStartDspTime;

    // Biến kiểm tra xem nhạc đã phát chưa
    private bool _isPlaying = false;

    /// <summary>
    /// SongTime — Thời gian hiện tại trong bài nhạc (tính bằng giây).
    /// Đây là thứ NoteSpawner và JudgmentSystem sẽ hỏi liên tục.
    /// Ví dụ: SongTime = 1.5 nghĩa là đang ở giây thứ 1.5 của bài nhạc.
    /// </summary>
    public double SongTime
    {
        get
        {
            if (!_isPlaying) return 0;
            // AudioSettings.dspTime: thời gian thực của audio engine
            // Trừ đi thời điểm bắt đầu → ra thời gian trong bài nhạc
            return AudioSettings.dspTime - _songStartDspTime;
        }
    }

    // Để class khác biết nhạc đang phát hay chưa
    public bool IsPlaying => _isPlaying;

    void Awake()
    {
        // Gán Instance để class khác gọi được AudioManager.Instance
        Instance = this;
    }

    void Start()
    {
        // Nếu có testClip thì tự động phát khi game chạy (để test)
        if (testClip != null)
        {
            PlaySong(testClip);
        }
    }

    /// <summary>
    /// Phát bài nhạc với delay (mặc định 1 giây).
    /// Delay để NoteSpawner có thời gian chuẩn bị spawn note đầu tiên.
    /// </summary>
    /// <param name="clip">File nhạc cần phát</param>
    /// <param name="startDelay">Đợi bao nhiêu giây trước khi phát (mặc định 1s)</param>
    public void PlaySong(AudioClip clip, double startDelay = 1.0)
    {
        musicSource.clip = clip;

        // Ghi lại thời điểm nhạc SẼ bắt đầu (trong tương lai)
        _songStartDspTime = AudioSettings.dspTime + startDelay;

        // PlayScheduled: phát đúng tại thời điểm đã hẹn — cực kỳ chính xác
        // Khác với Play() có thể bị lệch vài ms do frame rate
        musicSource.PlayScheduled(_songStartDspTime);

        _isPlaying = true;

        Debug.Log($"[AudioManager] Bài nhạc '{clip.name}' sẽ phát sau {startDelay}s");
    }

    /// <summary>
    /// Dừng nhạc lại
    /// </summary>
    public void StopSong()
    {
        musicSource.Stop();
        _isPlaying = false;
        Debug.Log("[AudioManager] Đã dừng nhạc.");
    }
}