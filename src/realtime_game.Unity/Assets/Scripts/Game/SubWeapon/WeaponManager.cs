using System.Linq;
using UnityEngine;

public class WeaponManager : MonoBehaviour {
    protected PlayerManager playerManager;
    protected NetworkObject networkObject;
    protected Rigidbody myRb;

    // VFX
    [SerializeField] protected GameObject fieldHitVFX;
    // VFXÇÃêeTransform
    protected Transform VFXParent;

    public float MaxLife { protected set; get; }
    public float Life { protected set; get; }
    public float AttackPower { protected set; get; }
    public float SmashPower { protected set; get; }
    public float ShotPower { protected set; get; }
    public float Radius { protected set; get; }

    private void Start() {
        VFXParent = GameObject.Find("VFX").transform;
    }
}
