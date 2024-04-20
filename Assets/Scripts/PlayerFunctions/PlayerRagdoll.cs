using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRagdoll : MonoBehaviour
{
    [SerializeField] private GameObject ragdollModelRoot;
    [SerializeField] private Transform normalModelReference;

    private Material currentMaterial;

    public void SpawnRagdoll()
    {
        var ragdoll = Instantiate(ragdollModelRoot, transform.position, transform.rotation);

        Renderer[] renderers = ragdoll.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = currentMaterial;
        }

        Transform ragdollModelReference = ragdoll.transform.Find("Reference");
        Transform[] children = ragdollModelReference.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.isKinematic = false;
                child.GetComponent<Collider>().enabled = true;
            }

            Transform matchingPart = normalModelReference.Find(child.name);
            if (matchingPart != null)
            {
                child.SetPositionAndRotation(matchingPart.position, matchingPart.rotation);
            }
        }
    }

    public void SetMaterial(Material material)
    {
        currentMaterial = material;
    }
}
