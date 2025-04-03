using System.Collections;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
	private CharacterController _characterController;
	private InputSystem_Actions _input;
	private WeaponInventory _weaponInventory;
	private Stats _stats;
	
	private float _xInput;
	private float _yInput;
	private float _mouseXDelta;
	private float _mouseYDelta;

	[SerializeField]private bool _isSprinting = false;
	private bool _canJump = true;
	[SerializeField, Range(0.0f, 5.0f)] private float _jumpForce = 1.0f;
	[SerializeField] private int _numberOfJumps = 2; //this is used to control the air jump
	private const int MAX_NUMBER_OF_JUMPS = 2; //necessary?
	private Vector3 _verticalVelocity;

	[SerializeField] private bool _isGrounded;
	
	private Vector3 _moveDirection;
	
	private Volume _postProcessVolume;
	private LensDistortion _lensDistortion;
	[SerializeField] private float _xOffset = 0.05f;
	[SerializeField] private float _yOffset = 0.5f;
	
	#region Properties

	public Vector3 MoveDirection => _moveDirection;
	public bool IsSprinting => _isSprinting;
	public CharacterController CharacterController => _characterController;
	public WeaponInventory WeaponInventory => _weaponInventory;
	
	#endregion

	[Header("Movement and Gravity")]
	[SerializeField] private float _currentMoveSpeed;
	[SerializeField, Range(4.0f, 36.0f)] private float _walkSpeed = 4.0f;
	[SerializeField, Range(16.0f, 72.0f)] private float _sprintSpeed = 8.0f;
	[SerializeField] private float _walkFraction = 0.5f;
	[SerializeField] private float _sprintFraction = 0.5f;
	[SerializeField] private float _gravityGrounded = -2.0f;
	[SerializeField] private float _gravityValue = -9.81f;
	[SerializeField] private float _fallMultiplier = 2.5f;
	[SerializeField] private float _staminaDrainRate = 5.0f;
	[SerializeField] private float _staminaRegenRate = 4.0f;
	
	[Header("Camera Control")]
	[SerializeField] private Camera _camera;
	[SerializeField] private float _cameraSensitivity = 30.0f;
	[SerializeField] private bool _inverseCamera = false;
	[SerializeField] private float _cameraMinAngle = -60.0f;
	[SerializeField] private float _cameraMaxAngle = 60.0f;
	private float _currentXRotation = 0.0f;

	[Header("Jump Sounds")] 
	[SerializeField] private AudioClip[] _jumpSounds;
	
	[Header("Footstep Sounds")]
	[SerializeField] private AudioClip[] _footstepSounds;
	[SerializeField] private bool _enableFootstepSounds = true;
	[SerializeField] private float _footstepInterval = 0.5f;
	[SerializeField] private float _footstepSpeedTreshold = 0.5f;
	[SerializeField] private float _runningFootstepInterval = 0.3f;
	[SerializeField] private float _runningSpeedThreshold = 4f;
	private float _footstepTimer = 0f;
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
		_stats = GetComponent<Stats>();
		_postProcessVolume = FindFirstObjectByType<Volume>();
		_postProcessVolume.profile.TryGet<LensDistortion>(out _lensDistortion);
	}

	private void Start()
	{

	}

	private void Update()
	{
		_isGrounded = _characterController.isGrounded;
		_footstepTimer += Time.deltaTime;
		if (GameManager.Instance.IsPlaying)
		{
			_canJump = _numberOfJumps > 0;
			if (_characterController.isGrounded)
			{
				_numberOfJumps = MAX_NUMBER_OF_JUMPS;
			}
			CheckForSprinting();
			if (!_isSprinting)
			{
				_stats.RegenStamina(_staminaRegenRate);
			}
			_currentMoveSpeed = _isSprinting ? _sprintSpeed : _walkSpeed;
			Move();
			LookUp();
			RotatePlayer();
			TryFireWeapon();
			TryReloadWeapon();
			TrySwitchWeapon();

			if (_characterController.isGrounded && _verticalVelocity.y < 0)
			{
				_verticalVelocity.y = _gravityGrounded;
			}
			else
			{
				if (_verticalVelocity.y < 0)
				{
					_verticalVelocity.y += _gravityValue * _fallMultiplier * Time.deltaTime;

				}
				else
				{
					_verticalVelocity.y += _gravityValue * _fallMultiplier * Time.deltaTime;
				}
			}
			
			Jump();

			Vector3 downwardForce = new Vector3(0.0f, _verticalVelocity.y, 0.0f) * Time.deltaTime;
			_characterController.Move(downwardForce);
			
			UpdateFootsteps();
		}
		else if (!GameManager.Instance.IsPlaying)
		{
			_verticalVelocity.y = 0;
		}
		TogglePauseMenu();
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
		if (_isSprinting && Mathf.Abs(_characterController.velocity.magnitude) > 5.0f)
		{
			_stats.DrainStamina(_staminaDrainRate);
		}
	}
	
	private void CheckForSprinting()
	{
		//ducttape - will refactor later
		
		if (_input.Player.Sprint.IsPressed() && _stats.CurrentStamina > 0)
		{
			_isSprinting = true;
		}
		else if (_input.Player.Sprint.WasReleasedThisFrame() || _stats.CurrentStamina <= 0)
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
		else if (!_weaponInventory.CurrentWeapon.IsSemi && _input.Player.Shoot.IsPressed())
		{
			_weaponInventory.CurrentWeapon.Fire();
		}
		else if (_input.Player.Shoot.WasReleasedThisFrame())
		{
			_weaponInventory.CurrentWeapon.StopMuzzleFlash();
		}
	}

	private void TryReloadWeapon()
	{
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

	private void Jump()
	{
		if (_input.Player.Jump.WasPressedThisFrame())
		{
			if (_canJump)
			{
				AudioManager.Instance.PlayEffect(_jumpSounds[GetRandomJumpSound()]);
				_verticalVelocity.y = Mathf.Sqrt(_jumpForce * -2f * _gravityValue);	
				_numberOfJumps--;
			}
		}
	}
	
	private void UpdateFootsteps()
	{
		if (!_enableFootstepSounds || !_isGrounded) return;
		
		float movementSpeed = new Vector3(_moveDirection.x, 0, _moveDirection.z).magnitude / Time.deltaTime;
		
		if (movementSpeed < _footstepSpeedTreshold) 
		{
			_footstepTimer = 0;
			return;
		}
		_footstepTimer += Time.deltaTime;
		
		float currentInterval = (_isSprinting || movementSpeed > _runningSpeedThreshold) ? 
			_runningFootstepInterval : _footstepInterval;
		
		if (_footstepTimer >= currentInterval)
		{
			AudioManager.Instance.PlayEffect(_footstepSounds[GetRandomWalkSound()]);
			_footstepTimer = 0f;
		}
	}
	private int GetRandomJumpSound()
	{
		return Random.Range(0, _jumpSounds.Length);
	}
	private int GetRandomWalkSound()
	{
		return Random.Range(0, _footstepSounds.Length);
	}
	private void OnDisable()
	{
		_input.Disable();
	}
	
	//post processing

	public void TriggerShake()
	{
		StartCoroutine(ScreenShake());
	}

	private IEnumerator ScreenShake()
	{
		_lensDistortion.active = true;

		float elapsed = 0.0f;

		while (elapsed < _weaponInventory.CurrentWeapon.ScreenShakeDuration)
		{
			float currentIntensity = Mathf.Lerp(
				_weaponInventory.CurrentWeapon.ScreenShakeIntensity,
				0,
				elapsed / _weaponInventory.CurrentWeapon.ScreenShakeDuration
			);

			float xDistortion = Mathf.Sin((Time.time * _weaponInventory.CurrentWeapon.ScreenShakeSpeed) * currentIntensity);
			float yDistortion = Mathf.Sin((Time.time * _weaponInventory.CurrentWeapon.ScreenShakeSpeed * 1.2f) * currentIntensity);
			
			_lensDistortion.intensity.Override(_xOffset * xDistortion);
			_lensDistortion.scale.Override(_yOffset + (yDistortion * 0.1f));

			elapsed += Time.deltaTime;
			yield return null;
		}
		
		_lensDistortion.intensity.Override(0);
		_lensDistortion.scale.Override(1f);
		_lensDistortion.active = false;

		StopCoroutine(ScreenShake());
	}
	
}
