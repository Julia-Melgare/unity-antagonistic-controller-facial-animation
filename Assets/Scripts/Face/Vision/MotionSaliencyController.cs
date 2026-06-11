using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MotionSaliencyController : MonoBehaviour
{
    public abstract List<FixationObject> GetSalientObjects();
}
