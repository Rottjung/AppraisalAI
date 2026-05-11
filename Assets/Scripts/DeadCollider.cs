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
        Debug.Log($"[DeadCollider] Hit by {collision.collider.name} tag={collision.collider.tag}", this);
        if (collision.collider.CompareTag("Enemy"))
        {
            Debug.Log("[DeadCollider] Enemy detected, calling TakeDamage", this);
            brain?.TakeDamage(damageAmount);
        }
    }
}
