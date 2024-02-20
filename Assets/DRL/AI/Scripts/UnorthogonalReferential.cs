using UnityEngine;

public class UnorthogonalReferential : Referential
{
    public UnorthogonalReferential(Transform root) : base(root) {}
    
    public Vector3 Project(Vector3 vec)
    {
        return new Vector3(Vector3.Dot(root.forward, vec), Vector3.Dot(Vector3.up, vec), Vector3.Dot(Vector3.Cross(root.forward, Vector3.up).normalized, vec));
    }

    public override Vector3 InverseTransformPoint(Vector3 point)
    {
        return Project(point-root.position);
    }

    public override Vector3 TransformPoint(Vector3 point)
    {
        throw new UnityException("Not implemented");
    }

    public override Vector3 InverseTransformVector(Vector3 vec)
    {
        return Project(vec);
    }

    public override Vector3 TransformVector(Vector3 vec)
    {
        throw new UnityException("Not implemented");
    }

}
