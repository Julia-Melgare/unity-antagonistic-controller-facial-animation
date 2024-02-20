using UnityEngine;

public class Referential
{

    public Transform root;

    public Referential(Transform root)
    {
        this.root = root;
    }

    public virtual void Prepare()
    {

    }

    public void SetRoot(Transform root)
    {
        this.root = root;
    }

    public Transform GetRoot()
    {
        return root;
    }

    public virtual Vector3 InverseTransformPoint(Vector3 point)
    {
        return root.InverseTransformPoint(point);
    }

    public virtual Vector3 TransformPoint(Vector3 point)
    {
        return root.TransformPoint(point);
    }

    public virtual Vector3 InverseTransformVector(Vector3 vec)
    {
        return root.InverseTransformVector(vec);
    }

    public virtual Vector3 TransformVector(Vector3 vec)
    {
        return root.TransformVector(vec);
    }


}
