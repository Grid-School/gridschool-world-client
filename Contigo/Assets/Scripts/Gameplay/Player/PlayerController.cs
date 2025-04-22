// using Core.Initialization;
// using Core.Input;
// using UnityEngine;
// using Core.Networking;
// using Newtonsoft.Json;
// #if ENABLE_INPUT_SYSTEM 
// using UnityEngine.InputSystem;
// #endif
//
// namespace Gameplay.Player
// {
//     [RequireComponent(typeof(CharacterController))]
// #if ENABLE_INPUT_SYSTEM 
//     [RequireComponent(typeof(PlayerInput))]
// #endif
//     public class PlayerController : MonoBehaviour
//     {
//         [Header("Player")]
//         [Tooltip("Move speed of the character in m/s")]
//         public float MoveSpeed = 2.0f;
//
//         [Tooltip("Sprint speed of the character in m/s")]
//         public float SprintSpeed = 5.335f;
//
//         [Tooltip("How fast the character turns to face movement direction")]
//         [Range(0.0f, 0.3f)]
//         public float RotationSmoothTime = 0.12f;
//
//         [Tooltip("Acceleration and deceleration")]
//         public float SpeedChangeRate = 10.0f;
//
//         public AudioClip LandingAudioClip;
//         public AudioClip[] FootstepAudioClips;
//         [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
//
//         [Space(10)]
//         [Tooltip("The height the player can jump")]
//         public float JumpHeight = 4.8f;
//
//         [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
//         public float Gravity = -15.0f;
//
//         [Space(10)]
//         [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
//         public float JumpTimeout = 0.50f;
//
//         [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
//         public float FallTimeout = 0.15f;
//
//         [Header("Player Grounded")]
//         [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
//         public bool Grounded = true;
//
//         [Tooltip("Useful for rough ground")]
//         public float GroundedOffset = -0.14f;
//
//         [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
//         public float GroundedRadius = 0.28f;
//
//         [Tooltip("What layers the character uses as ground")]
//         public LayerMask GroundLayers;
//
//         // Player variables
//         private float _speed;
//         private float _animationBlend;
//         private float _targetRotation = 0.0f;
//         private float _rotationVelocity;
//         private float _verticalVelocity;
//         private float _terminalVelocity = 53.0f;
//
//         // Timeout variables
//         private float _jumpTimeoutDelta;
//         private float _fallTimeoutDelta;
//
//         // Animation IDs
//         private int _animIDSpeed;
//         private int _animIDGrounded;
//         private int _animIDJump;
//         private int _animIDFreeFall;
//         private int _animIDMotionSpeed;
//
// #if ENABLE_INPUT_SYSTEM 
//         private PlayerInput _playerInput;
// #endif
//         private Animator _animator;
//         private CharacterController _controller;
//         private PlayerCharacterInput _input;
//         private GameObject _mainCamera;
//
//         private bool _hasAnimator;
//         private bool _isRemotePlayer;
//
//         // Public properties
//         public bool IsJumping { get; private set; }
//         public float CurrentSpeed => _speed;
//         public float MotionSpeed => _input != null ? (_input.analogMovement ? _input.move.magnitude : 1f) : 0f;
//
//         private bool IsCurrentDeviceMouse
//         {
//             get
//             {
// #if ENABLE_INPUT_SYSTEM
//                 return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
// #else
//                 return false;
// #endif
//             }
//         }
//
//         private void Awake()
//         {
//             if (_mainCamera == null)
//             {
//                 _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
//             }
//             _isRemotePlayer = gameObject.tag == "RemotePlayer";
//
//             _controller = GetComponent<CharacterController>();
//             _hasAnimator = TryGetComponent(out _animator);
//         }
//
//         private void Start()
//         {
//             if (_isRemotePlayer) return;
//
//             if (_input == null)
//                 _input = GetComponent<PlayerCharacterInput>();
//
// #if ENABLE_INPUT_SYSTEM 
//             _playerInput = GetComponent<PlayerInput>();
// #endif
//
//             if (_input == null || _controller == null || _playerInput == null)
//             {
//                 Debug.LogError("Missing required components on Player!", this);
//                 return;
//             }
//
//             AssignAnimationIDs();
//             _jumpTimeoutDelta = JumpTimeout;
//             _fallTimeoutDelta = FallTimeout;
//         }
//
//         private void Update()
//         {
//             if (_isRemotePlayer) return;
//
//             JumpAndGravity();
//             GroundedCheck();
//             Move();
//         }
//
//         private void AssignAnimationIDs()
//         {
//             _animIDSpeed = Animator.StringToHash("Speed");
//             _animIDGrounded = Animator.StringToHash("Grounded");
//             _animIDJump = Animator.StringToHash("Jump");
//             _animIDFreeFall = Animator.StringToHash("FreeFall");
//             _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
//         }
//
//         private void GroundedCheck()
//         {
//             Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
//                 transform.position.z);
//             Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
//                 QueryTriggerInteraction.Ignore);
//
//             if (_hasAnimator && _animator != null)
//             {
//                 _animator.SetBool(_animIDGrounded, Grounded);
//             }
//         }
//
//         private void Move()
//         {
//             if (_input == null || _controller == null) return;
//
//             // Determine target speed based on sprint state
//             float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
//             if (_input.move == Vector2.zero)
//             {
//                 targetSpeed = 0.0f;
//                 _input.sprint = false; // Force-clear sprint when not moving
//             }
//
//             float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
//             float speedOffset = 0.1f;
//             float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
//
//             // Smoothly adjust the current speed toward the target speed
//             if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
//             {
//                 _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
//                 _speed = Mathf.Round(_speed * 1000f) / 1000f;
//             }
//             else
//             {
//                 _speed = targetSpeed;
//             }
//
//             if (targetSpeed == 0.0f)
//             {
//                 _speed = 0.0f;
//             }
//
//             _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
//             if (_animationBlend < 0.01f || targetSpeed == 0.0f) _animationBlend = 0f;
//
//             Vector3 movement = Vector3.zero;
//             if (_input.move != Vector2.zero)
//             {
//                 if (_input.move.y >= 0)
//                 {
//                     Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
//                     float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
//                     _targetRotation = (_mainCamera != null ? _mainCamera.transform.eulerAngles.y : 0f) + targetAngle;
//                     float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
//                     transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
//
//                     Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
//                     movement = targetDirection.normalized;
//                 }
//                 else
//                 {
//                     Vector3 backwardInput = new Vector3(_input.move.x, 0.0f, _input.move.y);
//                     movement = (transform.right * backwardInput.x - transform.forward * Mathf.Abs(backwardInput.z)).normalized;
//                 }
//             }
//
//             if (_speed > 0) Debug.Log($"[MOVE EXECUTED] Final movement={movement} | speed={_speed} | verticalVelocity={_verticalVelocity}");
//
//             _controller.Move(movement * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
//
//             SendMovementToServer(inputMagnitude);
//
//             if (_hasAnimator && _animator != null)
//             {
//                 _animator.SetFloat(_animIDSpeed, _animationBlend);
//                 _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
//             }
//         }
//
//         private bool isSendingMovement = false;
//         private float lastLogTime = 0;
//         private float logInterval = 0.1f;
//
//         private void SendMovementToServer(float inputMagnitude)
//         {
//             if (isSendingMovement || GameInitializer.NetworkManagerInstance == null) return;
//             isSendingMovement = true;
//
//             var inputMessage = new InputMessage
//             {
//                 X = transform.position.x,
//                 Y = transform.position.y,
//                 Z = transform.position.z,
//                 Angle = transform.eulerAngles.y,
//                 Speed = _animationBlend,
//                 MotionSpeed = inputMagnitude,
//                 Jump = IsJumping,
//                 Grounded = Grounded,
//                 FreeFall = !Grounded && _fallTimeoutDelta <= 0f
//             };
//             string json = JsonConvert.SerializeObject(inputMessage);
//             GameInitializer.NetworkManagerInstance.SendMessage(json);
//
//             if (Time.time - lastLogTime >= logInterval)
//             {
//                 Debug.Log($"[PlayerController] Sent position: ({inputMessage.X}, {inputMessage.Y}, {inputMessage.Z}), Jump={inputMessage.Jump}, Grounded={inputMessage.Grounded}, FreeFall={inputMessage.FreeFall}");
//                 lastLogTime = Time.time;
//             }
//             isSendingMovement = false;
//         }
//
//         public void InjectInput(PlayerCharacterInput input)
//         {
//             _input = input;
//             AssignAnimationIDs();
//             _jumpTimeoutDelta = JumpTimeout;
//             _fallTimeoutDelta = FallTimeout;
//         }
//
//         private void JumpAndGravity()
//         {
//             if (_controller == null) return;
//
//             if (Grounded)
//             {
//                 _fallTimeoutDelta = FallTimeout;
//
//                 if (_hasAnimator && _animator != null)
//                 {
//                     _animator.SetBool(_animIDJump, false);
//                     _animator.SetBool(_animIDFreeFall, false);
//                 }
//
//                 if (_verticalVelocity < 0.0f)
//                 {
//                     _verticalVelocity = -2f;
//                 }
//
//                 if (_input != null && _input.jump && _jumpTimeoutDelta <= 0.0f)
//                 {
//                     _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
//
//                     if (_hasAnimator && _animator != null)
//                     {
//                         _animator.SetBool(_animIDJump, true);
//                         IsJumping = true;
//                     }
//                 }
//
//                 if (_jumpTimeoutDelta >= 0.0f)
//                 {
//                     _jumpTimeoutDelta -= Time.deltaTime;
//                 }
//             }
//             else
//             {
//                 _jumpTimeoutDelta = JumpTimeout;
//
//                 if (_fallTimeoutDelta >= 0.0f)
//                 {
//                     _fallTimeoutDelta -= Time.deltaTime;
//                 }
//                 else
//                 {
//                     if (_hasAnimator && _animator != null)
//                     {
//                         _animator.SetBool(_animIDFreeFall, true);
//                     }
//                 }
//
//                 if (_input != null)
//                 {
//                     _input.jump = false;
//                 }
//                 IsJumping = false;
//             }
//
//             if (_verticalVelocity < _terminalVelocity)
//             {
//                 _verticalVelocity += Gravity * Time.deltaTime;
//             }
//         }
//
//         private void OnDrawGizmosSelected()
//         {
//             Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
//             Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
//
//             if (Grounded) Gizmos.color = transparentGreen;
//             else Gizmos.color = transparentRed;
//
//             Gizmos.DrawSphere(
//                 new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
//                 GroundedRadius);
//         }
//
//         private void OnFootstep(AnimationEvent animationEvent)
//         {
//             if (_isRemotePlayer || _controller == null) return;
//
//             if (animationEvent.animatorClipInfo.weight > 0.5f)
//             {
//                 if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
//                 {
//                     var index = Random.Range(0, FootstepAudioClips.Length);
//                     AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
//                 }
//                 else
//                 {
//                     Debug.LogWarning("FootstepAudioClips is not assigned or empty!", this);
//                 }
//             }
//         }
//
//         private void OnLand(AnimationEvent animationEvent)
//         {
//             if (_isRemotePlayer || _controller == null) return;
//
//             if (animationEvent.animatorClipInfo.weight > 0.5f)
//             {
//                 if (LandingAudioClip != null)
//                 {
//                     AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
//                 }
//                 else
//                 {
//                     Debug.LogWarning("LandingAudioClip is not assigned!", this);
//                 }
//             }
//         }
//     }
//
//     // Define InputMessage to match server expectations
//     public class InputMessage
//     {
//         public float X { get; set; }
//         public float Y { get; set; }
//         public float Z { get; set; }
//         public float Angle { get; set; }
//         public float Speed { get; set; }
//         public float MotionSpeed { get; set; }
//         public bool Jump { get; set; }
//         public bool Grounded { get; set; }
//         public bool FreeFall { get; set; }
//     }
// }


using UnityEngine;
using Core.Networking;
using Core.Input;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace Gameplay.Player
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class PlayerController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 4.8f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private float _lastSendTime = 0f;
        private const float SendInterval = 0.1f;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private PlayerCharacterInput _input;
        private GameObject _mainCamera;
        private InkaNetworkManager _networkManager;

        private bool _hasAnimator;
        private bool _isRemotePlayer;

        public bool IsJumping { get; private set; }
        public float CurrentSpeed => _speed;
        public float MotionSpeed => _input != null ? (_input.analogMovement ? _input.move.magnitude : 1f) : 0f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        public void Initialize(InkaNetworkManager networkManager, PlayerCharacterInput input)
        {
            _networkManager = networkManager;
            _input = input;

            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            _isRemotePlayer = gameObject.tag == "RemotePlayer";

            _controller = GetComponent<CharacterController>();
            _hasAnimator = TryGetComponent(out _animator);

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#endif
            
            if (_isRemotePlayer)
            {
                // Disable local components for remote players
                if (_playerInput != null) Destroy(_playerInput);
                return; // Skip further initialization for remote players
            }

            if (_input == null || _controller == null || (_playerInput == null && !_isRemotePlayer))
            {
                Debug.LogError("Missing required components on Player!", this);
                return;
            }

            AssignAnimationIDs();
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            if (_isRemotePlayer) return;

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator && _animator != null)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void Move()
        {
            if (_input == null || _controller == null)
            {
                return;
            }

            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            if (_input.move == Vector2.zero)
            {
                targetSpeed = 0.0f;
                _input.sprint = false;
            }

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            if (targetSpeed == 0.0f)
            {
                _speed = 0.0f;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f || targetSpeed == 0.0f) _animationBlend = 0f;

            Vector3 movement = Vector3.zero;
            if (_input.move != Vector2.zero)
            {
                if (_input.move.y >= 0)
                {
                    Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                    float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                    _targetRotation = (_mainCamera != null ? _mainCamera.transform.eulerAngles.y : 0f) + targetAngle;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

                    Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                    movement = targetDirection.normalized;
                }
                else
                {
                    Vector3 backwardInput = new Vector3(_input.move.x, 0.0f, _input.move.y);
                    movement = (transform.right * backwardInput.x - transform.forward * Mathf.Abs(backwardInput.z)).normalized;
                }
            }

            _controller.Move(movement * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            SendMovementToServer(inputMagnitude);

            if (_hasAnimator && _animator != null)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private async void SendMovementToServer(float inputMagnitude)
        {
            if (Time.time - _lastSendTime < SendInterval || _networkManager == null) return;

            _lastSendTime = Time.time;

            var inputMessage = new InputMessage
            {
                X = transform.position.x,
                Y = transform.position.y,
                Z = transform.position.z,
                Angle = transform.eulerAngles.y,
                Speed = _animationBlend,
                MotionSpeed = inputMagnitude,
                Jump = IsJumping,
                Grounded = Grounded,
                FreeFall = !Grounded && _fallTimeoutDelta <= 0f
            };
            string json = JsonUtility.ToJson(inputMessage);
            await _networkManager.SendMessageAsync(json);
        }

        public void InjectInput(PlayerCharacterInput input)
        {
            _input = input;
            AssignAnimationIDs();
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void JumpAndGravity()
        {
            if (_controller == null) return;

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator && _animator != null)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input != null && _input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator && _animator != null)
                    {
                        _animator.SetBool(_animIDJump, true);
                        IsJumping = true;
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator && _animator != null)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                if (_input != null)
                {
                    _input.jump = false;
                }
                IsJumping = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (_isRemotePlayer || _controller == null) return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (_isRemotePlayer || _controller == null) return;

            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (LandingAudioClip != null)
                {
                    AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }
    }
    
    [System.Serializable]
    public struct InputMessage
    {
        public float X;
        public float Y;
        public float Z;
        public float Angle;
        public float Speed;
        public float MotionSpeed;
        public bool Jump;
        public bool Grounded;
        public bool FreeFall;
    }
}