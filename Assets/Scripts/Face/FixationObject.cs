using System;
using UnityEngine;

[Serializable]
public class FixationObject : IEquatable<FixationObject>
{
    public GameObject gameObject;
    public float imageSaliencyScore = 0f;
    public float motionSaliencyScore = 0f;
    public float currentIOR = 0f;
    public float scoreBoost = 0f;
    //public float distance = 0f;

    //public float firstAppearTime = 0f;
    //public float lastObservedTime = 0f;
    //public float uncertainty = 0f;
    public Vector3 localPoint = Vector3.zero;

    public FixationObject(GameObject obj, Vector3 point, float imageSaliency = 0f, float motionSaliency = 0f)
    {
        gameObject = obj;
        localPoint = point;
        imageSaliencyScore = imageSaliency;
        motionSaliencyScore = motionSaliency;
        //firstAppearTime =  Time.time;
    }

    public float GetSaliencyScore()
    {
        return 0.5f * imageSaliencyScore + 0.5f * motionSaliencyScore + scoreBoost - currentIOR;
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
