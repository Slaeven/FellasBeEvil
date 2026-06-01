using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private float trauma;
    private float shakeDuration;
    private float shakeTimer;
    private float currentIntensity;
    private Vector3 shakeOffset;

    public Vector3 CurrentOffset => shakeOffset;

    public void Shake(float intensity, float duration)
    {
        trauma = Mathf.Max(trauma, Mathf.Clamp01(intensity));
        currentIntensity = Mathf.Max(currentIntensity, intensity);
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
    }

    private void LateUpdate()
    {
        if (shakeTimer <= 0f)
        {
            shakeOffset = Vector3.zero;
            trauma = 0f;
            currentIntensity = 0f;
            return;
        }

        shakeTimer -= Time.deltaTime;

        float progress = shakeTimer / shakeDuration;
        float strength = trauma * trauma * progress * currentIntensity;

        shakeOffset = new Vector3(
            Random.Range(-strength, strength),
            Random.Range(-strength, strength),
            0f
        );

        if (shakeTimer <= 0f)
        {
            shakeOffset = Vector3.zero;
            trauma = 0f;
            currentIntensity = 0f;
        }
    }
}
