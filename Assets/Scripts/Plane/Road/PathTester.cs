using UnityEngine;


namespace Assets.Scripts.Plane.Road
{
#if UNITY_EDITOR || UNITY_WEBGL
    public class PathTester : MonoBehaviour
    {
        public RoadNetworkBuilder roadNetwork;

        [SerializeField] UserLocationManager user;
        [SerializeField] int startNodeId = 1;
        [SerializeField] int goalNodeId = 92;
        [SerializeField] bool exeFindTest = false;
        int preStartNodeId = 0;
        int preGoalNodeId = 0;

        void Update()
        {
            if (!exeFindTest)
            {
                return;
            }
            startNodeId = user.GetStartNodeForPathfinding().nodeId;
            if (preStartNodeId != startNodeId || preGoalNodeId != goalNodeId)
            {
                if (roadNetwork != null)
                {
                    Debug.Log($"Finding path from {startNodeId} to {goalNodeId}...");
                    roadNetwork.FindPath(startNodeId, goalNodeId);
                }
                preStartNodeId = startNodeId;
                preGoalNodeId = goalNodeId;
            }
        }
    }
#endif
}