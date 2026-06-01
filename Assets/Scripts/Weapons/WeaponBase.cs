using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string displayName;

    [Header("Weapon Stats")]
    [SerializeField] protected float damage = 25f;
    [SerializeField] protected float range = 50f;
    [SerializeField] protected float fireCooldown = 0.35f;
    [SerializeField] protected LayerMask hitMask = ~0;

    [Header("Ammo")]
    [SerializeField] protected AmmoType ammoType = AmmoType.Handgun;
    [SerializeField] protected int magazineSize = 10;
    [SerializeField] protected int currentAmmo = 10;
    [SerializeField] protected int reserveAmmo = 30;
    [SerializeField] protected float reloadTime = 1.5f;

    [Header("Debug")]
    [SerializeField] protected GameObject hitMarkerPrefab;

    [Header("Audio")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClip fireClip;
    [SerializeField, Range(0f, 1f)] protected float fireVolume = 1f;
    [SerializeField] protected Vector2 firePitchRange = Vector2.one;

    [Header("Casing Ejection")]
    [SerializeField] protected EjectedCasing casingPrefab;
    [SerializeField] protected Transform casingEjectionPoint;
    [SerializeField] protected Vector3 casingLocalOffset = new Vector3(0.2f, 0.1f, 0f);
    [SerializeField] protected Vector3 casingEjectionVelocity = new Vector3(1.4f, 1.1f, -0.25f);
    [SerializeField] protected Vector3 casingTorque = new Vector3(8f, 16f, 8f);
    [SerializeField] protected float casingRandomVelocity = 0.35f;
    [SerializeField] protected AudioClip[] casingLandingClips;
    [SerializeField, Range(0f, 1f)] protected float casingLandingVolume = 0.7f;
    [SerializeField] protected Vector2 casingLandingPitchRange = new Vector2(0.95f, 1.05f);

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => inventory != null ? inventory.GetAmmoCount(ammoType) : reserveAmmo;
    public int MagazineSize => magazineSize;
    public AmmoType AmmoType => ammoType;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? GetType().Name : displayName;
    public bool IsReloading => isReloading;
    public virtual bool FiresContinuously => false;
    public virtual string FireModeName => "Single";

    protected PlayerInventory inventory;
    protected Camera aimCamera;
    protected float nextFireTime;
    protected bool isReloading;

    public virtual void Initialise(Camera camera, PlayerInventory playerInventory = null)
    {
        aimCamera = camera;
        inventory = playerInventory;
        currentAmmo = Mathf.Clamp(currentAmmo, 0, magazineSize);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null && fireClip != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public virtual void TryFire()
    {
        if (!CanFireNow())
            return;

        Fire();
    }

    protected bool CanFireNow()
    {
        if (aimCamera == null)
        {
            Debug.LogWarning($"{name} has no aim camera assigned.");
            return false;
        }

        if (isReloading)
            return false;

        if (Time.time < nextFireTime)
            return false;

        if (currentAmmo <= 0)
        {
            Debug.Log($"{name} is empty.");
            return false;
        }

        return true;
    }

    protected virtual void Fire()
    {
        BeginFireCooldown();
        ConsumeAmmo(1);
        PlayFireSound();
        EjectCasing();

        FireHitscan(GetAimRay(), damage, range);

        Debug.Log($"{name} ammo: {currentAmmo}/{ReserveAmmo}");
    }

    protected Ray GetAimRay()
    {
        return aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    protected void BeginFireCooldown()
    {
        nextFireTime = Time.time + fireCooldown;
    }

    protected void ConsumeAmmo(int amount)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
    }

    protected virtual void PlayFireSound()
    {
        PlayOneShot(fireClip, fireVolume, firePitchRange);
    }

    protected virtual void EjectCasing()
    {
        if (casingPrefab == null)
            return;

        Transform spawnTransform = casingEjectionPoint != null ? casingEjectionPoint : transform;
        Vector3 spawnPosition = spawnTransform.TransformPoint(casingLocalOffset);
        Quaternion spawnRotation = spawnTransform.rotation * Random.rotation;

        EjectedCasing casing = Instantiate(casingPrefab, spawnPosition, spawnRotation);
        casing.Initialise(casingLandingClips, casingLandingVolume, casingLandingPitchRange);

        if (casing.TryGetComponent(out Rigidbody rigidbody))
        {
            Vector3 velocity =
                spawnTransform.right * casingEjectionVelocity.x +
                spawnTransform.up * casingEjectionVelocity.y +
                spawnTransform.forward * casingEjectionVelocity.z;

            velocity += Random.insideUnitSphere * casingRandomVelocity;

            rigidbody.linearVelocity = velocity;
            rigidbody.angularVelocity = new Vector3(
                Random.Range(-casingTorque.x, casingTorque.x),
                Random.Range(-casingTorque.y, casingTorque.y),
                Random.Range(-casingTorque.z, casingTorque.z)
            );
        }
    }

    protected void PlayOneShot(AudioClip clip, float volume = 1f, Vector2? pitchRange = null)
    {
        if (clip == null)
            return;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        float originalPitch = audioSource.pitch;
        Vector2 range = pitchRange ?? Vector2.one;
        float minPitch = Mathf.Min(range.x, range.y);
        float maxPitch = Mathf.Max(range.x, range.y);
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
        audioSource.pitch = originalPitch;
    }

    protected bool FireHitscan(Ray ray, float shotDamage, float shotRange)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, shotRange, hitMask))
        {
            Debug.Log($"{name} hit {hit.collider.name}");

            Damageable damageable = hit.collider.GetComponent<Damageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(shotDamage);
            }

            if (hitMarkerPrefab != null)
            {
                Instantiate(
                    hitMarkerPrefab,
                    hit.point,
                    Quaternion.LookRotation(hit.normal)
                );
            }

            return true;
        }

        Debug.Log($"{name} missed.");
        return false;
    }

    public virtual void ToggleFireMode()
    {
    }

    public virtual void TryReload()
    {
        if (isReloading)
            return;

        if (currentAmmo >= magazineSize)
            return;

        if (ReserveAmmo <= 0)
            return;

        StartCoroutine(ReloadRoutine());
    }

    protected virtual System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;

        Debug.Log($"{name} reloading...");

        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToLoad = inventory != null
            ? inventory.ConsumeAmmo(ammoType, ammoNeeded)
            : Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;

        if (inventory == null)
            reserveAmmo -= ammoToLoad;

        isReloading = false;

        Debug.Log($"{name} reloaded. Ammo: {currentAmmo}/{ReserveAmmo}");
    }
}
