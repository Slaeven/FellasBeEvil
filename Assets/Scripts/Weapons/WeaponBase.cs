using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] protected float damage = 25f;
    [SerializeField] protected float range = 50f;
    [SerializeField] protected float fireCooldown = 0.35f;
    [SerializeField] protected LayerMask hitMask = ~0;

    [Header("Ammo")]
    [SerializeField] protected int magazineSize = 10;
    [SerializeField] protected int currentAmmo = 10;
    [SerializeField] protected int reserveAmmo = 30;
    [SerializeField] protected float reloadTime = 1.5f;

    [Header("Debug")]
    [SerializeField] protected GameObject hitMarkerPrefab;

    protected Camera aimCamera;
    protected float nextFireTime;
    protected bool isReloading;

    public virtual void Initialise(Camera camera)
    {
        aimCamera = camera;
        currentAmmo = Mathf.Clamp(currentAmmo, 0, magazineSize);
    }

    public virtual void TryFire()
    {
        if (aimCamera == null)
        {
            Debug.LogWarning($"{name} has no aim camera assigned.");
            return;
        }

        if (isReloading)
            return;

        if (Time.time < nextFireTime)
            return;

        if (currentAmmo <= 0)
        {
            Debug.Log($"{name} is empty.");
            return;
        }

        Fire();
    }

    protected virtual void Fire()
    {
        nextFireTime = Time.time + fireCooldown;
        currentAmmo--;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            Debug.Log($"{name} hit {hit.collider.name}");

            Damageable damageable = hit.collider.GetComponent<Damageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            if (hitMarkerPrefab != null)
            {
                Instantiate(
                    hitMarkerPrefab,
                    hit.point,
                    Quaternion.LookRotation(hit.normal)
                );
            }
        }
        else
        {
            Debug.Log($"{name} missed.");
        }

        Debug.Log($"{name} ammo: {currentAmmo}/{reserveAmmo}");
    }

    public virtual void TryReload()
    {
        if (isReloading)
            return;

        if (currentAmmo >= magazineSize)
            return;

        if (reserveAmmo <= 0)
            return;

        StartCoroutine(ReloadRoutine());
    }

    protected virtual System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;

        Debug.Log($"{name} reloading...");

        yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;

        Debug.Log($"{name} reloaded. Ammo: {currentAmmo}/{reserveAmmo}");
    }
}