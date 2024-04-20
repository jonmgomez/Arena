using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRagdoll : MonoBehaviour
{
    [SerializeField] private GameObject ragdollModelPrefab;
    [SerializeField] private Transform normalModelReference;

    private Material currentMaterial;

    public void SpawnRagdoll(PlayerHitDetails hitDetails)
    {
        GameObject ragdoll = CreateRagdoll();

        Transform ragdollModelReference = ragdoll.transform.Find("Reference");
        if (ragdollModelReference == null)
        {
            Logger.Default.LogError("Ragdoll model reference not found");
            return;
        }

        Rigidbody rigidbodyToAddForce = null;
        Transform[] children = ragdollModelReference.GetComponentsInChildren<Transform>();
        Transform[] normalChildren = normalModelReference.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            Transform matchingPart = FindChildObject(normalChildren, child.name);
            if (matchingPart != null)
            {
                child.SetPositionAndRotation(matchingPart.position, matchingPart.rotation);
            }

            if (hitDetails != null && child.name == hitDetails.Collider.name)
            {
                rigidbodyToAddForce = child.GetComponent<Rigidbody>();
            }
        }

        if (rigidbodyToAddForce != null)
        {
            Vector3 forceDirection = hitDetails.Direction;
            rigidbodyToAddForce.AddForce(forceDirection.normalized * hitDetails.Force, ForceMode.Impulse);
        }
    }

    private Transform FindChildObject(Transform[] children, string name)
    {
        foreach (Transform child in children)
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private GameObject CreateRagdoll()
    {
        GameObject ragdoll = Instantiate(ragdollModelPrefab, transform.position, transform.rotation);

        Renderer[] renderers = ragdoll.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = currentMaterial;
        }

        return ragdoll;
    }

    public void SetMaterial(Material material)
    {
        currentMaterial = material;
    }
}
