using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.Dynamics;

public class VRCTwistBoneSetup : EditorWindow
{
    [MenuItem("Reapa Studio/Setup Twist Bones on Selected Armature")]
    private static void SetupTwistBones()
    {
        GameObject armature = Selection.activeGameObject;
        if (armature == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an Armature GameObject.", "OK");
            return;
        }

        int count = ProcessTwistBones(armature.transform);
        Debug.Log($"✅ Processed {count} Twist bones on {armature.name}");
        EditorUtility.DisplayDialog("Success", $"Configured {count} Twist bones.", "OK");
    }

    [MenuItem("GameObject/Reapa Studio/Setup Twist Bones", false, 10)]
    private static void ContextMenuSetup(MenuCommand menuCommand)
    {
        GameObject go = menuCommand.context as GameObject;
        if (go == null) return;
        int count = ProcessTwistBones(go.transform);
        Debug.Log($"✅ Processed {count} Twist bones.");
    }

    private static int ProcessTwistBones(Transform root)
    {
        int count = 0;
        List<Transform> twistBones = new List<Transform>();
        FindTwistBonesRecursive(root, twistBones);

        foreach (var twistBone in twistBones)
        {
            VRCRotationConstraint constraint = twistBone.gameObject.GetComponent<VRCRotationConstraint>();
            if (constraint == null)
                constraint = Undo.AddComponent<VRCRotationConstraint>(twistBone.gameObject);

            constraint.Sources.Clear();

            Transform source = FindAppropriateSource(twistBone);
            if (source != null)
            {
                VRCConstraintSource vrcSource = new VRCConstraintSource(source, 1.0f);
                constraint.Sources.Add(vrcSource);
            }

            constraint.IsActive = true;
            constraint.GlobalWeight = 0.65f;
            constraint.Locked = true;

            constraint.AffectsRotationX = false;
            constraint.AffectsRotationY = true;
            constraint.AffectsRotationZ = false;

            if (IsLegTwist(twistBone.name))
            {
                constraint.GlobalWeight = 0.75f;
            }

            constraint.ApplyConfigurationChanges();
            Undo.RecordObject(constraint, "Setup VRC Twist Constraint");
            count++;
        }

        return count;
    }

    private static void FindTwistBonesRecursive(Transform parent, List<Transform> results)
    {
        if (parent.name.ToLowerInvariant().Contains("twist"))
            results.Add(parent);

        foreach (Transform child in parent)
            FindTwistBonesRecursive(child, results);
    }

    private static bool IsLegTwist(string boneName)
    {
        string n = boneName.ToLowerInvariant();
        return n.Contains("leg") || n.Contains("thigh") || n.Contains("calf") || 
               n.Contains("shin") || n.Contains("knee") || n.Contains("foot");
    }

    private static Transform FindAppropriateSource(Transform twistBone)
    {
        string lowerName = twistBone.name.ToLowerInvariant();
        bool isLeg = IsLegTwist(lowerName);

        Transform current = twistBone;
        while (current != null)
        {
            string curr = current.name.ToLowerInvariant();

            if (isLeg)
            {
                if (curr.Contains("foot") || curr.Contains("ankle") || curr.Contains("lowerleg") ||
                    curr.Contains("calf") || curr.Contains("shin"))
                    return current;
            }
            else
            {
                if (curr.Contains("hand") || curr.Contains("wrist") || curr.Contains("lowerarm") ||
                    curr.Contains("forearm"))
                    return current;
            }
            current = current.parent;
        }

        return twistBone.parent;
    }

    [CustomEditor(typeof(Transform))]
    public class ArmatureInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("🔧 Setup Twist Bones (Arms + Legs)"))
            {
                ProcessTwistBones(((Transform)target).root);
            }
        }
    }
}