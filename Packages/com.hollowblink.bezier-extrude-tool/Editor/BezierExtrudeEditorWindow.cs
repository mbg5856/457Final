using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace BezierExtrudeTool
{
    public class BezierEditorWindow : EditorWindow
    {
        private String objectName;
        private BezierExtrude castedTarget;
        private GameObject controlPoint;
        private float modifyTimer;
        [SerializeField] [HideInInspector] private float modifyTimerMax = 0.1f;
        [SerializeField] [HideInInspector] private int ringSegments = 2;
        [SerializeField] [HideInInspector] private Material material;
        [SerializeField] [HideInInspector] private Shape2D shape2D;
        [SerializeField] [HideInInspector] private int controlPoints = 2;
        [SerializeField] [HideInInspector] private float continuity = 8f;

        [MenuItem("Tools/Bezier Extrude Editor")]
        public static void ShowWindow()
        {
            GetWindow(typeof(BezierEditorWindow));
        }

        private void CreateGUI()
        {
            autoRepaintOnSceneChange = true;
            Undo.undoRedoEvent += ChangeCastedTargetAttributes;
        }

        private void OnGUI()
        {

            GUILayout.Label("Bezier Extrude Tool", EditorStyles.label);
            GUILayout.Space(10);
            if(Selection.activeGameObject != null)
            {
                castedTarget = Selection.activeGameObject.GetComponent<BezierExtrude>() == null ? null : Selection.activeGameObject.GetComponent<BezierExtrude>();
                if(castedTarget != null) controlPoint = null;
                if(castedTarget == null)
                {
                    if(Selection.activeGameObject.transform.parent != null)
                    {
                        castedTarget = Selection.activeGameObject.transform.parent.GetComponent<BezierExtrude>() == null ? null : Selection.activeGameObject.transform.parent.GetComponent<BezierExtrude>();
                    }
                    if(castedTarget != null)
                    {
                        controlPoint = Selection.activeGameObject;
                        continuity = controlPoint.transform.localScale.x;
                    }
                }
            }

            if(castedTarget != null && Selection.activeGameObject != null)
            {
                modifyTimer += Time.deltaTime;
                if(modifyTimer > modifyTimerMax)
                {
                    castedTarget.ModifyMesh();
                    modifyTimer = 0;
                }

                //Material
                GUILayout.BeginHorizontal();
                GUILayout.Label("Material", EditorStyles.label);
                EditorGUI.BeginChangeCheck();
                Material material_ = (Material)EditorGUILayout.ObjectField(castedTarget.GetMaterial(), typeof(Material), false);
                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Material changed");
                    material = material_;
                    castedTarget.SetMaterial(material_);
                }
                GUILayout.EndHorizontal();

                //Shape2D
                GUILayout.BeginHorizontal();
                GUILayout.Label("Mesh2D", EditorStyles.label);
                EditorGUI.BeginChangeCheck();
                Shape2D shape2D_ = (Shape2D)EditorGUILayout.ObjectField(castedTarget.GetShape2D(), typeof(Shape2D), false);
                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Shape2D changed");
                    shape2D = shape2D_;
                    castedTarget.SetShape2D(shape2D_);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                //Modify timer
                GUILayout.Label("Time between mesh updates (LOW VALUES ARE VERY DEMANDING)", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Update time:", EditorStyles.label);
                EditorGUI.BeginChangeCheck();
                float modifyTimerMax_ = EditorGUILayout.Slider(modifyTimerMax, 0.1f, 5f);
                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Modify timer changed");
                    modifyTimerMax = modifyTimerMax_;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                //Ring segments
                GUILayout.BeginHorizontal();
                GUILayout.Label("Ring segments", EditorStyles.label);
                EditorGUI.BeginChangeCheck();
                int ringSegments_ = EditorGUILayout.IntSlider(ringSegments, 2, 32);
                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Ring segments changed");
                    ringSegments = ringSegments_;
                    castedTarget.SetRingSegments(ringSegments_);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                //Control points
                GUILayout.Label("Total control points: " + castedTarget.GetControlPointCount(), EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Add control point", EditorStyles.label);
                if (GUILayout.Button("Add"))
                {
                    Undo.RecordObject(this, "Add control point");
                    controlPoints++;
                    castedTarget.AddControlPoint();
                }
                GUILayout.EndHorizontal();
                if(castedTarget.GetControlPointCount() > 2)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Remove control point", EditorStyles.label);
                    if (GUILayout.Button("Remove"))
                    {
                        Undo.RecordObject(this, "Remove control point");
                        controlPoints--;
                        castedTarget.RemoveControlPoint();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(10);

                if(controlPoint != null) //TO DO
                {
                    GUILayout.Label("Control point impact", EditorStyles.boldLabel);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Impact", EditorStyles.label);
                    EditorGUI.BeginChangeCheck();
                    float continuity_ = EditorGUILayout.Slider(continuity, 1, 100);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(controlPoint.transform, "Continuity changed");
                        continuity = continuity_;
                        controlPoint.transform.localScale = new Vector3(continuity, 1, continuity);
                    }
                    GUILayout.EndHorizontal();
                }

                //Saving and deleting mesh asset
                if(!castedTarget.CheckMeshAsset())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Save mesh asset", EditorStyles.boldLabel);
                    if(GUILayout.Button("Save mesh asset")) castedTarget.GenerateMeshAsset();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Delete mesh asset", EditorStyles.boldLabel);
                    if(GUILayout.Button("Delete mesh asset")) castedTarget.DeleteMeshAsset();
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Object name: ", EditorStyles.label);
                objectName = GUILayout.TextField(objectName);
                GUILayout.EndHorizontal();
                if(GUILayout.Button("Create new bezier extrude object"))
                {
                    GameObject go = new GameObject(objectName);
                    Undo.RegisterCreatedObjectUndo(go, "Create new bezier extrude object");
                    go.AddComponent<BezierExtrude>();
                    objectName = "";
                }
            }
        }

        private void ChangeCastedTargetAttributes(in UndoRedoInfo info)
        {
            if(castedTarget != null)
            {
                if(castedTarget.GetShape2D() != shape2D) castedTarget.SetShape2D(shape2D);
                if (castedTarget.GetMaterial() != material) castedTarget.SetMaterial(material);
                if (castedTarget.GetRingSegments() != ringSegments) castedTarget.SetRingSegments(ringSegments);
                if (info.undoName == "Add control point" && !info.isRedo) castedTarget.RemoveControlPoint();
                else if (info.undoName == "Remove control point" && !info.isRedo) castedTarget.AddControlPoint();
                else if (info.undoName == "Add control point" && info.isRedo) castedTarget.AddControlPoint();
                else if (info.undoName == "Remove control point" && info.isRedo) castedTarget.RemoveControlPoint();
            }
        }
    }
}
