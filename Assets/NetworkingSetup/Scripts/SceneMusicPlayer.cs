using UnityEngine;
using System.Collections;

public class SceneMusicPlayer : MonoBehaviour
{
    public static SceneMusicPlayer Instance { get; private set; }

    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Clips")]
    [SerializeField] private AudioClip normalMusicClip;
    [SerializeField] private AudioClip endGameMusicClip;

    [Header("Settings")]
    [SerializeField] private float musicDelay = 0f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private bool loopMusic = true;

    [Header("Behaviour")]
    [Tooltip("If true, normal music starts automatically when the scene loads.")]
    [SerializeField] private bool playOnSceneStart = true;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = loopMusic;
        musicSource.volume = musicVolume;
    }

    private void Start()
    {
        if (playOnSceneStart && normalMusicClip != null)
        {
            PlayNormalMusic();
        }
    }

    // ---------- Public API ----------

    public void PlayNormalMusic()
    {
        PlayMusic(normalMusicClip);
    }

    public void PlayEndGameMusic()
    {
        PlayMusic(endGameMusicClip);
    }

    public void StopMusic()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (musicSource != null)
            musicSource.Stop();
    }

    // ---------- Internal helpers ----------

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        // Cancel any pending start
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        playRoutine = StartCoroutine(PlayMusicDelayed(clip));
    }

    private IEnumerator PlayMusicDelayed(AudioClip clip)
    {
        if (musicDelay > 0f)
            yield return new WaitForSeconds(musicDelay);

        musicSource.clip = clip;
        musicSource.loop = loopMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();

        playRoutine = null;
    }
}
