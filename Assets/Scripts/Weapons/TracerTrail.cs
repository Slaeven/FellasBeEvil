using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TracerTrail : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Color startColor;
    private Color endColor;
    private float lifetime;

    public void Initialise(Vector3 start, Vector3 end, Color color, float width, float duration)
    {
        lifetime = Mathf.Max(0.01f, duration);

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width * 0.35f;

        Material material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = material;

        startColor = color;
        endColor = new Color(color.r, color.g, color.b, color.a * 0.15f);
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;

        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float timer = 0f;

        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / lifetime);

            lineRenderer.startColor = WithAlpha(startColor, startColor.a * alpha);
            lineRenderer.endColor = WithAlpha(endColor, endColor.a * alpha);

            yield return null;
        }

        Destroy(gameObject);
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
