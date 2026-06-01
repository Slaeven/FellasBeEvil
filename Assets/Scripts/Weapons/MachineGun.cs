using System.Collections;
using UnityEngine;

public enum MachineGunFireMode
{
    Burst,
    FullAuto
}

public class MachineGun : WeaponBase
{
    [Header("Machine Gun")]
    [SerializeField] private MachineGunFireMode fireMode = MachineGunFireMode.Burst;
    [SerializeField] private int burstShotCount = 3;
    [SerializeField] private float burstShotDelay = 0.08f;

    [Header("Machine Gun Audio")]
    [SerializeField] private AudioClip burstFireClip;
    [SerializeField, Range(0f, 1f)] private float burstFireVolume = 1f;

    private bool isBursting;
    private bool suppressNextFireSound;

    public override bool FiresContinuously => fireMode == MachineGunFireMode.FullAuto;
    public override string FireModeName => fireMode.ToString();

    private void Reset()
    {
        weaponType = WeaponType.MachineGun;
        ammoType = AmmoType.MachineGun;
        damage = 10f;
        range = 55f;
        fireCooldown = 0.09f;
        magazineSize = 30;
        currentAmmo = 30;
        reserveAmmo = 90;
        reloadTime = 1.8f;
    }

    public override void TryFire()
    {
        if (fireMode == MachineGunFireMode.FullAuto)
        {
            base.TryFire();
            return;
        }

        if (isBursting || !CanFireNow())
            return;

        StartCoroutine(BurstFireRoutine());
    }

    public override void ToggleFireMode()
    {
        fireMode = fireMode == MachineGunFireMode.Burst
            ? MachineGunFireMode.FullAuto
            : MachineGunFireMode.Burst;

        Debug.Log($"{name} fire mode: {fireMode}");
    }

    public override void TryReload()
    {
        if (isBursting)
            return;

        base.TryReload();
    }

    protected override IEnumerator ReloadRoutine()
    {
        isBursting = false;
        yield return base.ReloadRoutine();
    }

    private IEnumerator BurstFireRoutine()
    {
        isBursting = true;

        bool useBurstClip = burstFireClip != null;

        if (useBurstClip)
            PlayOneShot(burstFireClip, burstFireVolume, firePitchRange);

        for (int i = 0; i < burstShotCount; i++)
        {
            if (i > 0)
                nextFireTime = Time.time;

            if (!CanFireNow())
                break;

            suppressNextFireSound = useBurstClip;
            Fire();

            if (i < burstShotCount - 1)
                yield return new WaitForSeconds(burstShotDelay);
        }

        isBursting = false;
    }

    protected override void PlayFireSound()
    {
        if (suppressNextFireSound)
        {
            suppressNextFireSound = false;
            return;
        }

        base.PlayFireSound();
    }
}
