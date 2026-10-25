using System.Collections;
using UnityEngine;

// put this on a ROOT object (not a child) and keep one copy in every scene
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] private float volume = 0.3f;

    private AudioSource source;
    private AudioClip targetClip;      // what we are playing (or fading to)
    private AudioClip levelTrack;      // the current level's normal music
    private Coroutine fade;

    void Awake()
    {
        // only one manager survives; the extra copies (one per scene) destroy themselves
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
    }

    // lets you drag the Volume slider while the game is running
    void OnValidate()
    {
        if (Application.isPlaying && source != null && fade == null && source.isPlaying)
            source.volume = volume;
    }

    public void PlayLevelTrack(AudioClip clip, float fadeTime = 1f)
    {
        levelTrack = clip;
        Play(clip, fadeTime);
    }

    public void ReturnToLevelTrack(float fadeTime = 1f) { Play(levelTrack, fadeTime); }

    public void Stop(float fadeTime = 1f) { Play(null, fadeTime); }

    public void Play(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == targetClip) return;          // already playing (or fading to) this track

        targetClip = clip;
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(SwitchTrack(clip, fadeTime));
    }

    private IEnumerator SwitchTrack(AudioClip clip, float fadeTime)
    {
        // fade the current track out
        float startVolume = source.volume;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
            yield return null;
        }
        source.volume = 0f;
        source.Stop();

        source.clip = clip;
        if (clip == null)
        {
            fade = null;
            yield break;
        }
        source.Play();

        // fade the new one in
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(0f, volume, t / fadeTime);
            yield return null;
        }
        source.volume = volume;
        fade = null;
    }
}