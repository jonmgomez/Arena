using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    const float SPEED_FOR_MISSED_COLLISIONS = 100f;

    [SerializeField] float damage = 10f;
    [SerializeField] float speed = 100f;
    [SerializeField] bool hitScan = false;
    [Tooltip("Hitscan only --- Originates the projectile from the barrel of the gun rather than the camera." +
             "Note that hitscan will still act as originating from the camera. A render thing only.")]
    [SerializeField] bool startFromBarrel = false;
    Vector3 originalPosition;
    [SerializeField] float maxDistance = 100f;
    Vector3 previousPosition;

    [Header("Debug")]
    [SerializeField] bool showHitScanRay = false;

    bool calculateCollisions = true;
    ulong firedFromClientId; // Who shot this projectile

    Destroy destroyTimer;

    void Start()
    {
        destroyTimer = gameObject.AddComponent<Destroy>();
        destroyTimer.SetDestroyTimer(maxDistance / speed);

        if (hitScan)
        {
            Vector3 startPosition = startFromBarrel ? originalPosition : transform.position;
            if (Physics.Raycast(startPosition, transform.forward, out RaycastHit hit, maxDistance))
            {
                OnCollision(hit.collider);
                destroyTimer.SetDestroyTimer(Vector3.Distance(transform.position, hit.point) / speed);

                if (startFromBarrel)
                    transform.LookAt(hit.point);
            }
            else if (startFromBarrel)
            {
                transform.LookAt(transform.position + transform.forward * maxDistance);
            }

            if (showHitScanRay)
            {
                Debug.DrawRay(startPosition, transform.forward * maxDistance, Color.red, 5f);
            }
        }
        else
        {
            previousPosition = transform.position;
        }
    }

    void Update()
    {
        MoveForward();

        if (!hitScan)
        {
            CheckForMissedCollisions();

            previousPosition = transform.position;
        }
    }

    private void MoveForward() => transform.position += transform.forward * (speed * Time.deltaTime);

    public virtual void OnTriggerEnter(Collider other)
    {
        OnCollision(other);
    }

    /// <summary>
    /// Projectiles that move fast enough can miss collisions between frames.
    /// This checks for collisions missed by raycasting between the previous position and the current position.
    /// </summary>
    private void CheckForMissedCollisions()
    {
        if (speed < SPEED_FOR_MISSED_COLLISIONS) return;

        if (Physics.Raycast(previousPosition, transform.forward, out RaycastHit hit, Vector3.Distance(previousPosition, transform.position)))
        {
            OnCollision(hit.collider);
        }
        Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(transform.position - previousPosition), 180f);
    }

    private void OnCollision(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            Player player = collider.GetComponent<Player>();

            if (!BelongsToPlayer(player)) // Don't deal damage to self
            {
                if (calculateCollisions)
                    player.TakeDamage(damage, firedFromClientId);

                // This may need to be done over the network by the player who shot rather than locally for clients.
                // Clients need to do more processing to determine if they hit a player and destroy this way,
                // but it seems responsive enough for the time being.
                Destroy(gameObject);
            }
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
