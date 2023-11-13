using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class PlayerController : Singleton<PlayerController>
{
    private Rigidbody rig;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private CapsuleCollider capCollider;

    [SerializeField] private Transform leftGrabSphere;
    [SerializeField] private Transform rightGrabSphere;

    [Header("Moving")]
    [SerializeField] private float maxSpeed = 10;
    [SerializeField] private float timeToAccelerate = 0.5f;
    [SerializeField] private float turnSpeed = 4;

    [Header("Climbing")]
    [SerializeField] private LayerMask grabAbleLayer;
    [SerializeField] private float grabRadius = 1;
    [SerializeField] private float armSpan = 5;
    [SerializeField] private float climbSpeed = 5;

    [Header("Jumping")]
    [SerializeField] private TrajectoryLine leftTrajectoryLine;
    [SerializeField] private TrajectoryLine rightTrajectoryLine;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float jumpGravityMultiplier;

    [Space(10)]
    [Header("Trajectory Line Smoothness/Length")]
    [SerializeField] private int segmentCount = 50;
    [SerializeField] private float curveLength = 3.5f;

    [Space(10)]
    [Header("Grounded Checks")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundedCheckDistance = 1.5f;

    private XRIDefaultInputActions inputActions;
    private InputAction leftHand;
    private InputAction rightHand;

    private InputAction leftJoystick;
    private InputAction rightJoystick;

    private bool leftHandGrabbed;
    private bool rightHandGrabbed;

    private bool leftHandAiming;
    private bool rightHandAiming;

    private Vector3 leftHandGrabPosition;
    private Vector3 rightHandGrabPosition;

    private Vector3 lastLeftHandOffset;
    private Vector3 lastRightHandOffset;

    private bool isJumping;
    private Vector3[] jumpTrajectory;
    private int jumpTrajectoryEndIndex;
    private Vector3 jumpTrajectoryEndPoint;

    private bool isGrounded;

    public LayerMask GrabAbleLayer {  get { return grabAbleLayer; } }
    public float JumpSpeed { get { return jumpSpeed; } }
    public float JumpGravityMultiplier { get {  return jumpGravityMultiplier; } }

    public int SegmentCount { get {  return segmentCount; } }
    public float CurveLength { get {  return curveLength; } }

    private void OnEnable()
    {
        inputActions ??= new XRIDefaultInputActions();

        inputActions.XRILeftHandInteraction.Activate.performed += OnGrab;
        inputActions.XRIRightHandInteraction.Activate.performed += OnGrab;

        inputActions.XRILeftHandInteraction.Activate.canceled += OnDrop;
        inputActions.XRIRightHandInteraction.Activate.canceled += OnDrop;

        inputActions.XRILeftHandInteraction.Activate.Enable();
        inputActions.XRIRightHandInteraction.Activate.Enable();

        inputActions.XRILeftHandInteraction.Select.performed += OnJumpAim;
        inputActions.XRIRightHandInteraction.Select.performed += OnJumpAim;

        inputActions.XRILeftHandInteraction.Select.canceled += OnJump;
        inputActions.XRIRightHandInteraction.Select.canceled += OnJump;

        inputActions.XRILeftHandInteraction.Select.Enable();
        inputActions.XRIRightHandInteraction.Select.Enable();

        leftHand = inputActions.XRILeftHand.Position;
        rightHand = inputActions.XRIRightHand.Position;

        leftHand.Enable();
        rightHand.Enable();

        leftJoystick = inputActions.XRILeftHandLocomotion.Move;
        rightJoystick = inputActions.XRIRightHandLocomotion.Turn;

        leftJoystick.Enable();
        rightJoystick.Enable();
    }

    private void OnDisable()
    {
        inputActions.XRILeftHandInteraction.Activate.performed -= OnGrab;
        inputActions.XRIRightHandInteraction.Activate.performed -= OnGrab;

        inputActions.XRILeftHandInteraction.Activate.canceled -= OnDrop;
        inputActions.XRIRightHandInteraction.Activate.canceled -= OnDrop;

        inputActions.XRILeftHandInteraction.Activate.Disable();
        inputActions.XRIRightHandInteraction.Activate.Disable();

        inputActions.XRILeftHandInteraction.Select.performed -= OnJumpAim;
        inputActions.XRIRightHandInteraction.Select.performed -= OnJumpAim;

        inputActions.XRILeftHandInteraction.Select.canceled -= OnJump;
        inputActions.XRIRightHandInteraction.Select.canceled -= OnJump;

        inputActions.XRILeftHandInteraction.Select.Disable();
        inputActions.XRIRightHandInteraction.Select.Disable();

        leftHand.Disable();
        rightHand.Disable();

        leftJoystick.Disable();
        rightJoystick.Disable();
    }

    void Start()
    {
        rig = GetComponent<Rigidbody>();
        jumpTrajectory = new Vector3[50];
    }

    private void FixedUpdate()
    {
        //leftGrabSphere.position = transform.position + transform.rotation * leftHand.ReadValue<Vector3>() + new Vector3(0, 1.15f, 0);
        //rightGrabSphere.position = transform.position + transform.rotation * rightHand.ReadValue<Vector3>() + new Vector3(0, 1.15f, 0);

        CheckIfGrounded();
        //Debug.Log($"IsGrounded : {isGrounded}");

        if (isJumping)
        {
            return;
        }

        if (leftHandGrabbed || rightHandGrabbed)
        {
            Vector3 climbVelocity = Vector3.zero;

            if (leftHandGrabbed)
            {
                Vector3 leftHandOffset = leftHand.ReadValue<Vector3>();
                Vector3 dif = lastLeftHandOffset - leftHandOffset;
                float difMag = dif.magnitude;
                climbVelocity += transform.rotation * dif.normalized * difMag;
                lastLeftHandOffset = leftHandOffset;
            }

            if (rightHandGrabbed)
            {
                Vector3 rightHandOffset = rightHand.ReadValue<Vector3>();
                Vector3 dif = lastRightHandOffset - rightHandOffset;
                float difMag = dif.magnitude;
                climbVelocity += transform.rotation * dif.normalized * difMag;
                lastRightHandOffset = rightHandOffset;
            }

            if (leftHandGrabbed || rightHandGrabbed)
            {
                rig.velocity = climbVelocity * climbSpeed;
            }
        }

        Vector2 turnInput = rightJoystick.ReadValue<Vector2>();

        float turnAmount = turnInput.x * turnSpeed * Time.fixedDeltaTime;

        transform.Rotate(Vector3.up, turnAmount);

        if (leftHandGrabbed || rightHandGrabbed)
        {
            return;
        }

        Vector2 movementInput = leftJoystick.ReadValue<Vector2>();

        Vector3 desiredVelocity = (playerTransform.forward * movementInput.y + playerTransform.right * movementInput.x) * maxSpeed;

        Vector3 velocityDiff = desiredVelocity - new Vector3(rig.velocity.x, 0, rig.velocity.z);
        Vector3 diffDirection = velocityDiff.normalized;

        Vector3 velocity = rig.velocity;
        velocity.y = 0;

        if (velocity.magnitude < desiredVelocity.magnitude)
        {
            float accelerationIncrement = GetAcceleration(maxSpeed, timeToAccelerate) * Time.deltaTime;

            if (velocityDiff.magnitude < accelerationIncrement)
            {
                velocity = desiredVelocity;
            }
            else
            {
                velocity += diffDirection * accelerationIncrement;
            }

        }
        else if (velocity.magnitude > desiredVelocity.magnitude)
        {
            float deaccelerationIncrement = GetAcceleration(maxSpeed, timeToAccelerate) * Time.deltaTime;

            if (velocityDiff.magnitude < deaccelerationIncrement)
            {
                velocity = desiredVelocity;
            }
            else
            {
                velocity += diffDirection * deaccelerationIncrement;
            }
        }

        rig.velocity = new Vector3(velocity.x, rig.velocity.y, velocity.z);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
        {
            SceneManager.Instance.ResetScene();
        }
    }

    public bool CheckIfGrounded()
    {
        isGrounded = (Physics.Raycast(playerTransform.position, Vector3.down, groundedCheckDistance, groundMask, QueryTriggerInteraction.Ignore));

        return isGrounded;
    }

    public void OnGrab(InputAction.CallbackContext obj)
    {
        if (IsLeftHand(obj.action.actionMap.name))
        {
            //Debug.Log("Left Hand Grabbed");
            if (leftHandAiming)
            {
                StopJumpAiming(true);
            }

            if (!Physics.CheckSphere(transform.position + transform.rotation * leftHand.ReadValue<Vector3>() + new Vector3(0, 1.15f, 0), grabRadius, grabAbleLayer, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            if (isJumping)
            {
                CancelJump();
            }

            leftHandGrabbed = true;
            leftHandGrabPosition = leftHand.ReadValue<Vector3>();
            lastLeftHandOffset = leftHandGrabPosition;
            leftHandGrabPosition += transform.position;
        } 
        else
        {
            //Debug.Log("Right Hand Grabbed");
            if (rightHandAiming)
            {
                StopJumpAiming(false);
            }

            if (!Physics.CheckSphere(transform.position + transform.rotation * rightHand.ReadValue<Vector3>() + new Vector3(0, 1.15f, 0), grabRadius, grabAbleLayer, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            if (isJumping)
            {
                CancelJump();
            }

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
            if (!leftHandGrabbed)
            {
                return;
            }

            leftHandGrabbed = false;
            leftHandGrabPosition = Vector3.zero;
        }
        else
        {
            //Debug.Log("Right Hand Let Go");
            if (!rightHandGrabbed)
            {
                return;
            }

            rightHandGrabbed = false;
            rightHandGrabPosition = Vector3.zero;
        }

        if (!leftHandGrabbed && !rightHandGrabbed)
        {
            rig.useGravity = true;
        }
    }

    public void DropAll()
    {
        leftHandGrabbed = false;
        leftHandGrabPosition = Vector3.zero;

        rightHandGrabbed = false;
        rightHandGrabPosition = Vector3.zero;

        rig.useGravity = true;
    }

    public void OnJumpAim(InputAction.CallbackContext obj)
    {
        if (IsLeftHand(obj.action.actionMap.name))
        {
            //Debug.Log("Left Hand Jump Aimed");
            if (leftHandGrabbed)
            {
                return;
            }

            leftHandAiming = true;
            leftTrajectoryLine.Activate(true);
        }
        else
        {
            //Debug.Log("Right Hand Jump Aimed");
            if (rightHandGrabbed)
            {
                return;
            }

            rightHandAiming = true;
            rightTrajectoryLine.Activate(true);
        }
    }

    public void OnJump(InputAction.CallbackContext obj)
    {
        if (isJumping || (!(leftHandGrabbed || rightHandGrabbed) && !isGrounded))
        {
            if (IsLeftHand(obj.action.actionMap.name))
            {
                if (leftHandAiming)
                {
                    StopJumpAiming(true);
                }
            }
            else
            {
                if (rightHandAiming)
                {
                    StopJumpAiming(false);
                }
            }
            return;
        }

        if (IsLeftHand(obj.action.actionMap.name))
        {
            //Debug.Log("Left Hand Jumped");
            if (!leftHandAiming)
            {
                return;
            }

            leftHandAiming = false;
            isJumping = true;
            Jump(leftTrajectoryLine);
            leftTrajectoryLine.Activate(false);

            if (isJumping)
            {
                DropAll();
            }
        }
        else
        {
            //Debug.Log("Right Hand Jumped");
            if (!rightHandAiming)
            {
                return;
            }

            rightHandAiming = false;
            isJumping = true;
            Jump(rightTrajectoryLine);
            rightTrajectoryLine.Activate(false);

            if (isJumping)
            {
                DropAll();
            }
        }
    }

    public void Jump(TrajectoryLine trajectoryLine)
    {
        //Debug.Log("Jumping");

        jumpTrajectory = CopyArray(jumpTrajectory, trajectoryLine.Segments);
        jumpTrajectoryEndIndex = trajectoryLine.SegmentEndIndex;
        jumpTrajectoryEndPoint = trajectoryLine.SegmentEndPoint;

        rig.useGravity = false;
        capCollider.enabled = false;

        StartCoroutine(JumpCoroutine());
    }

    public void StopJumpAiming(bool isLeftHand)
    {
        if (isLeftHand)
        {
            leftHandAiming = false;
            leftTrajectoryLine.Activate(false);
        }
        else
        {
            rightHandAiming = false;
            rightTrajectoryLine.Activate(false);
        }
    }

    public void CancelJump()
    {
        if (isJumping)
        {
            StopAllCoroutines();
            capCollider.enabled = true;
            isJumping = false;
        }
    }

    public bool IsLeftHand(string actionMapName)
    {
        string handLetter = actionMapName.Substring(4, 1);
        return handLetter.ToLower() == "l";
    }

    private IEnumerator JumpCoroutine()
    {
        Vector3 diff = jumpTrajectoryEndPoint - transform.position;
        diff.y = 0;

        // Could Fix Sometimes above ground on landing

        for (int i = 0; i < jumpTrajectoryEndIndex; i++)
        {
            float t = 0f;
            Vector3 offset = transform.position - playerTransform.position;
            Vector3 startPosition = transform.position;
            Vector3 endPosition = jumpTrajectory[i] + ((-diff.normalized * capCollider.radius + offset) + 0.5f * capCollider.height * Vector3.up) * i / jumpTrajectoryEndIndex;



            Quaternion targetRotation = Quaternion.LookRotation(diff.normalized, Vector3.up);

            while (t < 1f)
            {
                t += Time.deltaTime * 25;

                transform.SetPositionAndRotation(Vector3.Lerp(startPosition, endPosition, t), 
                    Quaternion.Lerp(transform.rotation, targetRotation, Mathf.Clamp(i / (jumpTrajectoryEndIndex * 0.5f), 0, 1)));

                //Debug.Log(Mathf.Clamp(i / (jumpTrajectoryEndIndex * 0.5f), 0, 1));

                yield return null;
            }

            //Debug.Log(transform.eulerAngles);
        }

        //Debug.Log("Jump Complete");
        isJumping = false;
        rig.useGravity = true;
        capCollider.enabled = true;
    }


    public Vector3[] CopyArray(Vector3[] array, Vector3[] arrayToCopy)
    {
        if (array.Length != arrayToCopy.Length)
        {
            return array;
        } 
        else
        {
            for (int i = 0; i < arrayToCopy.Length; i++)
            {
                array[i] = arrayToCopy[i];
            }
        }

        return array;
    }

    public float GetAcceleration(float maxSpeed, float timeToReachFullSpeed)
    {
        return (maxSpeed) / timeToReachFullSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Death"))
        {
            StopAllCoroutines();
            SceneManager.Instance.ResetScene();
        }
        else if (other.CompareTag("Goal"))
        {
            Timer.Instance.Deactivate();

            if (Drone.Instance.hasArrived)
            {
                Debug.Log("Player Lost To Drone");
            }
            else
            {
                Debug.Log("Player Wins");
            }
        }
    }
}