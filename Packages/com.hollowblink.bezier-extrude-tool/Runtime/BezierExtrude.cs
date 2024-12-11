using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Rendering;
using System.IO;


namespace BezierExtrudeTool
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    [AddComponentMenu("Splines/Bezier Extrude")]
    [ExecuteInEditMode]
    public class BezierExtrude : MonoBehaviour
    {
        [HideInInspector, SerializeField] private Shape2D shape2D;
        [HideInInspector, SerializeField] private int edgeRingCount = 8;
        private float t = 0;
        private int bezierSegment = 0;
        private bool showRingShape = false;

        [HideInInspector, SerializeField] private List<Transform> controlPoints;
        [HideInInspector, SerializeField] private List<Vector3> controlPointsPositions;
        [HideInInspector, SerializeField] private List<Quaternion> controlPointsRotations;
        [HideInInspector, SerializeField] private List<Vector3> controlPointsScales;

        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private int modifiedControlPointIndex = 0;

        private enum GenerateMeshMode{ GenerateEntirely, AddSegment, DeleteSegment, Modify }

    #if UNITY_EDITOR
        private Vector3 GetPos(int i )
        {
            if(i % 3 == 0) return controlPoints[i / 3].position;
            else if((i - 1) % 3 == 0) return controlPoints[(i - 1)/ 3].transform.position + controlPoints[(i - 1)/ 3].transform.forward * controlPoints[(i - 1)/ 3].localScale.z;
            else return controlPoints[(i + 1) / 3].transform.position - controlPoints[(i + 1) / 3].transform.forward  * controlPoints[(i + 1) / 3].localScale.x;
        }


        public void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            if (Application.isEditor && !Application.isPlaying)
            {
                mesh = new Mesh();
                mesh.name = name + " Mesh";
                if(controlPoints == null || controlPoints.Count == 0)
                {
                    controlPoints = new List<Transform>();
                    controlPointsPositions = new List<Vector3>();
                    controlPointsRotations = new List<Quaternion>();
                    controlPointsScales = new List<Vector3>();
                    for(int i = 0; i < 2; i++) AddControlPoint();
                }
                else
                {
                    controlPointsPositions = controlPoints.Select(cp => cp.localPosition).ToList();
                    controlPointsRotations = controlPoints.Select(cp => cp.localRotation).ToList();
                    controlPointsScales = controlPoints.Select(cp => cp.localScale).ToList();
                }
                if(shape2D == null) shape2D = AssetDatabase.LoadAssetAtPath("Packages/com.hollowblink.bezier-extrude-tool/Samples/Shapes2D/DefaultShape2D.asset", typeof(Shape2D)) as Shape2D;
                if(mesh.triangles.Length == 0) GenerateMesh(GenerateMeshMode.GenerateEntirely);
                meshFilter.sharedMesh = mesh;
                meshCollider.sharedMesh = mesh;
                if(meshRenderer.sharedMaterial == null) GetComponent<MeshRenderer>().sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultMaterial;
            }
        }

        public void ModifyMesh()
        {
            if(Application.isEditor && !Application.isPlaying)
            {
                for(int i = 0; i < controlPoints.Count; i++)
                {
                    if(controlPoints[i].localPosition != controlPointsPositions[i] || controlPoints[i].localRotation != controlPointsRotations[i] || controlPoints[i].localScale != controlPointsScales[i])
                    {
                        controlPointsPositions[i] = controlPoints[i].localPosition;
                        controlPointsRotations[i] = controlPoints[i].localRotation;
                        controlPointsScales[i] = controlPoints[i].localScale;
                        modifiedControlPointIndex = i;
                        GenerateMesh(GenerateMeshMode.Modify);
                    }
                }
            }
        }

        //Generates the mesh
        private void GenerateMesh(GenerateMeshMode mode)
        {
            List<Vector3> verts, normals, modifiedVerts, modifiedNormals;
            List<Vector2> uvs, modifiedUvs;
            List<int> triIndices, modifiedTriIndices;
            int bezierSegmentStart = 0, bezierSegmentEnd = 0, triangleRingStart = 0, triangleRingEnd = 0;

            //Initialize the lists for the modified mesh
            modifiedVerts = new List<Vector3>();
            modifiedNormals = new List<Vector3>();
            modifiedUvs = new List<Vector2>();
            modifiedTriIndices = new List<int>();

            //Initialize the mesh structures or copy the data from the current mesh
            if(mode == GenerateMeshMode.GenerateEntirely)
            {
                verts = new List<Vector3>();
                normals = new List<Vector3>();
                uvs = new List<Vector2>();
                triIndices = new List<int>();
            }
            else
            {
                verts = mesh.vertices.ToList();
                normals = mesh.normals.ToList();
                uvs = mesh.uv.ToList();
                triIndices = mesh.triangles.ToList();
            }

            //Set the start and end indexes for the bezier segments and the triangle rings
            if(mode == GenerateMeshMode.GenerateEntirely)
            {
                bezierSegmentStart = 0;
                bezierSegmentEnd = controlPoints.Count - 1;
                triangleRingStart = 0;
                triangleRingEnd = (edgeRingCount - 1) * (controlPoints.Count - 1);
                mesh = new Mesh();
                mesh.name = name + " Mesh";
            }
            else if(mode == GenerateMeshMode.AddSegment)
            {
                bezierSegmentStart = controlPoints.Count - 2;
                bezierSegmentEnd = controlPoints.Count - 1;
                triangleRingStart = (edgeRingCount - 1) * (controlPoints.Count - 2);
                triangleRingEnd = (edgeRingCount - 1) * (controlPoints.Count - 1);
            }
            else if(mode == GenerateMeshMode.Modify)
            {
                if(modifiedControlPointIndex == 0)
                {
                    bezierSegmentStart = 0;
                    bezierSegmentEnd = 1;
                    triangleRingStart = 0;
                    triangleRingEnd = edgeRingCount - 1;
                }
                else if(modifiedControlPointIndex == controlPoints.Count - 1)
                {
                    bezierSegmentStart = controlPoints.Count - 2;
                    bezierSegmentEnd = controlPoints.Count - 1;
                    triangleRingStart = (edgeRingCount - 1) * (controlPoints.Count - 2);
                    triangleRingEnd = (edgeRingCount - 1) * (controlPoints.Count - 1);
                }
                else
                {
                    bezierSegmentStart = modifiedControlPointIndex - 1;
                    bezierSegmentEnd = modifiedControlPointIndex + 1;
                    triangleRingStart = (edgeRingCount - 1) * (modifiedControlPointIndex - 1);
                    triangleRingEnd = (edgeRingCount - 1) * (modifiedControlPointIndex + 1);
                }
            }

            float uSpan = shape2D.CalculUsSpan();

            if(mode != GenerateMeshMode.DeleteSegment)
            {
                //Generate the vertices, normals and uvs
                for (int bezierSegment = bezierSegmentStart; bezierSegment < bezierSegmentEnd; bezierSegment++)
                {
                    for (int ring = 0; ring < edgeRingCount; ring++)
                    {
                        if(bezierSegment > 0 && ring == 0) continue;
                        float t = ring / (edgeRingCount - 1f);
                        OrientedPoint op = GetBezierOrientedPoint(t, bezierSegment);
                        for (int i = 0; i < shape2D.VertexCount; i++)
                        {
                            if(mode != GenerateMeshMode.Modify)
                            {
                                verts.Add(transform.InverseTransformPoint(op.LocalToWorldPos(shape2D.vertices[i].point)));
                                normals.Add(transform.InverseTransformDirection(op.LocalToWorldVec(shape2D.vertices[i].normal)));
                                uvs.Add(new Vector2(shape2D.vertices[i].u, (t + bezierSegment) * GetApproxLength() / uSpan));
                            }
                            else
                            {
                                modifiedVerts.Add(transform.InverseTransformPoint(op.LocalToWorldPos(shape2D.vertices[i].point)));
                                modifiedNormals.Add(transform.InverseTransformDirection(op.LocalToWorldVec(shape2D.vertices[i].normal)));
                                modifiedUvs.Add(new Vector2(shape2D.vertices[i].u, (t + bezierSegment) * GetApproxLength() / uSpan));
                            }
                        }
                    }
                }

                //Generate the triangles
                for (int ring = triangleRingStart; ring < triangleRingEnd; ring++)
                {
                    int rootIndex = ring * shape2D.VertexCount;
                    int nextRootIndex = (ring + 1) * shape2D.VertexCount;
                    for (int line = 0; line < shape2D.LineCount; line += 2)
                    {
                        int lineIndexA = shape2D.lineIndices[line];
                        int lineIndexB = shape2D.lineIndices[line + 1];
                        int currentA = rootIndex + lineIndexA;
                        int currentB = rootIndex + lineIndexB;
                        int nextA = nextRootIndex + lineIndexA;
                        int nextB = nextRootIndex + lineIndexB;
                        if(mode != GenerateMeshMode.Modify)
                        {
                            triIndices.Add(currentA);
                            triIndices.Add(nextA);
                            triIndices.Add(nextB);
                            triIndices.Add(currentA);
                            triIndices.Add(nextB);
                            triIndices.Add(currentB);
                        }
                        else
                        {
                            modifiedTriIndices.Add(currentA);
                            modifiedTriIndices.Add(nextA);
                            modifiedTriIndices.Add(nextB);
                            modifiedTriIndices.Add(currentA);
                            modifiedTriIndices.Add(nextB);
                            modifiedTriIndices.Add(currentB);
                        }
                    }
                }
            }
            else
            {
                //Remove the vertices, normals and uvs
                for (int ring = 0; ring < edgeRingCount - 1; ring++)
                {
                    for (int i = 0; i < shape2D.VertexCount; i++)
                    {
                        verts.RemoveAt(verts.Count - 1);
                        normals.RemoveAt(normals.Count - 1);
                        uvs.RemoveAt(uvs.Count - 1);
                    }
                }

                //Remove the triangles
                for (int ring = 0; ring < edgeRingCount - 1; ring++)
                {
                    for (int line = 0; line < shape2D.LineCount; line += 2)
                    {
                        for (int i = 0; i < 6; i++) triIndices.RemoveAt(triIndices.Count - 1);
                    }
                }
            }

            if(mode == GenerateMeshMode.Modify)
            {
                int start;

                if(bezierSegmentStart > 0) start = (bezierSegmentStart * (edgeRingCount - 1) + 1) * shape2D.VertexCount;
                else start = 0;

                for(int i = 0; i < modifiedVerts.Count; i++)
                {
                    verts[start + i] = modifiedVerts[i];
                    normals[start + i] = modifiedNormals[i];
                    uvs[start + i] = modifiedUvs[i];
                }

                if(bezierSegmentStart > 0) start = bezierSegmentStart * (edgeRingCount - 1) * shape2D.VertexCount * 3;
                else start = 0;

                for(int i = 0; i < modifiedTriIndices.Count; i++)
                {
                    triIndices[start + i] = modifiedTriIndices[i];
                }
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triIndices, 0);
            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        public void OnDrawGizmos()
        {
            for (int i = 0; i < 4 + (3 * (controlPoints.Count - 2)); i++)
            {
                Gizmos.DrawSphere(GetPos(i), 0.2f);
            }
            for(int i = 0; i < controlPoints.Count - 1; i++)
            {
                Handles.DrawBezier(GetPos(0 + (3 * i)), GetPos(3 + (3 * i)), GetPos(1 + (3 * i)), GetPos(2 + (3 * i)), Color.red, EditorGUIUtility.whiteTexture, 1f);
            }

            OrientedPoint op = GetBezierOrientedPoint(t, bezierSegment);
            //Handles.PositionHandle(op.position, op.rotation);

            if(showRingShape)
            {
                Vector3[] verts = shape2D.vertices.Select(v => op.LocalToWorldPos(v.point)).ToArray();
                for(int i = 0; i < shape2D.LineCount; i+=2)
                {
                    Vector3 a = verts[shape2D.lineIndices[i]];
                    Vector3 b = verts[shape2D.lineIndices[i+1]];
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        private OrientedPoint GetBezierOrientedPoint(float t, int bezierSegment)
        {
            Vector3[] pts = new Vector3[4];
            for (int i = 0; i < 4; i++) pts[i] = GetPos(i + (3 * bezierSegment));
            float omt = 1f-t;
            float omt2 = omt * omt;
            float t2 = t * t;
            Vector3 position =	pts[0] * ( omt2 * omt ) +
                                pts[1] * ( 3f * omt2 * t ) +
                                pts[2] * ( 3f * omt * t2 ) +
                                pts[3] * ( t2 * t );

            Vector3 tangent =   pts[0] * ( -omt2 ) +
                                pts[1] * ( 3 * omt2 - 2 * omt ) +
                                pts[2] * ( -3 * t2 + 2 * t ) +
                                pts[3] * ( t2 );
            Vector3 up = Vector3.Lerp(controlPoints[0 + (1 * bezierSegment)].up, controlPoints[1 + (1 * bezierSegment)].up, t).normalized;
            Quaternion rotation = Quaternion.LookRotation(tangent, up);
            return new OrientedPoint(position, rotation);
        }

        private float GetApproxLength(int precision = 100)
        {
            Vector3[] points = new Vector3[precision * (controlPoints.Count - 1)];
            for(int i = 0; i < controlPoints.Count - 1; i++)
            {
                for (int j = 0; j < precision; j++)
                {
                    float t = j / (precision - 1f);
                    points[j + (precision * i)] = GetBezierOrientedPoint(t, i).position;
                }
            }
            float distance = 0;
            for(int i = 0; i < precision - 1; i++)
            {
                distance += Vector3.Distance(points[i], points[i+1]);
            }
            return distance;
        }

        public void GenerateMeshAsset()
        {
            if(mesh == null)
            {
                mesh = new Mesh();
                mesh.name = name + " Mesh";
                GenerateMesh(GenerateMeshMode.GenerateEntirely);
            }
            if(!Directory.Exists("Assets/Resources/Bezier Models")) Directory.CreateDirectory("Assets/Resources/Bezier Models");
            var path = $"Assets/Resources/Bezier Models/{mesh.name}.asset";

            AssetDatabase.CreateAsset(mesh, path);
            EditorGUIUtility.PingObject(mesh);
        }

        public void DeleteMeshAsset()
        {
            var path = $"Assets/Resources/Bezier Models/{mesh.name}.asset";
            AssetDatabase.DeleteAsset(path);
            mesh = new Mesh();
            mesh.name = name + " Mesh";
            GenerateMesh(GenerateMeshMode.GenerateEntirely);
        }

        public bool CheckMeshAsset()
        {
            if(mesh == null) return false;
            var path = $"Assets/Resources/Bezier Models/{mesh.name}.asset";
            return AssetDatabase.LoadAssetAtPath<Mesh>(path) != null;
        }

        public int GetControlPointCount()
        {
            return controlPoints.Count;
        }

        public void AddControlPoint()
        {
            int index = controlPoints.Count;
            GameObject controlPoint = new GameObject($"p{index}");
            controlPoint.transform.SetParent(transform);
            if(index == 0)
            {
                controlPoint.transform.localPosition = new Vector3(0, 0, 0);
                controlPoint.transform.localScale = new Vector3(8, 1, 8);
            }
            else
            {
                controlPoint.transform.localPosition =  controlPoints[index - 1].localPosition + controlPoints[index - 1].forward.normalized * (10 + controlPoints[index - 1].localScale.z);
                controlPoint.transform.localRotation = controlPoints[index - 1].localRotation;
                controlPoint.transform.localScale = new Vector3(8, 1, 8);
            }
            controlPoints.Add(controlPoint.transform);
            controlPointsPositions.Add(controlPoint.transform.position);
            controlPointsRotations.Add(controlPoint.transform.rotation);
            controlPointsScales.Add(controlPoint.transform.localScale);
            if(controlPoints.Count > 2)GenerateMesh(GenerateMeshMode.AddSegment);
        }

        public void RemoveControlPoint()
        {
            DestroyImmediate(controlPoints[controlPoints.Count - 1].gameObject);
            controlPoints.RemoveAt(controlPoints.Count - 1);
            controlPointsPositions.RemoveAt(controlPointsPositions.Count - 1);
            controlPointsRotations.RemoveAt(controlPointsRotations.Count - 1);
            controlPointsScales.RemoveAt(controlPointsScales.Count - 1);
            GenerateMesh(GenerateMeshMode.DeleteSegment);
        }

        public void SetMaterial(Material material)
        {
            if(GetComponent<MeshRenderer>().sharedMaterial == material) return;
            GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        public Material GetMaterial()
        {
            return GetComponent<MeshRenderer>().sharedMaterial;
        }

        public void SetShape2D(Shape2D shape2D)
        {
            if(this.shape2D == shape2D || shape2D == null) return;
            this.shape2D = shape2D;
            GenerateMesh(GenerateMeshMode.GenerateEntirely);
        }

        public Shape2D GetShape2D()
        {
            return shape2D;
        }

        public int GetRingSegments()
        {
            return edgeRingCount;
        }

        public void SetRingSegments(int edgeRingCount)
        {
            if(this.edgeRingCount == edgeRingCount) return;
            this.edgeRingCount = edgeRingCount;
            GenerateMesh(GenerateMeshMode.GenerateEntirely);
        }

        #endif
    }
}