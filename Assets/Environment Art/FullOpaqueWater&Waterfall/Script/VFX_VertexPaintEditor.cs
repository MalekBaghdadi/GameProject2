using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MyTools.VertexPainting
{
    public class VertexPainterEditor : EditorWindow
    {
        private Color paintColor = Color.red;
        private float brushSize = 1f;
        private float brushStrength = 0.5f;
        private static Dictionary<Mesh, Mesh> clonedMeshes = new Dictionary<Mesh, Mesh>();

        [MenuItem("Tools/Vertex Painter")]
        public static void ShowWindow()
        {
            GetWindow<VertexPainterEditor>("Vertex Painter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Brush Settings", EditorStyles.boldLabel);
            paintColor = EditorGUILayout.ColorField("Color", paintColor);
            brushSize = EditorGUILayout.FloatField("Brush Size", brushSize);
            brushStrength = EditorGUILayout.Slider("Strength", brushStrength, 0f, 1f);

            GUILayout.Space(10);
            if (GUILayout.Button("Reset Selected Mesh"))
            {
                ResetMesh();
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (e == null)
                return;

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit);

            if (hasHit)
            {
                // Draw a green circle to indicate the brush influence area
                Handles.color = Color.green;
                Handles.DrawWireDisc(hit.point, hit.normal, brushSize);

                if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt)
                {
                    MeshFilter mf = hit.collider.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        Mesh mesh = GetClonedMesh(mf);
                        if (mesh != null)
                        {
                            Transform t = mf.transform;
                            Vector3 localHitPoint = t.InverseTransformPoint(hit.point);
                            Vector3[] vertices = mesh.vertices;
                            Color[] vertexColors = mesh.colors;

                            if (vertexColors == null || vertexColors.Length != vertices.Length)
                            {
                                vertexColors = new Color[vertices.Length];
                                for (int i = 0; i < vertexColors.Length; i++)
                                    vertexColors[i] = Color.white;
                            }

                            bool modified = false;
                            for (int i = 0; i < vertices.Length; i++)
                            {
                                float distance = Vector3.Distance(vertices[i], localHitPoint);
                                if (distance <= brushSize)
                                {
                                    float falloff = Mathf.Pow(1.0f - (distance / brushSize), 2);
                                    vertexColors[i] = Color.Lerp(vertexColors[i], paintColor, brushStrength * falloff);
                                    modified = true;
                                }
                            }

                            if (modified)
                            {
                                mesh.colors = vertexColors;
                                EditorUtility.SetDirty(mesh);
                            }
                            e.Use();
                        }
                    }
                }
            }
            else
            {
                // Draw a red circle to indicate an approximate brush area when no object is detected
                Handles.color = Color.red;
                Vector3 circlePos = ray.origin + ray.direction * 5f;
                Handles.DrawWireDisc(circlePos, Vector3.up, brushSize);
            }
            SceneView.RepaintAll();
        }

        private Mesh GetClonedMesh(MeshFilter mf)
        {
            if (mf.sharedMesh == null)
                return null;

            if (!clonedMeshes.TryGetValue(mf.sharedMesh, out Mesh newMesh))
            {
                newMesh = Instantiate(mf.sharedMesh);
                newMesh.name = mf.sharedMesh.name + "_Clone";
                mf.mesh = newMesh;
                clonedMeshes[mf.sharedMesh] = newMesh;

                if (newMesh.colors == null || newMesh.colors.Length != newMesh.vertexCount)
                {
                    Color[] initColors = new Color[newMesh.vertexCount];
                    for (int i = 0; i < initColors.Length; i++)
                        initColors[i] = Color.white;
                    newMesh.colors = initColors;
                }
            }
            return newMesh;
        }

        private void ResetMesh()
        {
            if (Selection.activeGameObject == null)
                return;

            MeshFilter mf = Selection.activeGameObject.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                return;

            Mesh mesh = GetClonedMesh(mf);
            if (mesh != null)
            {
                Color[] resetColors = new Color[mesh.vertexCount];
                for (int i = 0; i < resetColors.Length; i++)
                    resetColors[i] = Color.white;
                mesh.colors = resetColors;
                EditorUtility.SetDirty(mesh);
                SceneView.RepaintAll();
            }
        }
    }
}
