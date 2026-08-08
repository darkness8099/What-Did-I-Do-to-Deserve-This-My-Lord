using UnityEngine;

// Smoothly glides a monster view toward its target cell-center so movement looks continuous (a speed),
// instead of snapping grid-to-grid. The data layer still moves one cell per ecology tick; only the view interpolates.
public class MonsterViewMover : MonoBehaviour
{
    public float speed = 1.0f; // world units (cells) per second

    private Vector3 target;
    private bool hasTarget;
    private bool movementPaused;

    public bool IsMovementPaused => movementPaused;

    public void SnapTo(Vector3 worldPos)
    {
        target = worldPos;
        hasTarget = true;
        transform.position = worldPos;
    }

    public void MoveTo(Vector3 worldPos)
    {
        target = worldPos;
        hasTarget = true;
    }

    public void SetMovementPaused(bool paused)
    {
        movementPaused = paused;
    }

    private void Update()
    {
        if (!hasTarget || movementPaused) return;
        float step = Mathf.Max(0.01f, speed) * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target, step);
    }
}
