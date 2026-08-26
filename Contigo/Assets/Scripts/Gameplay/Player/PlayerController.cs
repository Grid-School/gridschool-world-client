using UnityEngine;
using System.Collections.Generic;
using Core.Input;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed = 2f;
    public float SprintSpeed = 5.335f;
    public float SpeedChangeRate = 10f;

    [Header("Jumping")]
    public float JumpHeight = 18.2f;
    public float JumpCooldown = 0.1f; // Minimum time between jumps
    public float GroundCheckDistance = 0.1f; // Distance for raycast ground check
    
    [Header("Shader")]
    public Material skyboxMaterial;

    private Rigidbody _rb;
    private Animator _anim;
    private CapsuleCollider _col;

    private Vector2 _moveInput;
    private bool _sprintInput;

    // Jump and ground state variables
    private bool _jumpRequest = false;    // Set when a jump is requested in Update
    private bool _releasedJump = true;    // Tracks if jump key was released since last jump
    private float _lastJumpTime = -999f;  // Timestamp of the last jump
    private HashSet<Collider> _groundColliders = new HashSet<Collider>(); // Track ground colliders
    private bool _isGrounded;             // Current grounded state

    // Animator hashes
    private int _hSpeed, _hMotion, _hJump, _hGrounded, _hFreeFall;

    // Previous states for change detection
    private bool _prevIsGrounded;
    private bool _prevJumpRequest;
    private bool _prevReleasedJump;
    private bool _prevJumpPressed;
    private bool _prevAnimGrounded;
    private bool _prevAnimFreeFall;
    private bool _prevAnimJump;
    
    private PlayerCharacterInput _input;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
        _col = GetComponent<CapsuleCollider>();
        _input = GetComponent<PlayerCharacterInput>();

        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _hSpeed = Animator.StringToHash("Speed");
        _hMotion = Animator.StringToHash("MotionSpeed");
        _hJump = Animator.StringToHash("Jump");
        _hGrounded = Animator.StringToHash("Grounded");
        _hFreeFall = Animator.StringToHash("FreeFall");

        // Initial state allows jumping
        _releasedJump = true;
    }

    void Update()
    {
        if (_input == null)
        {
            Debug.LogError("[PlayerController] PlayerCharacterInput component not found!");
            return;
        }
        
        // Input handling
        _moveInput = _input.move;
        _sprintInput = _input.sprint;
        bool jumpPressed = _input.jump;

        // Update grounded state
        _isGrounded = IsGrounded();

        // Detect jump input edges
        bool risingEdge = jumpPressed && !_prevJumpPressed;
        bool fallingEdge = !jumpPressed && _prevJumpPressed;

        // Jump logic
        if (risingEdge && _isGrounded && Time.time - _lastJumpTime > JumpCooldown)
        {
            _jumpRequest = true;
            _lastJumpTime = Time.time;
        }

        if (fallingEdge)
        {
            _releasedJump = true;
        }

        // Update animator parameters
        _anim.SetBool(_hGrounded, _isGrounded);
        _anim.SetBool(_hFreeFall, !_isGrounded);
        if (_isGrounded)
            _anim.SetBool(_hJump, false);

        // Check for any boolean state changes and log if detected
        if (_isGrounded != _prevIsGrounded ||
            _jumpRequest != _prevJumpRequest ||
            _releasedJump != _prevReleasedJump ||
            jumpPressed != _prevJumpPressed ||
            _anim.GetBool(_hGrounded) != _prevAnimGrounded ||
            _anim.GetBool(_hFreeFall) != _prevAnimFreeFall ||
            _anim.GetBool(_hJump) != _prevAnimJump)
        {
            LogStateChanges(jumpPressed);
        }

        // Update previous states
        _prevIsGrounded = _isGrounded;
        _prevJumpRequest = _jumpRequest;
        _prevReleasedJump = _releasedJump;
        _prevJumpPressed = jumpPressed;
        _prevAnimGrounded = _anim.GetBool(_hGrounded);
        _prevAnimFreeFall = _anim.GetBool(_hFreeFall);
        _prevAnimJump = _anim.GetBool(_hJump);
    }

    void FixedUpdate()
    {
        var pm = PlanetManager.Instance; // Assumes a singleton managing planet properties
        if (pm == null || pm.PlanetCenter == null) return;

        Vector3 inward = (pm.PlanetCenter.position - transform.position).normalized;
        Vector3 outward = -inward;
        
        // Send player position to shader
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetVector("_PlayerUp", outward); // Use GetUp() equivalent
        }

        // Execute jump
        if (_jumpRequest)
        {
            float jumpVelocity = Mathf.Sqrt(JumpHeight * 20f * pm.GravityForce);
            _rb.AddForce(outward * jumpVelocity, ForceMode.VelocityChange);
            _anim.SetBool(_hJump, true);
            _jumpRequest = false;
        }

        // Apply gravity
        _rb.AddForce(inward * pm.GravityForce, ForceMode.Acceleration);

        // Movement handling
        float speed = _sprintInput ? SprintSpeed : MoveSpeed;
        Vector3 camF = Vector3.ProjectOnPlane(Camera.main.transform.forward, inward).normalized;
        Vector3 camR = Vector3.ProjectOnPlane(Camera.main.transform.right, inward).normalized;

        Vector3 desired = (_moveInput.sqrMagnitude > 0.001f)
            ? (camR * _moveInput.x + camF * _moveInput.y).normalized * speed
            : Vector3.zero;

        Vector3 worldV = _rb.linearVelocity;
        Vector3 vin = Vector3.Project(worldV, inward);
        Vector3 vplan = Vector3.ProjectOnPlane(worldV, inward);

        if (_isGrounded && Vector3.Dot(worldV, inward) > 0f)
            vin = Vector3.zero;

        vplan = Vector3.Lerp(vplan, desired, Time.fixedDeltaTime * SpeedChangeRate);
        _rb.linearVelocity = vplan + vin;

        if (desired.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(desired.normalized, outward);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, look, Time.fixedDeltaTime * 10f));
        }

        _anim.SetFloat(_hSpeed, _rb.linearVelocity.magnitude);
        _anim.SetFloat(_hMotion, _moveInput.magnitude);
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, GetUp()) > 0.5f)
            {
                _groundColliders.Add(collision.collider);
                break; // One qualifying contact is enough per collision
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        _groundColliders.Remove(collision.collider);
    }

    bool IsGrounded()
    {
        // Primary check: collision-based grounding
        if (_groundColliders.Count > 0)
            return true;

        // Fallback: raycast downward to detect ground
        Vector3 down = -GetUp();
        Vector3 start = transform.position + GetUp() * (_col.height * 0.5f - _col.radius);
        float distance = _col.radius + GroundCheckDistance;

        if (Physics.Raycast(start, down, out RaycastHit hit, distance))
        {
            if (Vector3.Dot(hit.normal, GetUp()) > 0.5f)
            {
                _groundColliders.Add(hit.collider);
                return true;
            }
        }

        return false;
    }

    Vector3 GetUp()
    {
        var pm = PlanetManager.Instance;
        return pm == null || pm.PlanetCenter == null
            ? Vector3.up
            : (transform.position - pm.PlanetCenter.position).normalized;
    }

    void LogStateChanges(bool jumpPressed)
    {
        float verticalDot = Vector3.Dot(_rb.linearVelocity, GetUp());
        Vector3 position = transform.position;

        Debug.Log($"[JUMP-STATE] Time: {Time.time:F2} | " +
                  $"isGrounded: {_isGrounded} | " +
                  $"jumpRequest: {_jumpRequest} | " +
                  $"releasedJump: {_releasedJump} | " +
                  $"jumpPressed: {jumpPressed} | " +
                  $"animGrounded: {_anim.GetBool(_hGrounded)} | " +
                  $"animFreeFall: {_anim.GetBool(_hFreeFall)} | " +
                  $"animJump: {_anim.GetBool(_hJump)} | " +
                  $"groundContacts: {_groundColliders.Count} | " +
                  $"timeSinceLastJump: {Time.time - _lastJumpTime:F2} | " +
                  $"verticalVelocityDot: {verticalDot:F2} | " +
                  $"position: ({position.x:F2}, {position.y:F2}, {position.z:F2})");
    }
}