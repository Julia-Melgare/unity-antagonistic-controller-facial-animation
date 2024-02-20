using UnityEngine;

public class TargetedReferential : Referential
{

    private Transform target;

    public TargetedReferential(Transform root, Transform target) : base(root)
    {
        this.target = target;
    }

}
