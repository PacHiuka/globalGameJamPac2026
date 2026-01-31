using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour
{

    //states
    private enum BodyState
    {
        Mask,
        Shadow,
    }

    //Variables
    [SerializeField] private bool lookRight;
    [SerializeField] private BodyState bodyState;

    [SerializeField] private ShadowController shadowController;



    void Start()
    {
    }

    void Update()
    {
    }

#region input receive Functions

    void OnMove(InputValue value)
    {
        if (bodyState == BodyState.Mask)
        {
            //move mask
        }
        if (bodyState == BodyState.Shadow)
        {
            shadowController.receiveMoveInput(value.Get<Vector2>());
        }
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            if (bodyState == BodyState.Mask)
            {
                //jump mask
            }
            if (bodyState == BodyState.Shadow)
            {
                shadowController.jumpInput();
            }
        }
    }

    void OnAction(InputValue value)
    {
        if (value.isPressed)
        {
            if (bodyState == BodyState.Mask)
            {
                //action mask
            }
            if (bodyState == BodyState.Shadow)
            {
                shadowController.actionInput();
            }
        }
    }
#endregion

}
