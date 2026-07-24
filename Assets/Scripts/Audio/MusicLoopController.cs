using UnityEngine;

namespace VelocityRush.AudioSystem
{
    /// <summary>Small persistent music playlist player. Clips should be loop-ready, compressed
    /// and streamed when long; one source is intentionally used for mobile memory discipline.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicLoopController : MonoBehaviour
    {
        [SerializeField] private AudioClip[] loops;
        [SerializeField] private bool shuffle;
        [SerializeField, Range(0f, 1f)] private float volume = .65f;
        private AudioSource source;
        private int index;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = volume;
        }

        private void Start()
        {
            if (loops != null && loops.Length > 0) PlayIndex(0);
        }

        public void PlayNext()
        {
            if (loops == null || loops.Length == 0) return;
            index = shuffle ? Random.Range(0, loops.Length) : (index + 1) % loops.Length;
            PlayIndex(index);
        }

        public void PlayIndex(int value)
        {
            if (loops == null || loops.Length == 0) return;
            index = Mathf.Clamp(value, 0, loops.Length - 1);
            if (loops[index] == null) return;
            source.clip = loops[index];
            source.Play();
        }
    }
}
