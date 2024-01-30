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
    [SerializeField] float maxDistance = 100f;
    Vector3 previousPosition;

    bool calculateCollisions = true;
    ulong firedFromClientId; // Who shot this projectile

    Destroy destroyTimer;

    void Start()
    {
        destroyTimer = gameObject.AddComponent<Destroy>();
        destroyTimer.SetDestroyTimer(maxDistance / speed);

        if (hitScan)
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, maxDistance))
            {
                OnCollision(hit.collider);
                destroyTimer.SetDestroyTimer(Vector3.Distance(transform.position, hit.point) / speed);
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
        calculateCollisions = (isServer && !isHost) || clientId == NetworkManager.Singleton.LocalClientId;
        firedFromClientId = clientId;
    }
}
