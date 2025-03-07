using UnityEngine;

 [RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
	private CharacterController _characterController;
	private InputSystem_Actions _input;
	private WeaponInventory _weaponInventory;
	
	private float _xInput;
	private float _yInput;
	private float _mouseXDelta;
	private float _mouseYDelta;

	private bool _isSprinting = false;

	private Vector3 _moveDirection;
	
	#region Properties

	public Vector3 MoveDirection => _moveDirection;
	#endregion

	[Header("Movement and Gravity")]
	[SerializeField] private float _currentMoveSpeed;
	[SerializeField, Range(1.0f, 12.0f)] private float _walkSpeed = 4.0f;
	[SerializeField, Range(4.0f, 24.0f)] private float _sprintSpeed = 8.0f;
	[SerializeField] private float _gravityGrounded = -2.0f;
	[SerializeField] private float _gravityValue = -9.81f;

	[Header("Camera Control")] [SerializeField]
	private Camera _camera;
	[SerializeField] private float _cameraSensitivity = 30.0f;
	[SerializeField] private bool _inverseCamera = false;
	[SerializeField] private float _cameraMinAngle = -60.0f;
	[SerializeField] private float _cameraMaxAngle = 60.0f;
	private float _currentXRotation = 0.0f;
	private void OnEnable()
	{
		_input = new InputSystem_Actions();
		_input.Enable();
	}

	private void Awake()
	{
		_characterController = GetComponent<CharacterController>();
		_camera.tag = "MainCamera";
		_weaponInventory = GetComponent<WeaponInventory>();
	}

	private void Start()
	{

	}

	private void Update()
	{
		CheckForSprinting();
		
		_currentMoveSpeed = _isSprinting ? _sprintSpeed : _walkSpeed;
		Move();
		LookUp();
		RotatePlayer();
		TogglePauseMenu();
		TryFireWeapon();
		TryReloadWeapon();
		TrySwitchWeapon();
		_moveDirection.y = _gravityGrounded;

		_characterController.Move(_moveDirection);
	}

	private void Move()
	{
		_xInput = _input.Player.Move.ReadValue<Vector2>().x;
		_yInput = _input.Player.Move.ReadValue<Vector2>().y;

		Vector3 forwardMovement = transform.forward * _yInput;
		Vector3 rightMovement = transform.right * _xInput;

		_moveDirection = forwardMovement + rightMovement;

		_moveDirection.Normalize();
		_moveDirection *= _currentMoveSpeed * Time.deltaTime;
		_characterController.Move(_moveDirection);
	}

	private void CheckForSprinting()
	{
		//ducttape - will refactor later
		
		if (_input.Player.Sprint.WasPressedThisFrame())
		{
			_isSprinting = true;
		}
		else if (_input.Player.Sprint.WasReleasedThisFrame())
		{
			_isSprinting = false;
		}
	}

	private void RotatePlayer()
	{
		_mouseXDelta = _input.Player.Look.ReadValue<Vector2>().x;
		
		transform.Rotate(transform.up * (_mouseXDelta * Time.deltaTime * _cameraSensitivity));
	}

	private void LookUp()
	{
		_mouseYDelta = _input.Player.Look.ReadValue<Vector2>().y;
		float rotationAmount = (_mouseYDelta * _cameraSensitivity) * Time.deltaTime;

		if (_inverseCamera)
		{
			_currentXRotation += rotationAmount;
		}
		else if (!_inverseCamera)
		{
			_currentXRotation -= rotationAmount;
		}

		_currentXRotation = Mathf.Clamp(_currentXRotation, _cameraMinAngle, _cameraMaxAngle);
		_camera.transform.localRotation = Quaternion.Euler(_currentXRotation, 0.0f, 0.0f);

		//add inversion
	}

	private void TogglePauseMenu()
	{
		if (_input.Player.Pause.WasPressedThisFrame())
		{
			if (GameManager.Instance.CurrentGameState == GameManager.GameState.Playing)
			{
				GameManager.Instance.PauseGame();
			}
			else if (GameManager.Instance.CurrentGameState == GameManager.GameState.Paused)
			{
				GameManager.Instance.ResumeGame();
			}
		}
	}

	private void TryFireWeapon()
	{
		if (_weaponInventory.CurrentWeapon.IsSemi && _input.Player.Shoot.WasPressedThisFrame())
		{
			_weaponInventory.CurrentWeapon.Fire();
		}
	}

	private void TryReloadWeapon()
	{
		//edgecases and stuff
		if (_input.Player.Reload.WasPressedThisFrame())
		{
			_weaponInventory.CurrentWeapon.Reload();
		}
	}

	private void TrySwitchWeapon()
	{
		if (_input.Player.SwitchWeaponUp.WasPressedThisFrame())
		{
			_weaponInventory.IncrementWeaponIndex();
		}
		if (_input.Player.SwitchWeaponDown.WasPressedThisFrame())
		{
			_weaponInventory.DecrementWeaponIndex();
		}
	}
	
	private void OnDisable()
	{
		_input.Disable();
	}
}
