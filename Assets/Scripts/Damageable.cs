using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField] private float health = 100f;

    public void TakeDamage(float amount)
    {
        health -= amount;

        Debug.Log($"{name} took {amount} damage. Health: {health}");

        if (health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died.");
        gameObject.SetActive(false);
    }
}