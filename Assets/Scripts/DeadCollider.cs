using System;
using UnityEngine;

public class DeadCollider : MonoBehaviour
{
    public Action OnDead;
    private CreatureBrainController brain;

    private void Awake()
    {
        brain = GetComponent<CreatureBrainController>();
    }

    private void OnEnable()
    {
        OnDead += brain.Die;
    }

    private void OnDisable()
    {
        OnDead -= brain.Die;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
            OnDead?.Invoke();
    }
}
