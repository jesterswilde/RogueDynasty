using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkinnedMeshBaker
{
    /// <summary>
    /// Takes a root GameObject that has SkinnedMeshRenderers (possibly in children)
    /// and returns an array of new GameObjects, each with a static Mesh baked
    /// from the current animated pose.
    ///
    /// - All Unity API calls must run on the main thread.
    /// - Each returned GameObject is parented directly under <paramref name="root"/>.
    /// - World-space appearance (including scale) matches the source SkinnedMeshRenderer.
    /// </summary>
    /// <param name="root">
    /// Root GameObject containing one or more SkinnedMeshRenderer components.
    /// </param>
    /// <param name="copyMaterials">
    /// If true, copies the SkinnedMeshRenderer's sharedMaterials onto the new MeshRenderer.
    /// </param>
    /// <param name="disableOriginalSkinnedRenderers">
    /// If true, disables the SkinnedMeshRenderer components after baking.
    /// </param>
    /// <returns>
    /// Array of newly created GameObjects with MeshFilter + MeshRenderer,
    /// each a direct child of <paramref name="root"/>.
    /// </returns>
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

            // --- Bake mesh in the current animated pose ---
            Mesh bakedMesh = new Mesh
            {
                name = smr.sharedMesh.name + "_Baked"
            };

#if UNITY_2020_1_OR_NEWER
            // In newer Unity versions, 'useScale: true' ensures scaling is baked correctly.
            smr.BakeMesh(bakedMesh, true);
#else
            smr.BakeMesh(bakedMesh);
#endif

            // --- Create a new GameObject for the baked mesh ---
            GameObject bakedGo = new GameObject(smr.gameObject.name + "_Static");
            Transform bakedTransform = bakedGo.transform;
            Transform sourceTransform = smr.transform;

            // 1. Set world transform to match the SkinnedMeshRenderer's current world transform.
            bakedTransform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
            bakedTransform.localScale = sourceTransform.lossyScale;

            // 2. Reparent under the provided root, keeping world-space transform the same.
            bakedTransform.SetParent(null, worldPositionStays: true);

            // --- Add MeshFilter and MeshRenderer ---
            MeshFilter mf = bakedGo.AddComponent<MeshFilter>();
            mf.sharedMesh = bakedMesh;

            MeshRenderer mr = bakedGo.AddComponent<MeshRenderer>();
            if (copyMaterials)
            {
                var originalMats = smr.sharedMaterials;
                var matsCopy = new Material[originalMats.Length];
                Array.Copy(originalMats, matsCopy, originalMats.Length);
                mr.sharedMaterials = matsCopy;
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
    /// Same as BakeSkinnedHierarchyToStatic but named to emphasize that any
    /// "background thread" work must NOT touch UnityEngine.Object.
    ///
    /// This method itself still must be called on the main thread because it
    /// uses Unity APIs (SkinnedMeshRenderer, GameObject, etc.).
    /// </summary>
    public static GameObject[] BakeSkinnedHierarchyToStaticSafeForThreads(GameObject root)
    {
        // Any off-thread work you add should operate only on plain C# data.
        return BakeSkinnedHierarchyToStatic(root);
    }
}
