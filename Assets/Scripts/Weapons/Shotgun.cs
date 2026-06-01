using UnityEngine;

public class Shotgun : WeaponBase
{
    [Header("Shotgun")]
    [SerializeField] private int pelletCount = 8;
    [SerializeField] private float spreadAngle = 6f;

    [Header("Shotgun Audio")]
    [SerializeField] private AudioClip cockingClip;
    [SerializeField] private float cockingDelay = 0.35f;
    [SerializeField, Range(0f, 1f)] private float cockingVolume = 1f;
    [SerializeField] private AudioClip shellInsertClip;
    [SerializeField, Range(0f, 1f)] private float shellInsertVolume = 1f;

    private Coroutine cockingRoutine;

    private void Reset()
    {
        weaponType = WeaponType.Shotgun;
        ammoType = AmmoType.Shotgun;
        damage = 12f;
        range = 35f;
        fireCooldown = 0.9f;
        magazineSize = 6;
        currentAmmo = 6;
        reserveAmmo = 24;
        reloadTime = 2f;
    }

    public override void TryFire()
    {
        if (isReloading && currentAmmo > 0)
            CancelReload();

        base.TryFire();
    }

    protected override void Fire()
    {
        BeginFireCooldown();
        ConsumeAmmo(1);
        PlayFireSound();
        EjectCasing();
        ApplyCameraShake();

        Ray aimRay = GetAimRay();

        for (int i = 0; i < pelletCount; i++)
        {
            Ray pelletRay = new Ray(aimRay.origin, GetSpreadDirection(aimRay.direction));
            FireHitscan(pelletRay, damage, range);
        }

        Debug.Log($"{name} ammo: {currentAmmo}/{ReserveAmmo}");

        if (cockingRoutine != null)
            StopCoroutine(cockingRoutine);

        cockingRoutine = StartCoroutine(PlayCockingSoundRoutine());
    }

    protected override System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;

        Debug.Log($"{name} reloading shells...");

        while (currentAmmo < magazineSize && ReserveAmmo > 0)
        {
            PlayOneShot(shellInsertClip, shellInsertVolume);

            yield return new WaitForSeconds(reloadTime);

            int ammoToLoad = inventory != null
                ? inventory.ConsumeAmmo(ammoType, 1)
                : Mathf.Min(1, reserveAmmo);

            if (ammoToLoad <= 0)
                break;

            currentAmmo += ammoToLoad;

            if (inventory == null)
                reserveAmmo -= ammoToLoad;

            Debug.Log($"{name} loaded shell. Ammo: {currentAmmo}/{ReserveAmmo}");
        }

        isReloading = false;
        reloadRoutine = null;

        if (currentAmmo > 0)
            PlayOneShot(cockingClip, cockingVolume);
    }

    private System.Collections.IEnumerator PlayCockingSoundRoutine()
    {
        yield return new WaitForSeconds(cockingDelay);
        PlayOneShot(cockingClip, cockingVolume);
        cockingRoutine = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (cockingRoutine != null)
        {
            StopCoroutine(cockingRoutine);
            cockingRoutine = null;
        }
    }

    private Vector3 GetSpreadDirection(Vector3 direction)
    {
        float yaw = Random.Range(-spreadAngle, spreadAngle);
        float pitch = Random.Range(-spreadAngle, spreadAngle);

        return Quaternion.Euler(pitch, yaw, 0f) * direction;
    }
}
