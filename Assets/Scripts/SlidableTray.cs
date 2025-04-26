using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
[System.Obsolete("Code provide through the 'OXIPROJEKT', Do not remove this attribute. Required.")]

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class SlidableTray : MonoBehaviour
{
    public List<Vector2Int> CellsOccupied = new List<Vector2Int>() { Vector2Int.zero }; // cell side 1x1....
    private Camera cam;
    private bool isDragging = false;
    private Vector3 offset;
    private Vector2Int lastCell;
    private Rigidbody rb;
    public bool blockHorizontalDrag = false;
    public bool blockVerticalDrag = false;
    private Collider[] childColliders;
    
    [Header ("Adjust speed")]
    [Range(0f, 200f)]
    public float slideSpeed = 200f; // object sliding speed....
    [Range(0.1f, 1f)]
    public float frameMagnitude = 0.5f; // correct value for collision easily...
    [Range(-5f, 5f)]
    public float collisionTight = -1f; // collision before/after the time.... 


    [HideInInspector]
    public Vector3 defaultPosition;

    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody>();
        childColliders = GetComponentsInChildren<Collider>();
        lastCell = CheckerBoardManager.Instance.GetGridCellFromWorld(transform.position);
        MarkOccupied(lastCell);
        defaultPosition = transform.position;
    }

    void OnMouseDown()
    {
        SlideStart();
    }

    void OnMouseUp()
    {
        SlideStop();
    }

    void Update()
    {
        // Mobile touch inputs....
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 worldTouchPos = GetWorldPositionFromTouch(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (IsTouchingThisObject(worldTouchPos))
                    {
                        SlideStart();
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging)
                    {
                        SlideStop();
                    }
                    break;
            }
        }
    }

    void SlideStart()
    {
        FindObjectOfType<SoundManager>().Play("ClickDown");
        isDragging = true;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        offset = transform.position - GetMouseWorldPosition();
        ClearOccupied(lastCell);
    }

    void SlideStop()
    {
        FindObjectOfType<SoundManager>().Play("ClickDown");
        isDragging = false;
        SnapToGrid();
        rb.useGravity = true;
        rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector3 targetPos = mouseWorld + offset;

            var board = CheckerBoardManager.Instance;
            float cellSize = board.cellSize;
            float maxMovePerFrame = cellSize * frameMagnitude;
            int minOffsetX = int.MaxValue, maxOffsetX = int.MinValue;
            int minOffsetY = int.MaxValue, maxOffsetY = int.MinValue;
            foreach (var offset in CellsOccupied)
            {
                if (offset.x < minOffsetX) minOffsetX = offset.x;
                if (offset.x > maxOffsetX) maxOffsetX = offset.x;
                if (offset.y < minOffsetY) minOffsetY = offset.y;
                if (offset.y > maxOffsetY) maxOffsetY = offset.y;
            }

            float minX = board.gridOrigin.x + (-minOffsetX * cellSize);
            float maxX = board.gridOrigin.x + ((board.cols - 1 - maxOffsetX) * cellSize);
            float minZ = board.gridOrigin.z + (-minOffsetY * cellSize);
            float maxZ = board.gridOrigin.z + ((board.rows - 1 - maxOffsetY) * cellSize);

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.z = Mathf.Clamp(targetPos.z, minZ, maxZ);

            Vector3 newPos = transform.position;

            Vector3 moveDelta = targetPos - transform.position;
            moveDelta = Vector3.ClampMagnitude(moveDelta, maxMovePerFrame);
            blockHorizontalDrag = false;
            blockVerticalDrag = false;

            // Separate movement by axis
            Vector3 horizDelta = new Vector3(moveDelta.x, 0, 0);
            Vector3 vertDelta = new Vector3(0, 0, moveDelta.z);
            bool isHorizBlocked = false;
            bool isVertBlocked = false;
            bool isDiagBlocked = false;

            foreach (var col in childColliders)
            {
                Bounds bounds = col.bounds;
                bounds.Expand(collisionTight);
                Vector3 center = bounds.center;

                // Horizontal collision blocking....
                if (Mathf.Abs(horizDelta.x) > 0.01f)
                {
                    Vector3 direction = horizDelta.normalized;
                    float distance = Mathf.Abs(horizDelta.x);
                    if (Physics.BoxCast(center, bounds.extents, direction, out RaycastHit hitH, Quaternion.identity, distance))
                    {
                        if (hitH.collider.attachedRigidbody != null && hitH.collider.gameObject != gameObject)
                        {
                            isHorizBlocked = true;
                        }
                    }
                }

                // Vertical collision blocking....
                if (Mathf.Abs(vertDelta.z) > 0.01f)
                {
                    Vector3 direction = vertDelta.normalized;
                    float distance = Mathf.Abs(vertDelta.z);
                    if (Physics.BoxCast(center, bounds.extents, direction, out RaycastHit hitV, Quaternion.identity, distance))
                    {
                        if (hitV.collider.attachedRigidbody != null && hitV.collider.gameObject != gameObject)
                        {
                            isVertBlocked = true;
                        }
                    }
                }

                // Diagonal collision blocking....
                if (Mathf.Abs(moveDelta.x) > 0.01f && Mathf.Abs(moveDelta.z) > 0.01f)
                {
                    Vector3 diagDir = new Vector3(moveDelta.x, 0, moveDelta.z).normalized;
                    float diagDistance = moveDelta.magnitude;

                    if (Physics.BoxCast(center, bounds.extents, diagDir, out RaycastHit hitDiag, Quaternion.identity, diagDistance))
                    {
                        if (hitDiag.collider.attachedRigidbody != null && hitDiag.collider.gameObject != gameObject)
                        {
                            isDiagBlocked = true;
                        }
                    }
                }
            }

            blockHorizontalDrag = isHorizBlocked;
            blockVerticalDrag = isVertBlocked;

            // If diagonal blocked but not individually, cancel both
            if (isDiagBlocked && !isHorizBlocked && !isVertBlocked)
            {
                blockHorizontalDrag = true; //should be at least single side true....
                //blockVerticalDrag = true; //should be at least single side true....
            }

            // Apply movement
            if (!blockHorizontalDrag) newPos.x += horizDelta.x;
            if (!blockVerticalDrag) newPos.z += vertDelta.z;

            Vector3 smoothedPos = Vector3.MoveTowards(transform.position, new Vector3(newPos.x, transform.position.y, newPos.z), slideSpeed * Time.fixedDeltaTime);
            rb.MovePosition(smoothedPos);
        }


    }

    void SnapToGrid()
    {
        var board = CheckerBoardManager.Instance;
        Vector2Int targetCell = board.GetGridCellFromWorld(transform.position);

        if (!board.AreCellsFree(targetCell, CellsOccupied))
        {
            if (!board.TryFindNearestFreeCellShape(targetCell, CellsOccupied, out targetCell))
                targetCell = lastCell;
        }

        Vector3 snappedPos = board.GetWorldPositionFromGrid(targetCell);
        transform.position = new Vector3(snappedPos.x, transform.position.y, snappedPos.z);
        lastCell = targetCell;
        MarkOccupied(targetCell);

    }

    void MarkOccupied(Vector2Int origin)
    {
        foreach (var offset in CellsOccupied)
        {
            Vector2Int cell = origin + offset;
            CheckerBoardManager.Instance.gridOccupancy[cell] = this;
        }
    }

    void ClearOccupied(Vector2Int origin)
    {
        foreach (var offset in CellsOccupied)
        {
            Vector2Int cell = origin + offset;
            CheckerBoardManager.Instance.gridOccupancy.Remove(cell);
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, CheckerBoardManager.Instance.gridOrigin);
        return ground.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
    }

    Vector3 GetWorldPositionFromTouch(Vector2 screenPosition)
    {
        Ray ray = cam.ScreenPointToRay(screenPosition);
        Plane ground = new Plane(Vector3.up, CheckerBoardManager.Instance.gridOrigin);
        return ground.Raycast(ray, out float enter) ? ray.GetPoint(enter) : Vector3.zero;
    }

    bool IsTouchingThisObject(Vector3 worldTouchPos)
    {
        Ray ray = cam.ScreenPointToRay(Input.GetTouch(0).position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider != null && hit.collider.gameObject == gameObject;
        }
        return false;
    }
    public void ResetToDefaultPosition()
    {
        transform.position = defaultPosition;

        lastCell = CheckerBoardManager.Instance.GetGridCellFromWorld(transform.position);
        MarkOccupied(lastCell);

        CheckerBoardManager.Instance.RegisterTrayToGrid(this);
    }
}
