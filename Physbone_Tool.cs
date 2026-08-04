using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.Dynamics;

[InitializeOnLoad]
public class VRCPhysBoneTools
{
    static VRCPhysBoneTools()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
    }

    // Safely reset the tool visibility whenever you click off the object
    private static void OnSelectionChanged()
    {
        Tools.hidden = false;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0) return;

        bool hideDefaultTools = false;

        foreach (GameObject obj in Selection.gameObjects)
        {
            // 1. Draw Collider Tools (Magenta)
            VRCPhysBoneCollider collider = obj.GetComponent<VRCPhysBoneCollider>();
            if (collider != null)
            {
                DrawColliderHandles(collider);
                hideDefaultTools = true;
            }

            // 2. Draw PhysBone Tools (Cyan)
            VRCPhysBone physBone = obj.GetComponent<VRCPhysBone>();
            if (physBone != null)
            {
                DrawPhysBoneHandles(physBone);
                hideDefaultTools = true;
            }
        }

        // Hide Unity's default move/rotate/scale gizmo if we are drawing our own
        Tools.hidden = hideDefaultTools;
    }

    private static void DrawColliderHandles(VRCPhysBoneCollider col)
    {
        if (col == null) return;
        Transform t = col.transform;

        Vector3 worldPos = t.TransformPoint(col.position);
        Quaternion worldRot = t.rotation * col.rotation;
        float maxScale = Mathf.Max(t.lossyScale.x, Mathf.Max(t.lossyScale.y, t.lossyScale.z));
        float currentRadius = col.radius * maxScale;

        Color colColor = new Color(1f, 0.2f, 0.8f, 1f); // Distinct Magenta

        EditorGUI.BeginChangeCheck();

        // Custom Position Handle
        Vector3 newWorldPos = CustomPositionHandle(worldPos, worldRot, colColor);

        // Custom Rotation Handle
        Quaternion newWorldRot = worldRot;
        if (col.shapeType != VRCPhysBoneColliderBase.ShapeType.Sphere)
        {
            newWorldRot = CustomRotationHandle(worldRot, worldPos, colColor);
        }

        // Custom Radius Handle
        Handles.color = colColor;
        float newRadius = Handles.RadiusHandle(worldRot, newWorldPos, currentRadius);

        // Custom Height Handle (Capsules Only)
        float newHeight = col.height;
        if (col.shapeType == VRCPhysBoneColliderBase.ShapeType.Capsule)
        {
            Vector3 topEdge = newWorldPos + (newWorldRot * Vector3.up * ((col.height * maxScale) / 2));
            float size = HandleUtility.GetHandleSize(topEdge) * 0.15f;
            Vector3 newTopEdge = Handles.Slider(topEdge, newWorldRot * Vector3.up, size, Handles.ConeHandleCap, 0.1f);
            
            if (newTopEdge != topEdge)
            {
                float halfHeight = Vector3.Distance(newWorldPos, newTopEdge);
                newHeight = (halfHeight * 2f) / maxScale;
            }
        }

        // Apply
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(col, "Modify PhysBone Collider");
            col.position = t.InverseTransformPoint(newWorldPos);
            
            if (col.shapeType != VRCPhysBoneColliderBase.ShapeType.Sphere)
            {
                col.rotation = Quaternion.Inverse(t.rotation) * newWorldRot;
            }
            
            col.radius = Mathf.Max(0.001f, newRadius / maxScale);
            col.height = Mathf.Max(col.radius * 2, newHeight);
            EditorUtility.SetDirty(col);
        }
    }

    private static void DrawPhysBoneHandles(VRCPhysBone pb)
    {
        if (pb == null) return;
        
        Transform root = pb.rootTransform != null ? pb.rootTransform : pb.transform;
        
        Color pbColor = new Color(0.2f, 0.9f, 1f, 1f); // Distinct Cyan

        EditorGUI.BeginChangeCheck();

        // 1. Root Radius Handle
        Handles.color = pbColor;
        float newRadius = Handles.RadiusHandle(root.rotation, root.position, pb.radius);

        // 2. Endpoint Position Handle
        Vector3 worldEndpointPos = root.TransformPoint(pb.endpointPosition);
        Vector3 newWorldEndpointPos = CustomPositionHandle(worldEndpointPos, root.rotation, pbColor);

        // Apply
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pb, "Modify PhysBone properties");
            
            pb.radius = Mathf.Max(0.001f, newRadius);
            pb.endpointPosition = root.InverseTransformPoint(newWorldEndpointPos);
            
            EditorUtility.SetDirty(pb);
        }
    }

    // --- CUSTOM GIZMO DRAWERS ---

    private static Vector3 CustomPositionHandle(Vector3 position, Quaternion rotation, Color baseColor)
    {
        float size = HandleUtility.GetHandleSize(position);
        Color origColor = Handles.color;
        Vector3 newPos = position;

        // X Axis 
        Handles.color = baseColor;
        newPos = Handles.Slider(newPos, rotation * Vector3.right, size, Handles.ArrowHandleCap, 0f);
        
        // Y Axis
        Handles.color = new Color(baseColor.r * 0.8f, baseColor.g * 0.8f, baseColor.b * 0.8f, baseColor.a);
        newPos = Handles.Slider(newPos, rotation * Vector3.up, size, Handles.ArrowHandleCap, 0f);
        
        // Z Axis
        Handles.color = new Color(baseColor.r * 0.6f, baseColor.g * 0.6f, baseColor.b * 0.6f, baseColor.a);
        newPos = Handles.Slider(newPos, rotation * Vector3.forward, size, Handles.ArrowHandleCap, 0f);

        // Center Free-Move Cube
        Handles.color = baseColor;
        newPos = Handles.FreeMoveHandle(newPos, size * 0.1f, Vector3.zero, Handles.CubeHandleCap);

        Handles.color = origColor;
        return newPos;
    }

    private static Quaternion CustomRotationHandle(Quaternion rotation, Vector3 position, Color baseColor)
    {
        float size = HandleUtility.GetHandleSize(position);
        Color origColor = Handles.color;
        Quaternion newRot = rotation;

        // X Axis Roll
        Handles.color = baseColor;
        newRot = Handles.Disc(newRot, position, rotation * Vector3.right, size, false, 0f);
        
        // Y Axis Roll
        Handles.color = new Color(baseColor.r * 0.8f, baseColor.g * 0.8f, baseColor.b * 0.8f, baseColor.a);
        newRot = Handles.Disc(newRot, position, rotation * Vector3.up, size, false, 0f);
        
        // Z Axis Roll
        Handles.color = new Color(baseColor.r * 0.6f, baseColor.g * 0.6f, baseColor.b * 0.6f, baseColor.a);
        newRot = Handles.Disc(newRot, position, rotation * Vector3.forward, size, false, 0f);

        Handles.color = origColor;
        return newRot;
    }
}