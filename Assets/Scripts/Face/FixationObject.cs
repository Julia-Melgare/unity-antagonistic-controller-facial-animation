using System;
using UnityEngine;

[Serializable]
public class FixationObject : IEquatable<FixationObject>
{
    public GameObject gameObject;
    private Vector3 localPoint;

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
