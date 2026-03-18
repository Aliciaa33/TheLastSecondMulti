using System.Collections.Generic;
using UnityEngine;

public class MinimapObjectiveMarker : MonoBehaviour
{
    public static readonly List<MinimapObjectiveMarker> All = new List<MinimapObjectiveMarker>();

    [Header("Type")]
    [SerializeField] private MinimapObjectiveType objectiveType = MinimapObjectiveType.Hint;

    [Header("Optional target")]
    [SerializeField] private Transform markerTarget;

    [Header("Rotation")]
    [SerializeField] private bool rotateIconWithTarget = false;

    public MinimapObjectiveType ObjectiveType
    {
        get { return objectiveType; }
    }

    public Transform MarkerTarget
    {
        get { return markerTarget != null ? markerTarget : transform; }
    }

    public bool RotateIconWithTarget
    {
        get { return rotateIconWithTarget; }
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
        {
            All.Add(this);
        }
    }

    private void OnDisable()
    {
        All.Remove(this);
    }
}