using UnityEngine;

/// <summary>
/// Quản lý Hệ Thống Âm Thanh:
/// - Âm thanh nền (BGM): phát 'nhacnen.mp3' lặp vô tận, có thanh trượt riêng.
/// - Âm thanh hiệu ứng (SFX): tiếng bước chân, quái vật, vỗ cánh, đạn bắn, có thanh trượt riêng.
/// - Kéo thả thanh trượt mượt mà, thả tay ra đứng yên đúng vị trí.
/// - Lưu cài đặt vào PlayerPrefs để duy trì giữa các lần chơi.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("=== NHẠC NỀN (BGM) ===")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.65f;
    public bool isBgmMuted = false;

    [Header("=== HIỆU ỨNG ÂM THANH (SFX) ===")]
    [Range(0f, 1f)]
    public float sfxVolume = 0.85f;
    public bool isSfxMuted = false;

    // Tương thích ngược với các script cũ
    public float currentVolume { get => bgmVolume; set => SetBGMVolume(value); }
    public bool isMuted { get => isBgmMuted && isSfxMuted; set { SetBGMMuted(value); SetSFXMuted(value); } }

    private const string PREF_KEY_BGM_VOLUME = "Game_BGM_Volume";
    private const string PREF_KEY_BGM_MUTED = "Game_BGM_Muted";
    private const string PREF_KEY_SFX_VOLUME = "Game_SFX_Volume";
    private const string PREF_KEY_SFX_MUTED = "Game_SFX_Muted";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null && FindAnyObjectByType<GameAudioManager>() == null)
        {
            GameObject audioGo = new GameObject("[Game_Audio_Manager]");
            audioGo.AddComponent<GameAudioManager>();
            DontDestroyOnLoad(audioGo);
            Debug.Log("<color=#00FF88><b>[GameAudioManager]</b> Đã tự động kích hoạt Hệ thống Âm thanh Nhạc Nền & Hiệu Ứng!</color>");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo AudioSource cho Nhạc nền
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // Đọc cài đặt đã lưu của người chơi
        bgmVolume = PlayerPrefs.GetFloat(PREF_KEY_BGM_VOLUME, 0.65f);
        isBgmMuted = PlayerPrefs.GetInt(PREF_KEY_BGM_MUTED, 0) == 1;

        sfxVolume = PlayerPrefs.GetFloat(PREF_KEY_SFX_VOLUME, 0.85f);
        isSfxMuted = PlayerPrefs.GetInt(PREF_KEY_SFX_MUTED, 0) == 1;

        LoadAudioClip();
    }

    private void Start()
    {
        ApplyBGMVolume();
        PlayBGM();
    }

    private void LoadAudioClip()
    {
        if (bgmClip != null) return;

#if UNITY_EDITOR
        bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/nhacnen.mp3");
#endif
        if (bgmClip == null)
        {
            bgmClip = Resources.Load<AudioClip>("nhacnen");
        }
    }

    public void PlayBGM()
    {
        if (bgmSource == null) return;

        if (bgmClip == null)
        {
            LoadAudioClip();
        }

        if (bgmClip != null && bgmSource.clip != bgmClip)
        {
            bgmSource.clip = bgmClip;
        }

        if (bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
            Debug.Log("<color=#00FFAA><b>[GameAudioManager]</b> 🎵 Đang phát nhạc nền: nhacnen.mp3 (Âm lượng: " + Mathf.RoundToInt(bgmVolume * 100) + "%)</color>");
        }
    }

    // ========== ĐIỀU KHIỂN NHẠC NỀN (BGM) ==========
    public void SetBGMVolume(float newVolume)
    {
        bgmVolume = Mathf.Clamp01(newVolume);
        if (bgmVolume > 0.01f && isBgmMuted)
        {
            isBgmMuted = false;
            PlayerPrefs.SetInt(PREF_KEY_BGM_MUTED, 0);
        }
        else if (bgmVolume <= 0.01f)
        {
            isBgmMuted = true;
            PlayerPrefs.SetInt(PREF_KEY_BGM_MUTED, 1);
        }

        PlayerPrefs.SetFloat(PREF_KEY_BGM_VOLUME, bgmVolume);
        PlayerPrefs.Save();
        ApplyBGMVolume();
    }

    public void SetBGMMuted(bool mute)
    {
        isBgmMuted = mute;
        PlayerPrefs.SetInt(PREF_KEY_BGM_MUTED, isBgmMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyBGMVolume();
    }

    public void ToggleBGMMute()
    {
        if (isBgmMuted)
        {
            if (bgmVolume < 0.05f) bgmVolume = 0.65f;
            SetBGMMuted(false);
        }
        else
        {
            SetBGMMuted(true);
        }
    }

    public void ApplyBGMVolume()
    {
        if (bgmSource != null)
        {
            bgmSource.mute = isBgmMuted;
            bgmSource.volume = isBgmMuted ? 0f : bgmVolume;
        }
    }

    // ========== ĐIỀU KHIỂN HIỆU ỨNG ÂM THANH (SFX) ==========
    public void SetSFXVolume(float newVolume)
    {
        sfxVolume = Mathf.Clamp01(newVolume);
        if (sfxVolume > 0.01f && isSfxMuted)
        {
            isSfxMuted = false;
            PlayerPrefs.SetInt(PREF_KEY_SFX_MUTED, 0);
        }
        else if (sfxVolume <= 0.01f)
        {
            isSfxMuted = true;
            PlayerPrefs.SetInt(PREF_KEY_SFX_MUTED, 1);
        }

        PlayerPrefs.SetFloat(PREF_KEY_SFX_VOLUME, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXMuted(bool mute)
    {
        isSfxMuted = mute;
        PlayerPrefs.SetInt(PREF_KEY_SFX_MUTED, isSfxMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleSFXMute()
    {
        if (isSfxMuted)
        {
            if (sfxVolume < 0.05f) sfxVolume = 0.85f;
            SetSFXMuted(false);
        }
        else
        {
            SetSFXMuted(true);
        }
    }

    public float GetSFXVolume()
    {
        return isSfxMuted ? 0f : sfxVolume;
    }

    // Các hàm tương thích ngược
    public void SetVolume(float newVolume) => SetBGMVolume(newVolume);
    public void SetMuted(bool mute) { SetBGMMuted(mute); SetSFXMuted(mute); }
    public void ToggleMute()
    {
        bool hasSound = (!isBgmMuted && bgmVolume > 0.01f) || (!isSfxMuted && sfxVolume > 0.01f);
        if (hasSound)
        {
            SetBGMMuted(true);
            SetSFXMuted(true);
        }
        else
        {
            if (bgmVolume < 0.05f) bgmVolume = 0.65f;
            if (sfxVolume < 0.05f) sfxVolume = 0.85f;
            SetBGMMuted(false);
            SetSFXMuted(false);
        }
    }
    public void IncreaseVolume(float delta = 0.1f) => SetBGMVolume(bgmVolume + delta);
    public void DecreaseVolume(float delta = 0.1f) => SetBGMVolume(bgmVolume - delta);
    public void ApplyVolume() => ApplyBGMVolume();
}
