using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rig;

    [SerializeField] private LayerMask grabAbleLayer;
    [SerializeField] private float grabRadius = 1;
    [SerializeField] private float armSpan = 5;
    [SerializeField] private float climbSpeed = 5;

    private XRIDefaultInputActions inputActions;
    private InputAction leftHand;
    private InputAction rightHand;

    private bool leftHandGrabbed;
    private bool rightHandGrabbed;

    private Vector3 leftHandGrabPosition;
    private Vector3 rightHandGrabPosition;

    private Vector3 lastLeftHandOffset;
    private Vector3 lastRightHandOffset;

    private void OnEnable()
    {
        inputActions ??= new XRIDefaultInputActions();

        inputActions.XRILeftHandInteraction.Activate.performed += OnGrab;
        inputActions.XRIRightHandInteraction.Activate.performed += OnGrab;

        inputActions.XRILeftHandInteraction.Activate.canceled += OnDrop;
        inputActions.XRIRightHandInteraction.Activate.canceled += OnDrop;

        inputActions.XRILeftHandInteraction.Activate.Enable();
        inputActions.XRIRightHandInteraction.Activate.Enable();

        leftHand = inputActions.XRILeftHand.Position;
        rightHand = inputActions.XRIRightHand.Position;

        leftHand.Enable();
        rightHand.Enable();
    }

    private void OnDisable()
    {
        inputActions.XRILeftHandInteraction.Activate.performed -= OnGrab;
        inputActions.XRIRightHandInteraction.Activate.performed -= OnGrab;

        inputActions.XRILeftHandInteraction.Activate.canceled -= OnDrop;
        inputActions.XRIRightHandInteraction.Activate.canceled -= OnDrop;

        inputActions.XRILeftHandInteraction.Activate.Disable();
        inputActions.XRIRightHandInteraction.Activate.Disable();

        leftHand.Disable();
        rightHand.Disable();
    }

    void Start()
    {
        rig = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 velocity = Vector3.zero;

        if (leftHandGrabbed)
        {
            Vector3 leftHandOffset = leftHand.ReadValue<Vector3>();
            Vector3 dif = lastLeftHandOffset - leftHandOffset;
            velocity += dif;
            lastLeftHandOffset = leftHandOffset;
        }

        if (rightHandGrabbed)
        {
            Vector3 rightHandOffset = rightHand.ReadValue<Vector3>();
            Vector3 dif = lastRightHandOffset - rightHandOffset;
            velocity += dif;
            lastRightHandOffset = rightHandOffset;
        }

        if (leftHandGrabbed || rightHandGrabbed)
        {
            rig.velocity = velocity * climbSpeed;
        }
    }

    public void OnGrab(InputAction.CallbackContext obj)
    {
        if (IsLeftHand(obj.action.actionMap.name))
        {
            //Debug.Log("Left Hand Grabbed");
            leftHandGrabbed = true;
            leftHandGrabPosition = leftHand.ReadValue<Vector3>();
            lastLeftHandOffset = leftHandGrabPosition;
            leftHandGrabPosition += transform.position;
        } 
        else
        {
            //Debug.Log("Right Hand Grabbed");
            rightHandGrabbed = true;
            rightHandGrabPosition = rightHand.ReadValue<Vector3>();
            lastRightHandOffset = rightHandGrabPosition;
            rightHandGrabPosition += transform.position;
        }

        if (leftHandGrabbed || rightHandGrabbed)
        {
            rig.velocity = Vector3.zero;
            rig.useGravity = false;
        }
    }

    public void OnDrop(InputAction.CallbackContext obj)
    {
        if (IsLeftHand(obj.action.actionMap.name))
        {
            //Debug.Log("Left Hand Let Go");
            leftHandGrabbed = false;
            leftHandGrabPosition = Vector3.zero;
        }
        else
        {
            //Debug.Log("Right Hand Let Go");
            rightHandGrabbed = false;
            rightHandGrabPosition = Vector3.zero;
        }

        if (!leftHandGrabbed && !rightHandGrabbed)
        {
            rig.useGravity = true;
        }
    }

    public bool IsLeftHand(string actionMapName)
    {
        string handLetter = actionMapName.Substring(4, 1);
        return handLetter.ToLower() == "l";
    }
}