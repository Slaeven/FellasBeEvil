using UnityEngine;

public class Handgun : WeaponBase
{
    private void Reset()
    {
        ammoType = AmmoType.Handgun;
        damage = 25f;
        range = 50f;
        fireCooldown = 0.35f;
        magazineSize = 10;
        currentAmmo = 10;
        reserveAmmo = 30;
        reloadTime = 1.5f;
    }

    protected override void Fire()
    {
        base.Fire();

        // Later:
        // play handgun animation
        // spawn muzzle flash
        // play handgun sound
        // apply recoil
    }
}
