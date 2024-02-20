using UnityEngine;

public class SymmetricReferential : ChainedReferential
{

    public Transform mirror;

    public SymmetricReferential(Transform mirror, Referential previous) : base(previous) {
        this.mirror = mirror;
    }

    public override Vector3 InverseTransformPoint(Vector3 point)
	{
		point = Mirror.MirrorPosition(point, mirror.position, mirror.right);
		point = base.InverseTransformPoint(point);
		return point;
    }

    public override Vector3 TransformPoint(Vector3 point)
	{
		return base.TransformPoint(point);
	}

    public override Vector3 InverseTransformVector(Vector3 vec)
	{
		vec = Mirror.MirrorVector(vec, mirror.right);
		vec = base.InverseTransformVector(vec);
		return vec;
	}

    public override Vector3 TransformVector(Vector3 vec)
	{
		return base.TransformVector(vec);
	}
    
}
