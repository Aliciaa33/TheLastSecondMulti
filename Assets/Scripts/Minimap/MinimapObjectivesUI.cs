using System.Collections.Generic;
using UnityEngine;

public class MinimapObjectivesUI : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private MinimapBounds bounds;
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransform iconsRoot;

    [Header("Icon Prefabs")]
    [SerializeField] private RectTransform bombIconPrefab;
    [SerializeField] private RectTransform hintIconPrefab;

    [Header("Options")]
    [SerializeField] private bool clampInsideMap = true;

    private readonly Dictionary<MinimapObjectiveMarker, RectTransform> icons =
        new Dictionary<MinimapObjectiveMarker, RectTransform>();

    private readonly List<MinimapObjectiveMarker> removeBuffer =
        new List<MinimapObjectiveMarker>();

    private void LateUpdate()
    {
        if (bounds == null || mapRect == null || iconsRoot == null)
            return;

        SyncIcons();
        UpdateIcons();
    }

    private void SyncIcons()
    {
        for (int i = 0; i < MinimapObjectiveMarker.All.Count; i++)
        {
            MinimapObjectiveMarker marker = MinimapObjectiveMarker.All[i];

            if (marker == null)
                continue;

            if (icons.ContainsKey(marker))
                continue;

            RectTransform prefab = GetIconPrefab(marker.ObjectiveType);
            if (prefab == null)
                continue;

            RectTransform icon = Instantiate(prefab, iconsRoot);
            icon.name = marker.name + "_MinimapIcon";
            icons.Add(marker, icon);
        }

        removeBuffer.Clear();

        foreach (KeyValuePair<MinimapObjectiveMarker, RectTransform> pair in icons)
        {
            if (pair.Key == null || !MinimapObjectiveMarker.All.Contains(pair.Key))
            {
                removeBuffer.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            MinimapObjectiveMarker marker = removeBuffer[i];

            RectTransform icon;
            if (icons.TryGetValue(marker, out icon))
            {
                if (icon != null)
                    Destroy(icon.gameObject);

                icons.Remove(marker);
            }
        }
    }

    private void UpdateIcons()
    {
        foreach (KeyValuePair<MinimapObjectiveMarker, RectTransform> pair in icons)
        {
            MinimapObjectiveMarker marker = pair.Key;
            RectTransform icon = pair.Value;

            if (marker == null || icon == null)
                continue;

            Transform target = marker.MarkerTarget;
            if (target == null)
            {
                if (icon.gameObject.activeSelf)
                    icon.gameObject.SetActive(false);
                continue;
            }

            if (!icon.gameObject.activeSelf)
                icon.gameObject.SetActive(true);

            icon.anchoredPosition = WorldToAnchoredPosition(target.position);

            if (marker.RotateIconWithTarget)
            {
                float yaw = target.eulerAngles.y;
                icon.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }
            else
            {
                icon.localRotation = Quaternion.identity;
            }
        }
    }

    private RectTransform GetIconPrefab(MinimapObjectiveType type)
    {
        switch (type)
        {
            case MinimapObjectiveType.Bomb:
                return bombIconPrefab;

            case MinimapObjectiveType.Hint:
                return hintIconPrefab;
        }

        return null;
    }

    private Vector2 WorldToAnchoredPosition(Vector3 worldPosition)
    {
        Vector2 normalized = bounds.WorldToNormalized(worldPosition);

        if (clampInsideMap)
        {
            normalized.x = Mathf.Clamp01(normalized.x);
            normalized.y = Mathf.Clamp01(normalized.y);
        }

        Rect rect = mapRect.rect;

        float x = (normalized.x - 0.5f) * rect.width;
        float y = (normalized.y - 0.5f) * rect.height;

        return new Vector2(x, y);
    }
}