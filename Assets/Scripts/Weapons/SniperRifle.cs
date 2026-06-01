using UnityEngine;

public class SniperRifle : WeaponBase
{
    [Header("Sniper Audio")]
    [SerializeField] private AudioClip boltActionClip;
    [SerializeField] private float boltActionDelay = 0.45f;
    [SerializeField, Range(0f, 1f)] private float boltActionVolume = 1f;

    [Header("Scope")]
    [SerializeField] private bool startScoped;
    [SerializeField] private float scopedFOV = 18f;
    [SerializeField] private GameObject scopeOverlay;

    private bool isScoped;
    private bool hasInitialisedScope;
    private Coroutine boltActionRoutine;

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
        tracerColor = new Color(1f, 0.92f, 0.45f, 0.8f);
        tracerWidth = 0.035f;
        tracerDuration = 0.12f;
        shakeIntensity = 0.14f;
        shakeDuration = 0.12f;
    }

    public override void Initialise(Camera camera, PlayerInventory playerInventory = null)
    {
        base.Initialise(camera, playerInventory);

        if (!hasInitialisedScope)
        {
            isScoped = startScoped;
            hasInitialisedScope = true;
        }

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
        isScoped = !isScoped;
        UpdateScopeOverlay();
    }

    [ContextMenu("Toggle Scope")]
    private void ToggleScopeFromInspector()
    {
        ToggleScope();
    }

    public override float GetAimFOV(float defaultAimFOV)
    {
        return isScoped ? scopedFOV : defaultAimFOV;
    }

    public override void OnEquipped()
    {
        UpdateScopeOverlay();
    }

    public override void OnUnequipped()
    {
        base.OnUnequipped();

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
            scopeOverlay.SetActive(isScoped);
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
