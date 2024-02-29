using UnityEngine;

public static class ConfigurableJointExtensions {

	public static void SetTargetRotationWorld(this ConfigurableJoint cj, Quaternion startRot, Quaternion target, Space space)
	{
		Vector3 right = cj.axis;
		Vector3 forward = Vector3.Cross(cj.axis, cj.secondaryAxis).normalized;
		Vector3 up = Vector3.Cross(forward, right).normalized;
		Quaternion localToJointSpace = Quaternion.LookRotation(forward, up);
		if (space == Space.World)
		{
			Quaternion worldToLocal = Quaternion.Inverse(cj.transform.parent.rotation);
			target = worldToLocal * target;
		}
		cj.targetRotation = Quaternion.Inverse(localToJointSpace) * Quaternion.Inverse(target) * startRot * localToJointSpace;
	}

	/// <summary>
	/// Sets a joint's targetRotation to match a given local rotation.
	/// The joint transform's local rotation must be cached on Start and passed into this method.
	/// </summary>
	public static void SetTargetRotationLocal (this ConfigurableJoint joint, Quaternion targetLocalRotation, Quaternion startLocalRotation)
	{
		if (joint.configuredInWorldSpace) {
			Debug.LogError ("SetTargetRotationLocal should not be used with joints that are configured in world space. For world space joints, use SetTargetRotation.", joint);
		}
		SetTargetRotationInternal (joint, targetLocalRotation, startLocalRotation, Space.Self);
	}

	public static Quaternion GetTargetRotationLocal(this ConfigurableJoint joint, Quaternion targetLocalRotation, Quaternion startLocalRotation)
	{
		if (joint.configuredInWorldSpace)
		{
			Debug.LogError("GetTargetRotationLocal should not be used with joints that are configured in world space. For world space joints, use SetTargetRotation.", joint);
		}
		return GetTargetRotationInternal(joint, targetLocalRotation, startLocalRotation, Space.Self);
	}

	/// <summary>
	/// Sets a joint's targetRotation to match a given world rotation.
	/// The joint transform's world rotation must be cached on Start and passed into this method.
	/// </summary>
	public static void SetTargetRotation (this ConfigurableJoint joint, Quaternion targetWorldRotation, Quaternion startWorldRotation)
	{
		if (!joint.configuredInWorldSpace) {
			Debug.LogError ("SetTargetRotation must be used with joints that are configured in world space. For local space joints, use SetTargetRotationLocal.", joint);
		}
		SetTargetRotationInternal (joint, targetWorldRotation, startWorldRotation, Space.World);
	}

	public static Quaternion GetTargetRotation(this ConfigurableJoint joint, Quaternion targetWorldRotation, Quaternion startWorldRotation)
	{
		if (!joint.configuredInWorldSpace)
		{
			Debug.LogError("SetTargetRotation must be used with joints that are configured in world space. For local space joints, use SetTargetRotationLocal.", joint);
		}
		return GetTargetRotationInternal(joint, targetWorldRotation, startWorldRotation, Space.World);
	}

	static Quaternion GetTargetRotationInternal(ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation, Space space)
	{
		// Calculate the rotation expressed by the joint's axis and secondary axis
		var right = joint.axis;
		var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
		var up = Vector3.Cross(forward, right).normalized;
		Quaternion worldToJointSpace = Quaternion.LookRotation(forward, up);

		// Transform into world space
		Quaternion resultRotation = Quaternion.Inverse(worldToJointSpace);

		// Counter-rotate and apply the new local rotation.
		// Joint space is the inverse of world space, so we need to invert our value
		if (space == Space.World)
		{
			resultRotation *= startRotation * Quaternion.Inverse(targetRotation);
		}
		else
		{
			resultRotation *= Quaternion.Inverse(targetRotation) * startRotation;
		}

		// Transform back into joint space
		resultRotation *= worldToJointSpace;

		// Return target rotation to our newly calculated rotation
		return resultRotation;
	}

	static void SetTargetRotationInternal (ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation, Space space)
	{
		// Set target rotation to our newly calculated rotation
		joint.targetRotation = GetTargetRotationInternal(joint, targetRotation, startRotation, space);
	}

	public static Vector3 GetTargetAngularVelocityToTargetRotation(this ConfigurableJoint joint, Quaternion startLocalRotation)
	{
		var targetRotation = joint.targetRotation;

		joint.SetTargetRotationLocal(joint.transform.localRotation, startLocalRotation);

		var currentRotation = joint.targetRotation;

		joint.targetRotation = targetRotation;

		Quaternion rot = Quaternion.Inverse(currentRotation) * targetRotation;

		rot.ToAngleAxis(out float angle, out Vector3 axis);

		Vector3 error = angle * Mathf.Deg2Rad * axis.normalized / Time.fixedDeltaTime;

		//Vector3 error = targetRotation.eulerAngles - currentRotation.eulerAngles;

		return error;
	}

	public static Vector3 GetTargetAngularVelocity(this ConfigurableJoint joint, Quaternion rotation)
    {
        Quaternion rot = Quaternion.Inverse(joint.transform.localRotation) * rotation;

        /*
        if (Quaternion.Dot(a, b) < 0)

        {

            return a * Quaternion.Inverse(Multiply(b, -1));

        }

        else return a * Quaternion.Inverse(b);

        if (rot.w < 0)
        {
            rot.x = -rot.x;
            rot.y = -rot.y;
            rot.z = -rot.z;
            rot.w = -rot.w;
        }
		*/


        rot.ToAngleAxis(out float angle, out Vector3 axis);

        Vector3 error = angle * Mathf.Deg2Rad * axis.normalized;
		
        return error;
    }

}