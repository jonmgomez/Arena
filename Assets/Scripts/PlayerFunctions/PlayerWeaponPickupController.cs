using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerWeaponPickupController : MonoBehaviour
{
    private const string PICKUP_TEXT = "Press E to pickup ";
    private TextMeshProUGUI pickupText;

    private PlayerWeapon playerWeapon;

    private readonly List<WeaponPickup> pickupsInRange = new();
    private WeaponPickup inRangePickup;

    private void Start()
    {
        Player player = GetComponent<Player>();
        playerWeapon = player.GetPlayerWeapon();
        pickupText = player.GetPlayerHUD().GetPickupWeaponPromptText();
        pickupText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (inRangePickup != null)
        {
            if (Input.GetKey(KeyCode.E))
            {
                playerWeapon.PickupWeapon(inRangePickup.GetWeaponId());
                inRangePickup.PickupWeapon();
                ChangeInRangePickup(null);
            }
        }
    }

    private void FixedUpdate()
    {
        CalculateClosestWeapon();
    }

    public void PickupInRange(WeaponPickup pickup)
    {
        pickupsInRange.Add(pickup);
    }

    public void PickupOutOfRange(WeaponPickup pickup)
    {
        pickupsInRange.Remove(pickup);
        if (pickup == inRangePickup)
        {
            ChangeInRangePickup(null);
        }
    }

    private void CalculateClosestWeapon()
    {
        WeaponPickup closestPickup = null;
        if (pickupsInRange.Count > 1)
        {
            float closestDistance = float.MaxValue;

            foreach (var pickup in pickupsInRange)
            {
                float distance = Vector3.Distance(transform.position, pickup.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPickup = pickup;
                }
            }
        }
        else if (pickupsInRange.Count == 1)
        {
            closestPickup = pickupsInRange[0];
        }
        else
        {
            closestPickup = null;
        }

        if (inRangePickup != closestPickup)
        {
            ChangeInRangePickup(closestPickup);
        }
    }

    private void ChangeInRangePickup(WeaponPickup pickup)
    {
        inRangePickup = pickup;

        if (inRangePickup != null)
        {
            pickupText.text = PICKUP_TEXT + playerWeapon.GetWeaponName(inRangePickup.GetWeaponId());
            pickupText.gameObject.SetActive(true);
        }
        else
        {
            pickupText.gameObject.SetActive(false);
        }
    }
}
