using UnityEngine;

public class Events
{
	public delegate void GrabEvent(GameObject grabber, GameObject obj, PlayerType type);
	public static event GrabEvent pregrabEvent;
	public static event GrabEvent grabEvent;
	public static event GrabEvent ungrabEvent;
	public static void SignalGrab(GameObject grabber, GameObject obj, PlayerType type)
	{
		if (grabEvent != null) grabEvent(grabber, obj, type);
	}
	public static void SignalUnGrab(GameObject grabber, GameObject obj, PlayerType type)
	{
		if(ungrabEvent != null) ungrabEvent(grabber, obj, type);
	}
	public static void SignalPreGrab(GameObject grabber, GameObject obj, PlayerType type)
	{
		if (pregrabEvent != null) pregrabEvent(grabber, obj, type);
	}

	public delegate void DestroyEvent(GameObject obj);
	public static event DestroyEvent destroyEvent;
	public static void SignalDestroy(GameObject obj)
	{
		if (destroyEvent != null) destroyEvent(obj);
	}

	public enum CrimeType {
		attacking,
		breaking
	};

	public delegate void CrimeEvent(Vector3 position, GameObject crimer, GameObject crimee, CrimeType type, int level, float sound);
	public static event CrimeEvent crimeEvent;
	public static void SignalCrime(Vector3 position, GameObject crimer, GameObject crimee, CrimeType type, int level, float sound)
	{
		if (crimeEvent != null) crimeEvent(position, crimer, crimee, type, level, sound);
	}

}
