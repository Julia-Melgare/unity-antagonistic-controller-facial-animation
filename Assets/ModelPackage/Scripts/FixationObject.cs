using System;
using UnityEngine;

[Serializable]
public class FixationObject : IEquatable<FixationObject>
{
    public GameObject gameObject; // Corresponding game object
    public float imageSaliencyScore = 0f;
    public float motionSaliencyScore = 0f;
    public int objectType;
    private Vector3 localPoint; // Local point in the game object where the fixation raycast hit

    public FixationObject(GameObject obj, Vector3 point)
    {
        gameObject = obj;
        localPoint = point;
    }

    public Vector3 GetFixationPoint()
    {
        return gameObject.transform.TransformPoint(localPoint);
    }

    public bool Equals(FixationObject other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return gameObject.GetInstanceID() == other.gameObject.GetInstanceID();
    }
}
