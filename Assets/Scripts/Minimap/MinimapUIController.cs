using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUIController : MonoBehaviour
{
    [Header("Core References")]
    public MinimapBounds bounds;
    public RectTransform mapRect;
    public RectTransform iconsRoot;

    [Header("Local Player")]
    public Transform localPlayer;
    public RectTransform localPlayerIcon;
    public bool rotateLocalPlayerIcon = true;

    [Header("Teammates")]
    public RectTransform teammateIconPrefab;
    public bool rotateTeammateIcons = false;
    public float refreshPlayersInterval = 0.5f;

    private float refreshTimer;
    private PlayerIdentity cachedLocalIdentity;
    private readonly Dictionary<PlayerIdentity, RectTransform> teammateIcons = new Dictionary<PlayerIdentity, RectTransform>();

    private void Update()
    {
        if (bounds == null || mapRect == null || iconsRoot == null)
            return;

        UpdateLocalPlayerIcon();

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshPlayersInterval;
            RefreshTeammates();
        }

        UpdateTeammateIcons();
    }

    private void UpdateLocalPlayerIcon()
    {
        Transform target = localPlayer;

        if (target == null)
        {
            PlayerIdentity localIdentity = FindLocalPlayerIdentity();
            if (localIdentity != null)
                target = localIdentity.MarkerTarget;
        }

        if (target == null || localPlayerIcon == null)
            return;

        localPlayerIcon.anchoredPosition = WorldToAnchoredPosition(target.position);

        if (rotateLocalPlayerIcon)
        {
            float yaw = target.eulerAngles.y;
            localPlayerIcon.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }
    }

    private Vector2 WorldToAnchoredPosition(Vector3 worldPosition)
    {
        Vector2 uv = bounds.WorldToNormalized(worldPosition);
        Rect rect = mapRect.rect;

        float x = (uv.x - 0.5f) * rect.width;
        float y = (uv.y - 0.5f) * rect.height;

        return new Vector2(x, y);
    }

    private PlayerIdentity FindLocalPlayerIdentity()
    {
        if (cachedLocalIdentity != null && cachedLocalIdentity.isActiveAndEnabled)
            return cachedLocalIdentity;

        foreach (PlayerIdentity player in PlayerIdentity.All)
        {
            if (player != null && player.isLocalPlayer)
            {
                cachedLocalIdentity = player;
                return cachedLocalIdentity;
            }
        }

        return null;
    }

    private void RefreshTeammates()
    {
        PlayerIdentity localIdentity = FindLocalPlayerIdentity();
        if (localIdentity == null)
            return;

        HashSet<PlayerIdentity> validPlayers = new HashSet<PlayerIdentity>();

        foreach (PlayerIdentity player in PlayerIdentity.All)
        {
            if (player == null || !player.isActiveAndEnabled)
                continue;

            if (player == localIdentity)
                continue;

            if (player.teamId != localIdentity.teamId)
                continue;

            validPlayers.Add(player);

            if (!teammateIcons.ContainsKey(player))
            {
                RectTransform icon = Instantiate(teammateIconPrefab, iconsRoot);
                teammateIcons.Add(player, icon);
            }
        }

        List<PlayerIdentity> toRemove = new List<PlayerIdentity>();

        foreach (var pair in teammateIcons)
        {
            if (!validPlayers.Contains(pair.Key))
                toRemove.Add(pair.Key);
        }

        foreach (PlayerIdentity player in toRemove)
        {
            if (teammateIcons[player] != null)
                Destroy(teammateIcons[player].gameObject);

            teammateIcons.Remove(player);
        }
    }

    private void UpdateTeammateIcons()
    {
        foreach (var pair in teammateIcons)
        {
            PlayerIdentity player = pair.Key;
            RectTransform icon = pair.Value;

            if (player == null || icon == null)
                continue;

            Transform target = player.MarkerTarget;
            icon.anchoredPosition = WorldToAnchoredPosition(target.position);

            if (rotateTeammateIcons)
            {
                float yaw = target.eulerAngles.y;
                icon.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }
        }
    }
}