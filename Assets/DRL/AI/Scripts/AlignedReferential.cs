using UnityEngine;

public class AlignedReferential : Referential
{
    public AlignedReferential(Transform root) : base(root) {}

    public Matrix4x4 getTransformMatrix()
    {
        return Matrix4x4.TRS(root.position, root.rotation * Quaternion.FromToRotation(root.up, Vector3.up), root.localScale);
    }

    public override Vector3 InverseTransformPoint(Vector3 point)
    {
        return getTransformMatrix().inverse.MultiplyPoint(point);
    }

    public override Vector3 TransformPoint(Vector3 point)
    {
        return getTransformMatrix().MultiplyPoint(point);
    }

    public override Vector3 InverseTransformVector(Vector3 vec)
    {
        return getTransformMatrix().inverse.MultiplyVector(vec);
    }

    public override Vector3 TransformVector(Vector3 vec)
    {
        return getTransformMatrix().MultiplyVector(vec);
    }

}
