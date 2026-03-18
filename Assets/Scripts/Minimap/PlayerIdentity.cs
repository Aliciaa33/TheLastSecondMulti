using System.Collections.Generic;
using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    public static readonly List<PlayerIdentity> All = new List<PlayerIdentity>();

    [Header("Identity")]
    public string playerId;
    public int teamId = 0;
    public bool isLocalPlayer = false;

    [Header("Optional marker target")]
    public Transform markerTarget;

    public Transform MarkerTarget
    {
        get
        {
            return markerTarget != null ? markerTarget : transform;
        }
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }
}