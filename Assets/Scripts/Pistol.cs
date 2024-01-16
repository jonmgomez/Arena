using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : NetworkBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] LayerMask layerMask;

    [SerializeField] float damage = 10f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            // GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            // bullet.GetComponent<NetworkObject>().Spawn(true);
            // bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 100;
            // Destroy(bullet, 2.0f);

            if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Debug.Log(hit.transform.name);
                hit.transform.GetComponent<Player>().TakeDamage(damage);
            }
        }
    }
}
