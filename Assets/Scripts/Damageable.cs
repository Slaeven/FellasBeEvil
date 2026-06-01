using TMPro;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField] private float health = 100f;
    [SerializeField] private TMP_Text damageStatsText;
    [SerializeField] private bool hideOnDeath = true;

    private int hitCount;
    private float totalDamageTaken;
    private float lastDamageTaken;

    public int HitCount => hitCount;
    public float TotalDamageTaken => totalDamageTaken;
    public float LastDamageTaken => lastDamageTaken;
    public float CurrentHealth => health;

    private void Start()
    {
        UpdateDamageStatsText();
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        hitCount++;
        lastDamageTaken = amount;
        totalDamageTaken += amount;

        Debug.Log($"{name} took {amount} damage. Health: {health}");
        UpdateDamageStatsText();

        if (health <= 0f)
        {
            Die();
        }
    }

    private void UpdateDamageStatsText()
    {
        if (damageStatsText == null)
            return;

        damageStatsText.text =
            $"{name}\n" +
            $"Hits: {hitCount}\n" +
            $"Last Damage: {lastDamageTaken:0.#}\n" +
            $"Total Damage: {totalDamageTaken:0.#}\n" +
            $"Health: {Mathf.Max(0f, health):0.#}";
    }

    private void Die()
    {
        Debug.Log($"{name} died.");

        if (hideOnDeath)
            gameObject.SetActive(false);
    }
}
