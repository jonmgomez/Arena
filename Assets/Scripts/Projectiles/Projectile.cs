using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private const float SPEED_FOR_MISSED_COLLISIONS = 100f;

    [SerializeField] private float damage = 10f;
    [SerializeField] private float headShotDamageMultiplier = 2f;
    [SerializeField] private float speed = 100f;
    [SerializeField] private LayerMask collisionMask;

    [Header("Hit Scan Settings")]
    [SerializeField] private bool hitScan = false;
    [Tooltip("Hitscan only --- Originates the projectile from the barrel of the gun rather than the camera." +
             "Note that hitscan will still act as originating from the camera. A render thing only.")]
    [SerializeField] private bool startFromBarrel = false;
    [SerializeField] private float maxDistance = 100f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem metalImpactEffect;
    [SerializeField] private ParticleSystem humanImpactEffect;

    [Header("Debug")]
    [SerializeField] private bool showHitScanRay = false;

    private Vector3 originalPosition;
    private Vector3 previousPosition;

    private bool calculateCollisions = true;
    private ulong firedFromClientId; // Who shot this projectile

    private Destroy destroyTimer;
    private bool destroyed = false;

    void Start()
    {
        destroyTimer = gameObject.AddComponent<Destroy>();
        destroyTimer.SetDestroyTimer(maxDistance / speed);

        if (hitScan)
        {
            Vector3 startPosition = startFromBarrel ? originalPosition : transform.position;
            bool hitCollider = Physics.Raycast(startPosition, transform.forward, out RaycastHit hit, maxDistance, collisionMask);
            if (hitCollider)
            {
                OnCollision(hit.collider, hit);
                destroyTimer.SetDestroyTimer(Vector3.Distance(transform.position, hit.point) / speed);
            }

            if (startFromBarrel)
            {
                if (hitCollider)
                    transform.LookAt(hit.point);
                else
                    transform.LookAt(transform.position + transform.forward * maxDistance);
            }

            if (showHitScanRay)
            {
                if (hitCollider)
                    Debug.DrawLine(startPosition, hit.point, Color.red, 5f);
                else
                    Debug.DrawRay(startPosition, transform.forward * maxDistance, Color.red, 5f);
            }
        }
        else
        {
            previousPosition = transform.position;

            #if UNITY_EDITOR
            if (GetComponent<Rigidbody>() == null)
            {
                Logger.Default.LogError("Non-hitscan projectiles require a rigidbody to collide properly.");
                Debug.Break();
            }
            #endif

            GetComponent<Rigidbody>().velocity = transform.forward * speed;
        }
    }

    void Update()
    {
        if (hitScan)
        {
            MoveForward();
        }
        else
        {
            CheckForMissedCollisions();

            previousPosition = transform.position;
        }
    }

    private void MoveForward() => transform.position += transform.forward * (speed * Time.deltaTime);

    public virtual void OnCollisionEnter(Collision collision)
    {
        OnCollision(collision.collider, collision);
    }

    /// <summary>
    /// Projectiles that move fast enough can miss collisions between frames.
    /// This checks for collisions missed by raycasting between the previous position and the current position.
    /// </summary>
    private void CheckForMissedCollisions()
    {
        if (speed < SPEED_FOR_MISSED_COLLISIONS) return;

        Debug.DrawLine(previousPosition, transform.position, Utility.RandomColor(), 5f);

        if (Physics.Raycast(previousPosition, transform.forward, out RaycastHit hit,
                            Vector3.Distance(previousPosition, transform.position),
                            collisionMask))
        {
            Debug.Log("Missed collision detected");
            OnCollision(hit.collider, hit);
        }
    }

    private void OnCollision(Collider collider, Collision collision)
    {
        OnCollision(collider, collision.GetContact(0).point, collision.GetContact(0).normal);
    }

    private void OnCollision(Collider collider, RaycastHit hit)
    {
        OnCollision(collider, hit.point, hit.normal);
    }

    private void OnCollision(Collider collider, Vector3 hitPoint, Vector3 normal)
    {
        if (destroyed) return; // Prevent multiple collisions

        if (collider.transform.root.CompareTag("Player"))
        {
            Player player = collider.transform.root.GetComponent<Player>();

            if (!BelongsToPlayer(player)) // Don't deal damage to self
            {
                if (calculateCollisions)
                {
                    Debug.Log("Hit player collider: " + collider.name);
                    bool headShot = player.IsHeadCollider(collider);

                    float damageToDeal = headShot ? damage * headShotDamageMultiplier : damage;
                    player.TakeDamage(damageToDeal, firedFromClientId);

                    Player ownerPlayer = GameState.Instance.GetPlayer(firedFromClientId);
                    if (ownerPlayer == null)
                    {
                        Logger.Default.LogError($"Player {firedFromClientId} not found");
                        return;
                    }

                    ownerPlayer.DealtDamage(player, damageToDeal, headShot);
                }

                var hitEffect = Instantiate(humanImpactEffect, hitPoint, quaternion.identity);
                hitEffect.transform.LookAt(hitPoint + normal);
            }
        }
        else
        {
            var hitEffect = Instantiate(metalImpactEffect, hitPoint, quaternion.identity);
            hitEffect.transform.LookAt(hitPoint + normal);
        }

        if (!hitScan)
        {
            // This may need to be done over the network by the player who shot rather than locally for clients.
            // Clients need to do more processing to determine if they hit a player and destroy this way,
            // but it seems responsive enough for the time being.
            Destroy(gameObject);
            destroyed = true;
        }
    }

    private bool BelongsToPlayer(Player player) => player.OwnerClientId == firedFromClientId;

    public void SetFiredFromClient(bool isServer, bool isHost, ulong clientId)
    {
        calculateCollisions = (Net.IsServerOnly) || clientId == NetworkManager.Singleton.LocalClientId;
        firedFromClientId = clientId;
    }

    public void SetSpawnDetails(Vector3 spawn, Vector3 muzzlePosition)
    {
        originalPosition = spawn;
        if (startFromBarrel)
        {
            transform.position = muzzlePosition;
        }
    }
}
