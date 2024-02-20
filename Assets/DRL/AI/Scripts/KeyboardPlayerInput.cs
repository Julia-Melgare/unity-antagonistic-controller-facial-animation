using System;
using UnityEngine;

public class KeyboardPlayerInput : GenericPlayerInput
{

    public KeyCode activateKey = KeyCode.P;

    public override float GetControlAxis()
    {
        var f = Input.mouseScrollDelta.y;
        CheckAction(f != 0f);
        return f;
    }

    public override bool GetJumpButton()
    {
        var k = Input.GetKey(KeyCode.Space);
        CheckAction(k);
        return k;
    }

    public override Vector2 GetMovementAxis()
    {
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");
        var v = new Vector2(horizontal, vertical);
        var m = v.magnitude;
		CheckAction(m > 0);
        if(m > 0) v /= m;
        return v;
    }

    public override bool GetPrimaryActionButton()
    {
        var b = Input.GetMouseButton(0);
        CheckAction(b);
        return b;
    }

    public override bool GetSecondaryActionButton()
    {
        var b = Input.GetMouseButton(1);
        CheckAction(b);
        return b;
    }

    public override bool GetInteractButton()
    {
        return Input.GetKey(KeyCode.E);
    }

    public override bool GetWalkingButton()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    public override Vector2 GetViewAxis()
    {
        float vertical = Input.GetAxisRaw("Mouse Y");
        float horizontal = Input.GetAxisRaw("Mouse X");
        var v = new Vector2(horizontal, vertical);
        CheckAction(v.magnitude > 0);
        return v;
    }

    private void CheckAction(bool action)
    {
        if (action) lastAction = Time.timeAsDouble;
    }

    [NonSerialized]
    public double lastAction;

	[NonSerialized]
	public bool escaped = false;

    public void FixedUpdate()
    {
        
    }

    public override bool IsActive()
    {
        if(Input.GetKey(activateKey))
        {
            escaped = true;
        }
        //CheckAction(Keyboard.current.anyKey.isPressed);
        return escaped;
    }

}