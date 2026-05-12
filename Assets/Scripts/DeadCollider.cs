using UnityEngine;

public class DeadCollider : MonoBehaviour
{
    [SerializeField] private float damageAmount = 1f;
    private CreatureBrainController brain;

    private void Awake()
    {
        brain = GetComponent<CreatureBrainController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
            brain?.TakeDamage(damageAmount);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
            brain?.TakeDamage(damageAmount);
    }
}
