using UnityEngine;
using UnityEngine.Tilemaps;

public static class AStarUtil
{
    public class Node
    {
        public Vector3Int cellPosition;
        public int gCost;
        public int hCost;
        public Node parent;

        public Node(Vector3Int pos)
        {
            cellPosition = pos;
        }

        public int fCost => gCost + hCost;
    }
}
