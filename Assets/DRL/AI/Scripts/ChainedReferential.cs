using System.Drawing;
using UnityEngine;

public class ChainedReferential : Referential
{

    public Referential previous;

    public ChainedReferential(Referential previous) : base(previous.GetRoot()) {
        this.previous = previous;
    }

    public override Vector3 InverseTransformPoint(Vector3 point)
    {
        return previous.InverseTransformPoint(point);
    }

    public override Vector3 TransformPoint(Vector3 point)
	{
		return previous.TransformPoint(point);
	}

    public override Vector3 InverseTransformVector(Vector3 vec)
	{
		return previous.InverseTransformVector(vec);
	}

    public override Vector3 TransformVector(Vector3 vec)
	{
		return previous.TransformVector(vec);
	}
    
}
