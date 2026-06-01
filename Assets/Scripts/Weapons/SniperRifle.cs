using UnityEngine;

public class SniperRifle : WeaponBase
{
    [Header("Sniper Audio")]
    [SerializeField] private AudioClip boltActionClip;
    [SerializeField] private float boltActionDelay = 0.45f;
    [SerializeField, Range(0f, 1f)] private float boltActionVolume = 1f;

    [Header("Scope")]
    [SerializeField] private float scopedFOV = 18f;
    [SerializeField] private GameObject scopeOverlay;

    private bool isAiming;
    private Coroutine boltActionRoutine;

    private void Reset()
    {
        weaponType = WeaponType.SniperRifle;
        ammoType = AmmoType.Rifle;
        damage = 90f;
        range = 150f;
        fireCooldown = 1.25f;
        magazineSize = 5;
        currentAmmo = 5;
        reserveAmmo = 15;
        reloadTime = 2.4f;
        tracerColor = new Color(1f, 0.92f, 0.45f, 0.8f);
        tracerWidth = 0.035f;
        tracerDuration = 0.12f;
        shakeIntensity = 0.14f;
        shakeDuration = 0.12f;
    }

    public override void Initialise(Camera camera, PlayerInventory playerInventory = null)
    {
        base.Initialise(camera, playerInventory);

        UpdateScopeOverlay();
    }

    protected override void Fire()
    {
        base.Fire();

        if (boltActionRoutine != null)
            StopCoroutine(boltActionRoutine);

        boltActionRoutine = StartCoroutine(PlayBoltActionRoutine());
    }

    public override void ToggleScope()
    {
        UpdateScopeOverlay();
    }

    [ContextMenu("Toggle Scope")]
    private void ToggleScopeFromInspector()
    {
        ToggleScope();
    }

    public override float GetAimFOV(float defaultAimFOV)
    {
        return HasScopeAttachment() ? scopedFOV : defaultAimFOV;
    }

    public override void SetAiming(bool aiming)
    {
        isAiming = aiming;
        UpdateScopeOverlay();
    }

    public override void OnEquipped()
    {
        UpdateScopeOverlay();
    }

    public override void OnUnequipped()
    {
        base.OnUnequipped();
        isAiming = false;

        if (scopeOverlay != null)
            scopeOverlay.SetActive(false);
    }

    private System.Collections.IEnumerator PlayBoltActionRoutine()
    {
        yield return new WaitForSeconds(boltActionDelay);
        PlayOneShot(boltActionClip, boltActionVolume);
        boltActionRoutine = null;
    }

    private void UpdateScopeOverlay()
    {
        if (scopeOverlay != null)
            scopeOverlay.SetActive(isAiming && HasScopeAttachment());
    }

    private bool HasScopeAttachment()
    {
        return inventory != null && inventory.HasAttachment(AttachmentType.SniperScope);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (boltActionRoutine != null)
        {
            StopCoroutine(boltActionRoutine);
            boltActionRoutine = null;
        }

        if (scopeOverlay != null)
            scopeOverlay.SetActive(false);
    }
}
