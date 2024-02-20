using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMover : MonoBehaviour
{

    public Vector3 randomRange(float size)
    {
        transform.position = new Vector3(Random.Range(-size, size), Random.Range(-size, size), Random.Range(-size, size));
        return transform.position;
    }

    public Vector3 randomRadius(Vector3 position, float minRadius, float maxRadius)
    {
        float radius = Random.Range(minRadius, maxRadius);
        return randomAngle(position, radius);
    }

    public Vector3 randomAngle(Vector3 position, float radius)
    {
        float angle = Random.Range(0f, 2 * Mathf.PI);
        transform.position = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius + position;
        return transform.position;
    }

}
