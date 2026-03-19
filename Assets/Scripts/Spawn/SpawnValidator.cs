using UnityEngine;

public static class SpawnValidator
{
    public static bool TryFindSafeSpawnPoint(
        Vector3 areaCenter,
        Vector3 areaSize,
        out Vector3 result,
        int maxAttempts = 30,
        int spawnSurfaceMask = 0,
        int obstacleMask = 0,
        int tableMask = 0,
        float clearRadius = 1.5f,
        float playerHeight = 2f,
        float spawnHeightOffset = 0.1f)
    {
        result = Vector3.zero;

        if (spawnSurfaceMask == 0)
            spawnSurfaceMask = LayerMask.GetMask("Ground", "Table");

        if (obstacleMask == 0)
            obstacleMask = LayerMask.GetMask("Obstacles");

        if (tableMask == 0)
            tableMask = LayerMask.GetMask("Table");

        float halfX = areaSize.x * 0.5f;
        float halfZ = areaSize.z * 0.5f;
        float rayStartHeight = areaCenter.y + areaSize.y + 50f;

        for (int i = 0; i < maxAttempts; i++)
        {
            // get random x,z
            float randomX = Random.Range(areaCenter.x - halfX, areaCenter.x + halfX);
            float randomZ = Random.Range(areaCenter.z - halfZ, areaCenter.z + halfZ);
            Vector3 rayOrigin = new Vector3(randomX, rayStartHeight, randomZ);

            // ray: only on Ground and Table
            RaycastHit hit;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out hit, Mathf.Infinity, spawnSurfaceMask))
                continue;

            Vector3 surfacePoint = hit.point;
            int hitLayer = hit.collider.gameObject.layer;

            // check whether is Ground or Table
            if (((1 << hitLayer) & spawnSurfaceMask) == 0)
                continue;

            bool isTable = ((1 << hitLayer) & tableMask) != 0;

            // If Ground, check whether there is Obstacle
            if (!isTable)
            {
                Vector3 upRayStart = surfacePoint + Vector3.up * 0.1f;
                if (Physics.Raycast(upRayStart, Vector3.up, 50f, obstacleMask))
                    continue;
            }

            // horizontal obstacle
            Vector3 checkCenter = surfacePoint + Vector3.up * 0.5f;
            Collider[] nearbyObstacles = Physics.OverlapSphere(
                checkCenter,
                clearRadius,
                obstacleMask
            );

            if (nearbyObstacles.Length > 0)
                continue;

            //check whether player can stand here
            Vector3 capsuleBottom = surfacePoint + Vector3.up * 0.3f;
            Vector3 capsuleTop = surfacePoint + Vector3.up * playerHeight;
            float capsuleRadius = 0.3f;

            Collider[] standingCheck = Physics.OverlapCapsule(
                capsuleBottom,
                capsuleTop,
                capsuleRadius,
                obstacleMask
            );

            if (standingCheck.Length > 0)
                continue;

            // PASS
            result = surfacePoint + Vector3.up * spawnHeightOffset;
            return true;
        }

        return false;
    }

    public static bool TryFindSafeSpawnPointInBox(
        BoxCollider box,
        out Vector3 result,
        int maxAttempts = 30,
        int spawnSurfaceMask = 0,
        int obstacleMask = 0,
        int tableMask = 0,
        float clearRadius = 1.5f,
        float playerHeight = 2f,
        float spawnHeightOffset = 0.1f)
    {
        result = Vector3.zero;

        if (box == null)
            return false;

        if (spawnSurfaceMask == 0)
            spawnSurfaceMask = LayerMask.GetMask("Ground", "Table");

        if (obstacleMask == 0)
            obstacleMask = LayerMask.GetMask("Obstacle");

        if (tableMask == 0)
            tableMask = LayerMask.GetMask("Table");

        Vector3 localMin = box.center - box.size * 0.5f;
        Vector3 localMax = box.center + box.size * 0.5f;

        float rayStartHeight = box.transform.TransformPoint(
            new Vector3(box.center.x, localMax.y, box.center.z)
        ).y + 50f;

        for (int i = 0; i < maxAttempts; i++)
        {
            // —— 第 1 步：Box 内随机 XZ ——
            Vector3 localPoint = new Vector3(
                Random.Range(localMin.x, localMax.x),
                localMax.y,
                Random.Range(localMin.z, localMax.z)
            );
            Vector3 worldPoint = box.transform.TransformPoint(localPoint);
            Vector3 rayOrigin = new Vector3(worldPoint.x, rayStartHeight, worldPoint.z);

            // —— 第 2 步：向下射线，只打 Ground 和 Table ——
            RaycastHit hit;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out hit, Mathf.Infinity, spawnSurfaceMask))
                continue;

            Vector3 surfacePoint = hit.point;

            // —— 第 3 步：确认在 Box 范围内 ——
            Vector3 localHit = box.transform.InverseTransformPoint(surfacePoint);
            if (localHit.x < localMin.x || localHit.x > localMax.x ||
                localHit.z < localMin.z || localHit.z > localMax.z)
                continue;

            // —— 第 4 步：Layer 确认 ——
            int hitLayer = hit.collider.gameObject.layer;
            if (((1 << hitLayer) & spawnSurfaceMask) == 0)
                continue;

            bool isTable = ((1 << hitLayer) & tableMask) != 0;

            // —— 第 5 步：Ground 才检查头顶，Table 跳过 ——
            if (!isTable)
            {
                Vector3 upRayStart = surfacePoint + Vector3.up * 0.1f;
                if (Physics.Raycast(upRayStart, Vector3.up, 50f, obstacleMask))
                    continue;
            }

            // —— 第 6 步：水平障碍物检查 ——
            Vector3 checkCenter = surfacePoint + Vector3.up * 0.5f;
            Collider[] nearby = Physics.OverlapSphere(
                checkCenter,
                clearRadius,
                obstacleMask
            );

            if (nearby.Length > 0)
                continue;

            // —— 第 7 步：站立空间检查 ——
            Vector3 capsuleBottom = surfacePoint + Vector3.up * 0.3f;
            Vector3 capsuleTop = surfacePoint + Vector3.up * playerHeight;
            float capsuleRadius = 0.3f;

            Collider[] standingCheck = Physics.OverlapCapsule(
                capsuleBottom,
                capsuleTop,
                capsuleRadius,
                obstacleMask
            );

            if (standingCheck.Length > 0)
                continue;

            // —— 安全 ——
            result = surfacePoint + Vector3.up * spawnHeightOffset;
            return true;
        }

        return false;
    }
}