using System.Linq;
using UnityEngine;

public static class SkinnedMeshBaker
{
    /// <summary>
    /// Bakes the current pose of a SkinnedMeshRenderer into a new Mesh and
    /// returns a new GameObject with a MeshFilter + MeshRenderer.
    /// 
    /// The returned GameObject:
    /// - Has no parent (is at the root of the hierarchy)
    /// - Appears in the exact same world position, rotation, and pose
    /// - Copies the materials from the original SkinnedMeshRenderer
    /// </summary>
    public static GameObject Bake(SkinnedMeshRenderer skinnedMeshRenderer) {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("SkinnedMeshBaker.Bake: skinnedMeshRenderer is null.");
            return null;
        }

        // 1. Bake the skinned mesh into a new Mesh
        Mesh bakedMesh = new Mesh();
        skinnedMeshRenderer.BakeMesh(bakedMesh);

        // 2. Create a new GameObject to hold the baked mesh
        GameObject bakedObject = new GameObject(skinnedMeshRenderer.name + "_Baked");

        // Ensure it has no parent
        bakedObject.transform.SetParent(null, worldPositionStays: false);

        // 3. Match world-space transform
        Transform originalTransform = skinnedMeshRenderer.transform;
        bakedObject.transform.position = originalTransform.position;
        bakedObject.transform.rotation = originalTransform.rotation;

        // Use lossyScale so the world-space scale matches even if the original had parents
        bakedObject.transform.localScale = originalTransform.lossyScale;

        // 4. Add MeshFilter and MeshRenderer and assign the baked mesh & materials
        MeshFilter meshFilter = bakedObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = bakedMesh;

        MeshRenderer meshRenderer = bakedObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials;

        return bakedObject;
    }
    public static GameObject[] BakeHiearchy(GameObject go) {
        return go.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s => Bake(s)).ToArray();
    }
}
