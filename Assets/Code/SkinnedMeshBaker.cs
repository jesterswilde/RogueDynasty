using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkinnedMeshBaker
{
    /// <summary>
    /// Bakes all SkinnedMeshRenderers under <paramref name="root"/> into
    /// static MeshRenderers whose vertices are in **world space**.
    ///
    /// Each returned GameObject:
    /// - Has identity transform (position=0, rotation=identity, scale=1).
    /// - Renders at the same world-space pose as the SkinnedMeshRenderer at bake time.
    /// - Is NOT parented under root (they live at scene root).
    /// </summary>
    public static GameObject[] BakeSkinnedHierarchyToStatic(
        GameObject root,
        bool copyMaterials = true,
        bool disableOriginalSkinnedRenderers = false)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        SkinnedMeshRenderer[] skinnedRenderers =
            root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: false);

        List<GameObject> bakedObjects = new List<GameObject>(skinnedRenderers.Length);

        foreach (var smr in skinnedRenderers)
        {
            if (smr == null || smr.sharedMesh == null)
                continue;

            // 1. Bake the skinned mesh in its current pose
            Mesh bakedMesh = new Mesh
            {
                name = smr.sharedMesh.name + "_Baked"
            };

            // Version-agnostic: default overload, vertices relative to smr.transform
            smr.BakeMesh(bakedMesh);

            // 2. Convert baked vertices (and normals/tangents) into WORLD space
            Matrix4x4 localToWorld = smr.transform.localToWorldMatrix;

            Vector3[] verts   = bakedMesh.vertices;
            Vector3[] normals = bakedMesh.normals;
            Vector4[] tangents = bakedMesh.tangents;

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = localToWorld.MultiplyPoint3x4(verts[i]);
            }

            if (normals != null && normals.Length == verts.Length)
            {
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = localToWorld.MultiplyVector(normals[i]).normalized;
                }
            }

            if (tangents != null && tangents.Length == verts.Length)
            {
                for (int i = 0; i < tangents.Length; i++)
                {
                    Vector4 t = tangents[i];
                    Vector3 t3 = new Vector3(t.x, t.y, t.z);
                    t3 = localToWorld.MultiplyVector(t3).normalized;
                    tangents[i] = new Vector4(t3.x, t3.y, t3.z, t.w);
                }
            }

            bakedMesh.vertices = verts;
            if (normals != null && normals.Length == verts.Length)
                bakedMesh.normals = normals;
            if (tangents != null && tangents.Length == verts.Length)
                bakedMesh.tangents = tangents;

            bakedMesh.RecalculateBounds();

            // 3. Create a new GameObject for the baked mesh
            GameObject bakedGo = new GameObject(smr.gameObject.name + "_Static");
            Transform bakedTransform = bakedGo.transform;

            // Identity transform so local space == world space
            bakedTransform.position = Vector3.zero;
            bakedTransform.rotation = Quaternion.identity;
            bakedTransform.localScale = Vector3.one;

            // NOTE: we intentionally do NOT parent it under root,
            // to keep the transform identity and avoid re-introducing
            // hierarchical scaling/rotation.
            bakedTransform.SetParent(null, worldPositionStays: false);

            // 4. Add MeshFilter + MeshRenderer
            MeshFilter mf = bakedGo.AddComponent<MeshFilter>();
            mf.sharedMesh = bakedMesh;

            MeshRenderer mr = bakedGo.AddComponent<MeshRenderer>();
            if (copyMaterials)
            {
                mr.sharedMaterials = smr.sharedMaterials;
            }

            bakedObjects.Add(bakedGo);

            if (disableOriginalSkinnedRenderers)
            {
                smr.enabled = false;
            }
        }

        return bakedObjects.ToArray();
    }

    /// <summary>
    /// Kept for compatibility; just forwards to BakeSkinnedHierarchyToStatic.
    /// </summary>
    public static GameObject[] BakeSkinnedHierarchyToStaticSafeForThreads(GameObject root)
    {
        return BakeSkinnedHierarchyToStatic(root);
    }
}
