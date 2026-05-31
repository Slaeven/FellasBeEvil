using UnityEngine;

public class Handgun : WeaponBase
{
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