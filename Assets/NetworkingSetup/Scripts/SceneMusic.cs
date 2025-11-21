using UnityEngine;
using System.Collections;

public class SceneMusic : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float musicDelay = 0f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private bool loopMusic = true;

    [Header("Behaviour")]
    [Tooltip("If true, music starts automatically when the scene loads.")]
    [SerializeField] private bool playOnSceneStart = true;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = loopMusic;
        musicSource.volume = musicVolume;
    }

    private void Start()
    {
        if (playOnSceneStart)
        {
            PlayMusic();
        }
    }

    /// <summary>
    /// Starts the music (respecting musicDelay). 
    /// Call this from a UI Button OnClick or other scripts.
    /// </summary>
    public void PlayMusic()
    {
        if (musicSource == null || musicClip == null)
            return;

        // Cancel any pending start
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        playRoutine = StartCoroutine(PlayMusicDelayed());
    }

    private IEnumerator PlayMusicDelayed()
    {
        if (musicDelay > 0f)
            yield return new WaitForSeconds(musicDelay);

        musicSource.clip = musicClip;
        musicSource.Play();
        playRoutine = null;
    }

    public void StopMusic()
    {
        // cancel delayed start if it's pending
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (musicSource != null)
            musicSource.Stop();
    }
}
