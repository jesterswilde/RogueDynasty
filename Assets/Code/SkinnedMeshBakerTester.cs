using UnityEngine;

public class SkinnedMeshBakerTester : MonoBehaviour
{
    [Header("Root with SkinnedMeshRenderers")]
    [SerializeField]
    private GameObject rootObject;

    [Header("Options")]
    [SerializeField]
    private bool copyMaterials = true;

    [SerializeField]
    private bool disableOriginalSkinnedRenderers = false;

    [Header("Debug (runtime)")]
    [SerializeField]
    private GameObject[] bakedObjects;

    private bool _hasRun;

    private void OnGUI()
    {
        const float width = 260f;
        const float height = 40f;

        Rect rect = new Rect(10f, 10f, width, height);

        GUI.enabled = rootObject != null;

        if (GUI.Button(rect, "Bake Skinned Hierarchy To Static"))
        {
            RunBake();
        }

        GUI.enabled = true;
    }

    private void RunBake()
    {
        if (rootObject == null)
        {
            Debug.LogWarning("SkinnedMeshBakerTester: Root object is null, nothing to bake.");
            return;
        }

        // This call must run on the main thread.
        bakedObjects = SkinnedMeshBaker.BakeHiearchy(
            rootObject
        );

        _hasRun = true;

        Debug.Log($"SkinnedMeshBakerTester: Baked {bakedObjects.Length} mesh(es) from '{rootObject.name}'.");
    }
}
