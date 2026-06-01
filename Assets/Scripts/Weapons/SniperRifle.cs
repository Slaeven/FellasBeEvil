using UnityEngine;

public class SniperRifle : WeaponBase
{
    private void Reset()
    {
        ammoType = AmmoType.Rifle;
        damage = 90f;
        range = 150f;
        fireCooldown = 1.25f;
        magazineSize = 5;
        currentAmmo = 5;
        reserveAmmo = 15;
        reloadTime = 2.4f;
    }

    protected override void Fire()
    {
        base.Fire();

        // Later:
        // add scope sway, zoom, bolt animation and heavier recoil.
    }
}
