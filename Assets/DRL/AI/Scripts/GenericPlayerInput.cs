using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenericPlayerInput : MonoBehaviour
{

    public static GenericPlayerInput GetInput(GameObject gameObject)
    {
        GenericPlayerInput input = gameObject.GetComponent<GenericPlayerInput>();
        while(input == null)
        {
            if (gameObject.transform.parent == null) return null;
            gameObject = gameObject.transform.parent.gameObject;
            input = gameObject.GetComponent<GenericPlayerInput>();
        }
        return input;
    }

    public abstract Vector2 GetMovementAxis();
    public abstract Vector2 GetViewAxis();
    public abstract bool GetJumpButton();
    public abstract bool GetPrimaryActionButton();
    public abstract bool GetSecondaryActionButton();
    public abstract bool GetInteractButton();

    public abstract bool GetWalkingButton();
    public abstract float GetControlAxis();

    public abstract bool IsActive();


}
