using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    static ParticleManager t;
    public static ParticleManager T => t;
    [SerializeField]
    void Awake()
    {
        if (t != null)
        {
            Destroy(this);
            Debug.LogWarning($"Multiple Particle Managers, one on {name}");
            return;
        }
        t = this;
    }
}
