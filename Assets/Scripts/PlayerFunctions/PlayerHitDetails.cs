using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitDetails
{
    public Collider Collider;
    public Vector3  Position;
    public Vector3  Normal;
    public Vector3  Direction;
    public float    Force;

    public PlayerHitDetails(Collision collision, Vector3 direction, float force)
    {
        Collider  = collision.collider;
        Position  = collision.contacts[0].point;
        Normal    = collision.contacts[0].normal;
        Direction = direction;
        Force     = force;
    }

    public PlayerHitDetails(Collider collider, Vector3 position, Vector3 normal, Vector3 direction, float force)
    {
        Collider  = collider;
        Position  = position;
        Normal    = normal;
        Direction = direction;
        Force     = force;
    }
}
