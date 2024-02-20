using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Normalizer
{
    public float scale;
    public float max;
    public Normalizer(float max)
    {
        this.max = 0;
        this.scale = 1f / max;
    }
    public float Normalize(float value)
    {
        max = Mathf.Max(max, Mathf.Abs(value));

        float normalized = value * scale;

        return normalized;
    }
}
