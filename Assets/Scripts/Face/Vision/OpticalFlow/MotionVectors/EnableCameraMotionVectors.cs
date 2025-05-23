using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class EnableCameraMotionVectors : MonoBehaviour
{
    private Camera m_Camera;
    void Awake()
    {
		m_Camera = GetComponent<Camera>();
		m_Camera.depthTextureMode |= DepthTextureMode.MotionVectors;
	}
}
