using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrannyController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject projectilePrefab;
    public Transform weaponFirePoint;
    public float fireRate = 0.5f;
    public float projectileSpeed = 20f;
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    
    [Header("Crosshair Settings")]
    public Image crosshairImage;
    public float crosshairDistance = 100f; // Distance from center
    public bool hideMouseCursor = true;
    
    [Header("Time Slow Settings")]
    public float slowTimeScale = 0.2f;
    public float normalTimeScale = 1f;
    public bool useFixedTimestep = true;
    
    [Header("Audio")]
    private AudioSource audioSource;
    
    private float nextFireTime = 0f;
    private bool isTimeSlowed = false;
    private Camera mainCamera;
    private RectTransform crosshairRect;

    void Start()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        
        // Add AudioSource if not present
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Setup crosshair
        if (crosshairImage != null)
        {
            crosshairRect = crosshairImage.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogWarning("GrannyController: Crosshair Image not assigned!");
        }

        // Hide cursor if enabled
        if (hideMouseCursor)
        {
            Cursor.visible = false;
        }
    }

    void Update()
    {
        UpdateCrosshair();
        UpdateWeaponAim();
        HandleShooting();
        HandleTimeControl();
    }

    void UpdateCrosshair()
    {
        if (crosshairRect == null) return;

        // Get mouse position
        Vector3 mousePos = Input.mousePosition;
        
        // Set crosshair position to follow mouse
        crosshairRect.position = mousePos;
    }

    void UpdateWeaponAim()
    {
        if (weaponFirePoint == null || mainCamera == null) return;

        // Raycast from camera through mouse position to find aim point
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        Vector3 targetPoint;
        
        // If we hit something, aim at that point
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            targetPoint = hit.point;
        }
        else
        {
            // If no hit, aim at a point far away in that direction
            targetPoint = ray.GetPoint(100f);
        }
        
        // Make weapon point towards target
        Vector3 direction = (targetPoint - weaponFirePoint.position).normalized;
        weaponFirePoint.rotation = Quaternion.LookRotation(direction);
    }

    void HandleShooting()
    {
        // Left Mouse Button - Shoot
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void HandleTimeControl()
    {
        // Right Mouse Button - Toggle Time Slow
        if (Input.GetButtonDown("Fire2"))
        {
            if (!isTimeSlowed)
            {
                SlowTime();
            }
            else
            {
                RestoreTime();
            }
        }
    }

    void Shoot()
    {
        // Play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Play shoot sound
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // Spawn projectile
        if (projectilePrefab != null && weaponFirePoint != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, weaponFirePoint.position, weaponFirePoint.rotation);
            
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                // Get bike's velocity from the scooter controller
                Vector3 bikeVelocity = Vector3.zero;
                ScooterController scooter = GetComponentInParent<ScooterController>();
                if (scooter != null && scooter.sphere != null)
                {
                    bikeVelocity = scooter.sphere.velocity;
                }
                
                // Initialize projectile with direction and bike velocity
                projectileScript.Initialize(weaponFirePoint.forward, bikeVelocity);
            }
            
            // Fallback if no projectile script
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null && projectileScript == null)
            {
                rb.velocity = weaponFirePoint.forward * projectileSpeed;
            }
        }
        else
        {
            // If no projectile prefab, do a raycast hit detection
            RaycastHit hit;
            if (Physics.Raycast(weaponFirePoint.position, weaponFirePoint.forward, out hit, 100f))
            {
                Debug.Log("Hit: " + hit.collider.name);
                
                // You can add damage logic here
                // Example: hit.collider.GetComponent<IDamageable>()?.TakeDamage(damage);
            }
        }
    }

    void SlowTime()
    {
        isTimeSlowed = true;
        Time.timeScale = slowTimeScale;
        
        if (useFixedTimestep)
        {
            Time.fixedDeltaTime = 0.02f * slowTimeScale;
        }
        
        Debug.Log("Time Slowed");
    }

    void RestoreTime()
    {
        isTimeSlowed = false;
        Time.timeScale = normalTimeScale;
        
        if (useFixedTimestep)
        {
            Time.fixedDeltaTime = 0.02f;
        }
        
        Debug.Log("Time Restored");
    }

    // Optional: Visualize weapon direction in editor
    void OnDrawGizmos()
    {
        if (weaponFirePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(weaponFirePoint.position, weaponFirePoint.forward * 2f);
        }
    }

    // Reset time scale when destroyed or disabled
    void OnDisable()
    {
        if (isTimeSlowed)
        {
            RestoreTime();
        }
        
        // Show cursor again
        Cursor.visible = true;
    }
}
