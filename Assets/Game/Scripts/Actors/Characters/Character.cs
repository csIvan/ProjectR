using UnityEditor.MemoryProfiler;
using UnityEngine;


public enum CharacterStatus {
    Alive,
    Paused,
    Dead
}

public class Character : MonoBehaviour {

    [Header("Movement")]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float rotateSpeed;
    protected Vector3 MoveDirection;

    [Header("Jumping")]
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float airControl;
    [SerializeField] protected float fallMultiplier;
    [SerializeField] protected float lowJumpMultiplier;

    [Header("Ground Check")]
    [SerializeField] protected float groundCheckDistance;
    [SerializeField] protected Vector3 GroundCheckBoxExtents;
    [SerializeField] protected LayerMask GroundLayer;


    // References
    protected Rigidbody CharacterRigidbody;
    protected Rigidbody PlatformRigidbody;
    protected CharacterStatus Status;


    // Locomotion States
    public bool bMoving { get; protected set; }
    public bool bJumping { get; protected set; }
    public bool bOnPlatform { get; protected set; }



    // --------------------------------------------------------------------
    protected virtual void Awake() {
        CharacterRigidbody = GetComponent<Rigidbody>();
        Status = CharacterStatus.Paused;
        bMoving = false;
    }


    // --------------------------------------------------------------------
    protected virtual void OnEnable() {
        EventManager.OnGameStarted += HandleGameStart;
        EventManager.OnGameEnded += HandleGameOver;
        InputManager.OnPause += SetCharacterPause;
    }


    // --------------------------------------------------------------------
    protected virtual void OnDisable() {
        EventManager.OnGameStarted -= HandleGameStart;
        EventManager.OnGameEnded -= HandleGameOver;
        InputManager.OnPause -= SetCharacterPause;
    }


    // --------------------------------------------------------------------
    protected virtual void FixedUpdate() {
        if (Status == CharacterStatus.Alive) {
            AdjustGravityScale();          
        }
    }


    // --------------------------------------------------------------------
    protected virtual void HandleGameStart() {
        Status = CharacterStatus.Alive;
    }

    // --------------------------------------------------------------------
    protected virtual void HandleGameOver() {
        SetCharacterPause(true);
    }


    // --------------------------------------------------------------------
    private void AdjustGravityScale() {
        bool bFalling = CharacterRigidbody.linearVelocity.y < 0.0f && !IsGrounded();
        bool bEarlyJumpRelease = !bFalling && !bJumping;

        if (bFalling) {
            CharacterRigidbody.linearVelocity += Physics.gravity.y * fallMultiplier 
                                        * Time.fixedDeltaTime * Vector3.up;
        }
        else if(bEarlyJumpRelease) {
            CharacterRigidbody.linearVelocity += Physics.gravity.y * lowJumpMultiplier 
                                        * Time.fixedDeltaTime * Vector3.up;
        }
    }


    // --------------------------------------------------------------------
    protected bool IsGrounded() {
        Collider[] HitColliders = Physics.OverlapBox(transform.position, 
                        GroundCheckBoxExtents, Quaternion.identity, GroundLayer);

        return HitColliders.Length > 0;
    }


    // --------------------------------------------------------------------
    public virtual void SwitchRigidbodyMode(bool To3DMode) {
        CharacterRigidbody.rotation = Quaternion.identity; 
        if (To3DMode) {
            CharacterRigidbody.useGravity = true;
            CharacterRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else {
            CharacterRigidbody.useGravity = false;
            CharacterRigidbody.constraints = RigidbodyConstraints.FreezeRotation | 
                                             RigidbodyConstraints.FreezePositionY;
        }
    }


    // --------------------------------------------------------------------
    public virtual void SetCharacterPause(bool bPause) {
        Status = (bPause) ? CharacterStatus.Paused : CharacterStatus.Alive;
        CharacterRigidbody.isKinematic = bPause;

    }


    // --------------------------------------------------------------------
    private void OnTriggerStay(Collider other) {
        if (other.transform.parent && other.transform.parent.TryGetComponent(out Platform PlatformAI)) {
            bOnPlatform = true;
            PlatformRigidbody = PlatformAI.PlatformRigidbody;
        }
    }


    // --------------------------------------------------------------------
    private void OnTriggerExit(Collider other) {
        if (other.transform.parent && other.transform.parent.TryGetComponent(out Platform PlatformAI)) {
            bOnPlatform = false;
            PlatformRigidbody = null;
        }
    }


    // --------------------------------------------------------------------
    // Debugging Helper Functions
    private void OnDrawGizmos() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(transform.position, GroundCheckBoxExtents);
        Gizmos.DrawSphere(transform.position + transform.up * 0.5f, 0.025f);
        if(PlatformRigidbody != null) {
            Vector3 PlatformToPlayer = transform.position - PlatformRigidbody.position;
            Vector3 AngularVelocity = PlatformRigidbody.angularVelocity;
            Vector3 RotationalMovement = Vector3.Cross(AngularVelocity, PlatformToPlayer);
            Gizmos.DrawLine(transform.position, transform.position + RotationalMovement * 2.0f);
        }
    }

}