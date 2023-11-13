using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject landingCircle;

    private int segmentCount;
    private float curveLength;

    private Vector3[] segments;
    private int segmentEndIndex = 0;
    private Vector3 segmentEndPoint = Vector3.zero;
    private LineRenderer lineRenderer;

    private float jumpSpeed;
    private float jumpGravityMultiplier;

    private const float TimeCurveAdition = 0.5f;

    private LayerMask grabAbleLayer;
    private bool trajectoryActive = false;

    public Vector3[] Segments { get { return segments; } }
    public int SegmentEndIndex { get { return segmentEndIndex; } }
    public Vector3 SegmentEndPoint { get { return segmentEndPoint; } }

    public void Activate(bool trajectoryActive)
    {
        if (!trajectoryActive)
        {
            landingCircle.SetActive(false);
        }

        lineRenderer.enabled = trajectoryActive;

        this.trajectoryActive = trajectoryActive;
    }


    void Start()
    {
        segmentCount = PlayerController.Instance.SegmentCount;
        curveLength = PlayerController.Instance.CurveLength;

        segments = new Vector3[segmentCount];

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount;

        jumpSpeed = PlayerController.Instance.JumpSpeed;
        jumpGravityMultiplier = PlayerController.Instance.JumpGravityMultiplier;

        grabAbleLayer = PlayerController.Instance.GrabAbleLayer;
    }

    void LateUpdate()
    {
        if (!trajectoryActive)
        {
            return;
        }

        bool collided = false;
        Vector3 collidedPoint = Vector3.zero;

        Vector3 startPos = transform.position;
        segments[0] = startPos;
        lineRenderer.SetPosition(0, startPos);

        Vector3 startVelocity = transform.forward * jumpSpeed;

        for (int i = 1; i < segmentCount; i++)
        {
            float timeOffset = (i * Time.fixedDeltaTime * curveLength);

            Vector3 gravityOffset = TimeCurveAdition * Physics.gravity * jumpGravityMultiplier * Mathf.Pow(timeOffset, 2);

            segments[i] = segments[0] + startVelocity * timeOffset + gravityOffset;
            lineRenderer.SetPosition(i, segments[i]);

            if (collided)
            {
                continue;
            }

            if (Physics.Raycast(segments[i - 1], segments[i] - segments[i - 1], out RaycastHit hitInfo, (segments[i - 1] - segments[i]).magnitude, grabAbleLayer, QueryTriggerInteraction.Ignore))
            {
                collided = true;
                collidedPoint = hitInfo.point;
                segmentEndIndex = i;
            }
        }

        if (collided)
        {
            landingCircle.SetActive(true);
            landingCircle.transform.position = collidedPoint;
        } 
        else
        {
            landingCircle.SetActive(false);
            segmentEndIndex = 0;
        }

        segmentEndPoint = collidedPoint;
    }
}