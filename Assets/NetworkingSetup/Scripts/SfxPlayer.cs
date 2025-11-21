using UnityEngine;
using System.Collections;

public class SfxPlayer : MonoBehaviour
{
    [Header("Sound Effect")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip sfxClip;
    [SerializeField] private float sfxDelay = 0f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        if (sfxSource != null && sfxClip != null)
            playRoutine = StartCoroutine(PlaySfxDelayed());
    }

    private IEnumerator PlaySfxDelayed()
    {
        if (sfxDelay > 0f)
            yield return new WaitForSeconds(sfxDelay);

        sfxSource.PlayOneShot(sfxClip, sfxVolume);
        playRoutine = null;
    }

    public void StopSfx()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (sfxSource != null)
            sfxSource.Stop();
    }
}