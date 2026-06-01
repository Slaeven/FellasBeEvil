using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EjectedCasing : MonoBehaviour
{
    [SerializeField] private AudioClip[] landingClips;
    [SerializeField, Range(0f, 1f)] private float landingVolume = 0.7f;
    [SerializeField] private Vector2 landingPitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private float minimumImpactSpeed = 0.75f;
    [SerializeField] private float armingDelay = 0.1f;
    [SerializeField] private float lifetime = 8f;

    private bool hasPlayedLandingSound;
    private float spawnTime;

    private void Start()
    {
        spawnTime = Time.time;
        Destroy(gameObject, lifetime);
    }

    public void Initialise(AudioClip[] clips, float volume, Vector2 pitchRange)
    {
        landingClips = clips;
        landingVolume = volume;
        landingPitchRange = pitchRange;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPlayedLandingSound ||
            Time.time < spawnTime + armingDelay ||
            collision.contactCount <= 0 ||
            collision.relativeVelocity.magnitude < minimumImpactSpeed)
        {
            return;
        }

        PlayLandingSound(collision.GetContact(0).point);
        hasPlayedLandingSound = true;
    }

    private void PlayLandingSound(Vector3 position)
    {
        if (landingClips == null || landingClips.Length == 0)
            return;

        AudioClip clip = landingClips[Random.Range(0, landingClips.Length)];

        if (clip == null)
            return;

        GameObject audioObject = new GameObject("Casing Landing Sound");
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = landingVolume;
        source.pitch = Random.Range(
            Mathf.Min(landingPitchRange.x, landingPitchRange.y),
            Mathf.Max(landingPitchRange.x, landingPitchRange.y)
        );
        source.spatialBlend = 1f;
        source.Play();

        Destroy(audioObject, clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch)));
    }
}
