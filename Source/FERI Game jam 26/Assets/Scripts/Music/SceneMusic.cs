using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip track;
    [SerializeField] private float fadeTime = 1f;

    void Start()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayLevelTrack(track, fadeTime);
    }
}