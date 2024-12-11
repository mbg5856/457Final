using UnityEngine;

namespace BezierExtrudeTool
{
    [CreateAssetMenu]
    public class Shape2D : ScriptableObject
    {
        [System.Serializable]
        public class Vertex
        {
            public Vector2 point;
            public Vector2 normal;
            public float u;
        }

        public Vertex[] vertices;
        public int[] lineIndices;
        public int VertexCount => vertices.Length;
        public int LineCount => lineIndices.Length;
        public float CalculUsSpan()
        {
            float distance = 0;
            for (int i = 0; i < vertices.Length - 1; i++)
            {
                Vector2 a = vertices[i].point;
                Vector2 b = vertices[i + 1].point;
                distance += (a - b).magnitude;
            }
            return distance;
        }
    }
}
