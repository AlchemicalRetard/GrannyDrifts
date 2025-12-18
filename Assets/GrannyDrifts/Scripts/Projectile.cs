using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 50f; // Faster for SMG
    public bool useGravity = false;
    
    [Header("Damage")]
    public float damage = 10f;
    public string[] damageableTags = { "Enemy", "Destructible" };
    
    [Header("Effects")]
    public ParticleSystem hitParticles;
    public AudioClip hitSound;
    public GameObject impactPrefab; // Optional impact effect prefab
    public TrailRenderer trail; // Bullet trail
    
    [Header("Physics")]
    public bool destroyOnCollision = true;
    public float lifetime = 3f; // Shorter for SMG bullets
    
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool hasHit = false;
    private Vector3 shootDirection;

    public void Initialize(Vector3 direction, Vector3 shooterVelocity)
    {
        shootDirection = direction.normalized;
        
        rb = GetComponent<Rigidbody>();
        
        // Setup Rigidbody if not present
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.isKinematic = false;
        rb.useGravity = useGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better for fast bullets
        
        // Set velocity: bullet speed + shooter's velocity (bike momentum)
        rb.velocity = shootDirection * speed + shooterVelocity;

        // Setup AudioSource for impact sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && hitSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    void Start()
    {
        // If Initialize wasn't called, use default forward direction
        if (shootDirection == Vector3.zero)
        {
            Initialize(transform.forward, Vector3.zero);
        }
        
        // DEBUG: Log projectile spawn
        Debug.Log($"Projectile spawned at {transform.position}, velocity: {rb.velocity.magnitude}");
        
        // DEBUG: Ensure projectile has a visible component
        if (GetComponent<Renderer>() == null)
        {
            Debug.LogWarning("Projectile has no Renderer! Adding a visual sphere...");
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.2f;
            Destroy(sphere.GetComponent<Collider>()); // Remove duplicate collider
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        HandleHit(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
    }

    void OnTriggerEnter(Collider collision)
    {
        if (hasHit) return;

        HandleHit(collision.gameObject, collision.ClosestPoint(transform.position), transform.forward);
    }

    void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        hasHit = true;

        // Deal damage if it's a damageable object
        DealDamage(hitObject);

        // Play impact effects
        PlayImpactEffects(hitPoint, hitNormal);

        // Destroy projectile
        if (destroyOnCollision)
        {
            Destroy(gameObject);
        }
    }

    void DealDamage(GameObject target)
    {
        // Check if target has a damageable tag
        foreach (string tag in damageableTags)
        {
            if (target.CompareTag(tag))
            {
                // Try to find IDamageable interface
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"Projectile dealt {damage} damage to {target.name}");
                }
                
                // Alternative: Try direct Health component
                Health health = target.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    Debug.Log($"Projectile dealt {damage} damage to {target.name}");
                }

                return;
            }
        }
    }

    void PlayImpactEffects(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Play particle effect
        if (hitParticles != null)
        {
            ParticleSystem particles = Instantiate(hitParticles, hitPoint, Quaternion.LookRotation(hitNormal));
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }

        // Play impact prefab
        if (impactPrefab != null)
        {
            Instantiate(impactPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        // Play sound
        if (hitSound != null && audioSource != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, hitPoint, 1f);
        }
    }

    // Optional: Add trail renderer for visual feedback
    void OnDrawGizmos()
    {
        // Draw trajectory line in editor
        if (Application.isPlaying) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
    }
}

// Interface for damaging objects
public interface IDamageable
{
    void TakeDamage(float damage);
}

// Example Health component (use this or implement IDamageable on your enemies)
public class Health : MonoBehaviour, IDamageable
{
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        Destroy(gameObject);
    }

    public float GetHealth()
    {
        return currentHealth;
    }
}
