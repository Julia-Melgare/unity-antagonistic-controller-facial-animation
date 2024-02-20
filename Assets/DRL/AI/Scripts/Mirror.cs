using UnityEngine;
using UnityEngine.UIElements;

public class Mirror
{

	// Mirror position with respect to the normal of a plane
	public static Vector3 MirrorPosition(Vector3 position, Vector3 center, Vector3 normal)
	{
		position -= center;
		position = MirrorVector(position, normal);
		position += center;
		return position;
	}

	// Mirror vector with respect to the normal of a plane
	public static Vector3 MirrorVector(Vector3 vector, Vector3 normal) {
		/*
		// Get the distance from the plane
		float distance = Vector3.Dot(vector, normal);
		// Get the mirrored position
		Vector3 mirroredPosition = vector - 2 * distance * normal;
		// Return the mirrored position
		*/
		return Vector3.Reflect(vector, normal);
	}

	// Mirror rotation with respect to the normal of a plane
	public static Quaternion MirrorRotation(Quaternion rotation, Vector3 normal)
	{
		rotation.ToAngleAxis(out float angle, out Vector3 axis);
		// Get the mirrored rotation
		Quaternion mirroredRotation = Quaternion.AngleAxis(-angle, MirrorVector(axis, normal));
		// Return the mirrored rotation
		return mirroredRotation;
	}

}
