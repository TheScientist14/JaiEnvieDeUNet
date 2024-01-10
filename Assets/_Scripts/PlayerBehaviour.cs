using Cinemachine;
using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] private Vector2 camSens = new(100, 100);
    [SerializeField] private float playerSpeed = 2.0f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravityValue = -9.81f;


    private CharacterController _controller;
    private Vector3 _playerVelocity;
    private bool _groundedPlayer;
    private InputManager _inputManager;
    private Transform _camTransform;
    private CinemachineVirtualCamera _virtualCamera;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _inputManager = InputManager.instance;
        _camTransform = GetComponentInChildren<Camera>().transform;
        _virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
    }

    void Update()
    {
        _virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = camSens.x / 100;
        _virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = camSens.y / 100;

        _groundedPlayer = _controller.isGrounded;
        if (_groundedPlayer && _playerVelocity.y < 0)
        {
            _playerVelocity.y = 0f;
        }

        Vector3 move = new Vector3(_inputManager.GetPlayerMovement().x, 0, _inputManager.GetPlayerMovement().y);
        move = _camTransform.forward * move.z + _camTransform.right * move.x;
        move.y = 0f;
        _controller.Move(move * (Time.deltaTime * playerSpeed));

        // if (move != Vector3.zero)
        // {
        //     gameObject.transform.forward = move;
        // }

        // Changes the height position of the player..
        if (_inputManager.PlayerJumped() && _groundedPlayer)
        {
            _playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }

        _playerVelocity.y += gravityValue * Time.deltaTime;
        _controller.Move(_playerVelocity * Time.deltaTime);
    }
}