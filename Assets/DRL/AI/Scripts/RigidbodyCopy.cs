using UnityEngine;

public class RigidbodyCopy
{
    public Vector3 localPosition;
    public Vector3 velocity;
    public Quaternion localRotation;
    public Vector3 angularVelocity;
    public ArticulationReducedSpace jointPosition;
    public ArticulationReducedSpace jointVelocity;

    public RigidbodyCopy(Vector3 localPosition, Vector3 velocity, Quaternion localRotation, Vector3 angularVelocity, ArticulationReducedSpace jointPosition, ArticulationReducedSpace jointVelocity)
    {
        this.localPosition = localPosition;
        this.velocity = velocity;
        this.localRotation = localRotation;
        this.angularVelocity = angularVelocity;
        this.jointPosition = jointPosition;
        this.jointVelocity = jointVelocity;
    }

    public RigidbodyCopy(Transform tf, Rigidbody body) : this(tf.localPosition, body.velocity, tf.localRotation, body.angularVelocity, new ArticulationReducedSpace(), new ArticulationReducedSpace())
    {

    }

    public RigidbodyCopy(Transform tf, ArticulationBody body) : this(tf.localPosition, body.velocity, tf.localRotation, body.angularVelocity, body.jointPosition, body.jointVelocity)
    {

    }

    public void paste(Transform tf, Rigidbody body)
    {
        tf.localPosition = this.localPosition;
        tf.localRotation = this.localRotation;
        if(!body.isKinematic)
        {
			body.velocity = velocity;
			body.angularVelocity = angularVelocity;
		}
    }

    public void paste(Transform tf, ArticulationBody body)
    {

        tf.localPosition = this.localPosition;
        tf.localRotation = this.localRotation;
        body.velocity = velocity;
        body.angularVelocity = angularVelocity;

        body.jointPosition = jointPosition;
        body.jointVelocity = jointVelocity;
    }

};