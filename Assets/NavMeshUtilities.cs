using UnityEngine;
using UnityEngine.AI;

public static class NavMeshUtilities
{
    /// <summary>
    /// Samples a random point on the NavMesh within radius. Returns Vector3.zero if fails.
    /// </summary>
    public static Vector3 SampleRandomNavMeshPoint(Vector3 origin, float radius, int attempts = 10)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 randomPoint = origin + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return Vector3.zero;
    }
}
