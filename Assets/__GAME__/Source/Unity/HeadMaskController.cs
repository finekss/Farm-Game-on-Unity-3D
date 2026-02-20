using UnityEngine;

public class HeadMaskController : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private Transform headBone;
    [SerializeField] private float radius = 0.25f;

    private Material materialInstance;

    void Awake()
    {
        materialInstance = meshRenderer.material;
    }

    void LateUpdate()
    {
        if (headBone == null) return;

        materialInstance.SetVector("_HeadPos", headBone.position);
        materialInstance.SetFloat("_Radius", radius);
    }
}