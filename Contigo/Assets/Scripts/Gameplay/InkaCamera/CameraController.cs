using UnityEngine;
using Gameplay.Managers;

namespace InkaCamera
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float distanceBehind = 5f;
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private float smoothTime = 0.3f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookUpOffset = 1f;

        private PlayerManager _playerManager;
        private Transform _playerTransform;
        private bool _isInitialized;
        private bool _isPlayerSet;
        private Vector3 _velocity = Vector3.zero;

        private void TryEnable()
        {
            if (_isInitialized && _playerTransform != null)
            {
                enabled = true;
            }
        }

        public void Initialize(PlayerManager playerManager)
        {
            _playerManager = playerManager;
            TrySubscribe();

            if (_playerManager.LocalPlayer != null)
                SetPlayerTransform(_playerManager.LocalPlayer.transform);

            _isInitialized = true;
            TryEnable();
        }

        public void SetPlayerTransform(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            _playerTransform = playerTransform;
            _isPlayerSet = true;
            TryEnable();
        }

        private void TrySubscribe()
        {
            if (_playerManager == null)
            {
                return;
            }

            _playerManager.OnLocalPlayerSpawned += (input) =>
            {
                SetPlayerTransform(input.transform);
            };
        }

        private void OnEnable()
        {
            if (!_isInitialized || !_isPlayerSet)
            {
                enabled = false;
                return;
            }
        }

        private void LateUpdate()
        {
            if (!_isInitialized || !_isPlayerSet || _playerTransform == null)
            {
                enabled = false;
                return;
            }

            Vector3 gravityUp = (_playerTransform.position - PlanetManager.Instance.PlanetCenter.position).normalized;
            Vector3 playerWorldPosition = _playerTransform.position;
            Vector3 relativePosition = new Vector3(0, heightOffset, -distanceBehind);
            Vector3 desiredPos = playerWorldPosition + _playerTransform.TransformDirection(relativePosition);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothTime);

            Vector3 lookAtTarget = playerWorldPosition + _playerTransform.forward * lookAheadDistance + gravityUp * lookUpOffset;
            Quaternion desiredRotation = Quaternion.LookRotation(lookAtTarget - transform.position, gravityUp);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

            Vector3 chatBubblePos = playerWorldPosition + new Vector3(0, 2.69f, 0); // Assuming chat bubble offset
        }
    }
}