using UnityEngine;

public class IdentityReferential : Referential
{
    public IdentityReferential(Transform root) : base(root) {}

    public override Vector3 InverseTransformPoint(Vector3 point)
    {
        return point;
    }

    public override Vector3 TransformPoint(Vector3 point)
    {
        return point;
    }

    public override Vector3 InverseTransformVector(Vector3 vec)
    {
        return vec;
    }

    public override Vector3 TransformVector(Vector3 vec)
    {
        return vec;
    }
    
}
