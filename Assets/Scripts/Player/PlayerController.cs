using System.Collections;
using System.Net;
using Unity.VisualScripting;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

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

	[SerializeField] private bool _isSprinting = false;
	[SerializeField] private bool _canSprint = true;
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
	public InputSystem_Actions Input => _input;

	#endregion

	[Header("Movement and Gravity")] [SerializeField]
	private float _currentMoveSpeed;

	[SerializeField, Range(4.0f, 8.0f)] private float _walkSpeed = 4.0f;
	[SerializeField, Range(6.0f, 20.0f)] private float _sprintSpeed = 8.0f;
	[SerializeField, Range(2.0f, 6.0f)] private float _aimingSpeed = 2.0f;
	[SerializeField] private float _gravityGrounded = -2.0f;
	[SerializeField] private float _gravityValue = -9.81f;
	[SerializeField] private float _fallMultiplier = 2.5f;
	[SerializeField] private float _staminaDrainRate = 5.0f;
	[SerializeField] private float _staminaRegenRate = 4.0f;
	[SerializeField] private float _backwardMovementModifier = 0.6f;
	[SerializeField] private float _sidewaysMovementModifier = 0.75f;
	
	// For momentum and acceleration
	private Vector3 _currentVelocity; 
	[Tooltip("Deceleration is 1.5 of Acceleration")]
	[SerializeField]private float _acceleration = 10f; //10 is default value - responsive and snappy
	[Tooltip("Deceleration is 1.5 of Acceleration")]
	[SerializeField]private float _deceleration = 15f; //15 default value - responsive and snappy
	[SerializeField]private float _sprintAcceleration = 12f; // Faster acceleration when starting to sprint
	[SerializeField]private float _aimDeceleration = 20f;    // Quick deceleration when aiming for responsiveness
	
	[Header("Camera Control")] [SerializeField]
	private Camera _camera;
	[SerializeField] private float _cameraSensitivity = 30.0f;
	[SerializeField] private bool _inverseCamera = false;
	[SerializeField] private float _cameraMinAngle = -60.0f;
	[SerializeField] private float _cameraMaxAngle = 60.0f;
	private float _currentXRotation = 0.0f;
	private Vector3 _originalCameraPosition;
	[SerializeField] private Vector3 _crouchedCameraPosition;
	
	[Header("Camera and Mouse Smoothing")]
	private Vector2 _currentMouseDelta = Vector2.zero;
	private Vector2 _targetMouseDelta = Vector2.zero;
	[SerializeField] private float _mouseSmoothTime = 0.03f; // Lower for more responsive, higher for smoother
	private Vector2 _currentMouseDeltaVelocity = Vector2.zero;
	private bool _isCrouching = false;

	[Header("Jump Sounds")] [SerializeField]
	private AudioClip[] _jumpSounds;

	[Header("Footstep Sounds")] [SerializeField]
	private AudioClip[] _footstepSounds;

	[SerializeField] private bool _enableFootstepSounds = true;
	[SerializeField] private float _footstepInterval = 0.5f;
	[SerializeField] private float _footstepSpeedTreshold = 0.5f;
	[SerializeField] private float _runningFootstepInterval = 0.3f;
	[SerializeField] private float _runningSpeedThreshold = 4f;
	[SerializeField] private float _aimingFootstepsInterval = 0.25f;
	[SerializeField] private float _aimingSpeedTreshold = 2f;
	private float _footstepTimer = 0f;

	[Header("ADS")] [SerializeField] private Transform _weaponPivot;
	[SerializeField] private bool _isAiming;
	private Vector3 _weaponPivotOriginalPosition;
	[SerializeField] private float _adsSmoothingTime;

	[Header("Aiming")] 
	[SerializeField] private Transform _aimPoint;

	[SerializeField] private float _aimRaycastDistance;
	[SerializeField] private LayerMask _aimingLayer;

	[Header("Misc")] 
	[SerializeField] private GameObject _flashLight;
	[SerializeField] private AudioClip _flashLightToggleSound;
	[SerializeField] private float _flashLightCurrentBattery;
	[SerializeField] private float _flashLightMaxBattery = 100;
	[SerializeField] private float _flashLightDrainRate = 0.5f;
	[SerializeField] private float _flashLightRechargeRate = 0.5f;
	private bool _flashlightActive = false;

	[Header("Interaction")] [SerializeField]
	private InteractionController _interactionController;
	
	private void OnEnable()
	{
		_input = new InputSystem_Actions();
		_input.Enable();
	}

	private void Awake()
	{
		_weaponPivotOriginalPosition = _weaponPivot.localPosition;
		_characterController = GetComponent<CharacterController>();
		_camera.tag = "MainCamera";
		_weaponInventory = GetComponent<WeaponInventory>();
		_stats = GetComponent<Stats>();
		_postProcessVolume = FindFirstObjectByType<Volume>();
		_postProcessVolume.profile.TryGet<LensDistortion>(out _lensDistortion);
		_flashLight.SetActive(false);
		if (!_interactionController)
		{
			_interactionController = GetComponent<InteractionController>();
		}
		_originalCameraPosition = _camera.transform.position;
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
			SetMoveSpeed();
			if (!_isSprinting && Mathf.Abs(_characterController.velocity.magnitude) < 10.0f)
			{
				_stats.RegenStamina(_staminaRegenRate);
			}
			Move();
			// Get the raw input
			_targetMouseDelta = _input.Player.Look.ReadValue<Vector2>();
			// Smoothly interpolate to target input
			_currentMouseDelta = Vector2.SmoothDamp(_currentMouseDelta, _targetMouseDelta, 
				ref _currentMouseDeltaVelocity, _mouseSmoothTime);
			LookUp();
			ToggleCrouch();
			RotatePlayer();
			TryFireWeapon();
			TryReloadWeapon();
			TrySwitchWeapon();
			CheckForAdsHeld();
			SetADS();
			SetAim();
			ToggleFlashLight(
				_input.Player.FlashlightToggle.WasPressedThisFrame());
			FlashLightDrainAndRecharge();
			TryToggleSemi();
			if (_input.Player.Help.WasPressedThisFrame())
			{
				_stats.TakeDamage(5);
			}
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
		
		if (_flashLightCurrentBattery <= 0.1f && _flashlightActive)
		{
			_flashlightActive = false;
			_flashLight.SetActive(false);
			AudioManager.Instance.PlayEffectDoubleVolume(_flashLightToggleSound);
		}
		TogglePauseMenu();
	}

	private void SetAim()
	{
		if (!_isAiming)
		{
			//Raycast
			RaycastHit outHit;
			Vector3 direction = _camera.transform.forward;
			Vector3 lookPosition;
			bool rayCast = Physics.Raycast
			(
				_camera.transform.position,
				direction,
				out outHit,
				_aimRaycastDistance,
				_aimingLayer
			);

			if (rayCast)
			{
				_aimPoint.position = outHit.point;
				lookPosition = outHit.point;
			}
			else
			{
				_aimPoint.position = _camera.transform.position + direction * _aimRaycastDistance;
				lookPosition = _camera.transform.position + direction * _aimRaycastDistance;
			}
			_weaponInventory.CurrentWeapon.SetMuzzlePointLookDirection(lookPosition);
		}
		
		else if (_isAiming)
		{
			//Raycast
			RaycastHit outHit;
			Vector3 direction = _weaponInventory.CurrentWeapon.AimReticleObject.forward;
			Vector3 lookPosition;
			bool rayCast = Physics.Raycast
			(
				_weaponInventory.CurrentWeapon.AimReticleObject.position,
				direction,
				out outHit,
				_aimRaycastDistance,
				_aimingLayer
			);

			if (rayCast)
			{
				_aimPoint.position = outHit.point;
				lookPosition = outHit.point;
			}
			else
			{
				_aimPoint.position = _weaponInventory.CurrentWeapon.AimReticleObject.position + direction * _aimRaycastDistance;
				lookPosition = _weaponInventory.CurrentWeapon.AimReticleObject.position + direction * _aimRaycastDistance;
			}
			_weaponInventory.CurrentWeapon.SetMuzzlePointLookDirection(lookPosition);
			Debug.DrawRay(_weaponInventory.CurrentWeapon.AimReticleObject.position, direction * _aimRaycastDistance, Color.red);

		}
		
		
	}

	private void ToggleFlashLight(bool input)
	{
		if (input)
		{
			_flashlightActive = !_flashlightActive;
			{
				_flashLight.SetActive(_flashlightActive);
				AudioManager.Instance.PlayEffectDoubleVolume(_flashLightToggleSound);
			}
		}
	}

	private void FlashLightDrainAndRecharge()
	{
		if (_flashlightActive)
		{
			_flashLightCurrentBattery -= _flashLightDrainRate * Time.deltaTime;
			HUDManager.Instance.SetFlashLightFill(_flashLightCurrentBattery, _flashLightMaxBattery);
		}
		else if(!_flashlightActive)
		{
			_flashLightCurrentBattery += _flashLightRechargeRate * Time.deltaTime;
			HUDManager.Instance.SetFlashLightFill(_flashLightCurrentBattery, _flashLightMaxBattery);
			if (_flashLightCurrentBattery >= _flashLightMaxBattery)
			{
				_flashLightCurrentBattery = _flashLightMaxBattery;
			}
		}
	}

	private void Move()
	{
    	_xInput = _input.Player.Move.ReadValue<Vector2>().x;
    	_yInput = _input.Player.Move.ReadValue<Vector2>().y;
	
    	
    	// Store base direction for determining movement type
    	Vector3 forwardMovement = transform.forward * _yInput;
    	Vector3 rightMovement = transform.right * _xInput;
	
    	// Apply strafe and backwards speed modifiers
    	if (_xInput != 0 && Mathf.Abs(_xInput) > Mathf.Abs(_yInput))
    	{
    	    // Primarily strafing side to side
    	    rightMovement *= _sidewaysMovementModifier;
    	}
    	
    	if (_yInput < 0)
    	{
    	    // Moving backwards
    	    forwardMovement *= _backwardMovementModifier;
    	}
	
    	Vector3 targetMoveDirection = forwardMovement + rightMovement;
    	
    	// Normalize only if there's actual input
    	if (targetMoveDirection.magnitude > 0.1f)
    	{
    	    targetMoveDirection.Normalize();
    	    
    	    // Calculate speed based on predominant direction
    	    float speedModifier = 1.0f;
    	    
    	    // Get angle between forward and input direction
    	    float movementAngle = Vector3.Angle(transform.forward, targetMoveDirection);
    	    
    	    // More precise directional speed modifiers using angles
    	    if (movementAngle > 135f && _yInput < 0)
    	    {
    	        // Moving predominantly backwards (135-180 degrees from forward)
    	        speedModifier = _backwardMovementModifier;
    	    }
    	    else if (movementAngle > 45f && movementAngle < 135f)
    	    {
    	        // Moving predominantly sideways (45-135 degrees from forward)
    	        speedModifier = _sidewaysMovementModifier;
    	        
    	        // Optional: slightly slower when strafing backward vs forward
    	        if (_yInput < 0)
    	        {
    	            speedModifier *= 0.9f;
    	        }
    	    }
    	    
    	    // Apply speed with modifier
    	    targetMoveDirection *= _currentMoveSpeed * speedModifier;
    	}
	
    	// Apply smooth acceleration toward target direction
    	_currentVelocity = Vector3.Lerp(_currentVelocity, targetMoveDirection, _acceleration * Time.deltaTime);
    	
    	// Apply deceleration when no input
    	if (targetMoveDirection.magnitude < 0.1f)
    	{
    	    _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, _deceleration * Time.deltaTime);
    	}
    	
    	// Calculate movement this frame
    	_moveDirection = _currentVelocity * Time.deltaTime;
    	
    	// Apply movement
    	_characterController.Move(_moveDirection);
    
    // Drain stamina when sprinting and actually moving
		if (_isSprinting && _canSprint && Mathf.Abs(_characterController.velocity.magnitude) > 5.0f)
		{
			_stats.DrainStamina(_staminaDrainRate);
		}
	}
	
	private void CheckForSprinting()
	{
		if (_stats.CurrentStamina <= 0.1f && _isSprinting)
		{
			_isSprinting = false;
			DisableSprint();
			return;
		}
    
		if (!_canSprint || _input.Player.Sprint.WasReleasedThisFrame())
		{
			_isSprinting = false;
			return;
		}
		if (_canSprint && _input.Player.Sprint.WasPressedThisFrame())
		{
			_isSprinting = true;
		}
	}

	private void DisableSprint()
	{
		_canSprint = false;
	}

	private void EnableSprint()
	{
		_canSprint = true;
	}
	
	private void RotatePlayer()
	{
		_mouseXDelta = _currentMouseDelta.x;
    
		transform.Rotate(transform.up * (_mouseXDelta * Time.deltaTime * _cameraSensitivity));
	}

	private void LookUp()
	{
		_mouseYDelta = _currentMouseDelta.y;
		float rotationAmount = (_mouseYDelta * _cameraSensitivity) * Time.deltaTime;

		if (_inverseCamera)
		{
			_currentXRotation += rotationAmount;
		}
		else
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

	private void CheckForAdsHeld()
	{
		if (_input.Player.Aim.IsPressed() && _weaponInventory.CurrentWeapon.CanADS)
		{
			_isAiming = true;
			_canSprint = false;
		}
		else
		{
			_isAiming = false;
			_canSprint = true;
		}
	}

	private void SetMoveSpeed()
	{
		// Determine target speed based on current state
		float targetSpeed;
    
		if (_stats.CurrentStamina <= 0.1f)
		{
			targetSpeed = _walkSpeed;
		}
		else if (_isSprinting && !_isAiming && _stats.CurrentStamina > 0.1f)
		{
			targetSpeed = _sprintSpeed;
		}
		else if (!_isSprinting && !_isAiming)
		{
			targetSpeed = _walkSpeed;
		}
		else if (_isAiming && !_isSprinting)
		{
			targetSpeed = _aimingSpeed;
		}
		else
		{
			// Fallback
			targetSpeed = _walkSpeed;
		}
    
		// Apply different acceleration rates based on state transition
		float transitionRate;
    
		if (targetSpeed > _currentMoveSpeed)
		{
			// Accelerating - use acceleration
			transitionRate = _acceleration;
        
			// If transitioning to sprint, use a faster sprint-specific acceleration
			if (targetSpeed == _sprintSpeed)
			{
				transitionRate = _sprintAcceleration; // You might want to add this variable, typically higher than regular acceleration
			}
		}
		else if (targetSpeed < _currentMoveSpeed)
		{
			// Decelerating - use deceleration
			transitionRate = _deceleration;
        
			// If transitioning to aiming, use a faster aim-specific deceleration for responsiveness
			if (targetSpeed == _aimingSpeed)
			{
				transitionRate = _aimDeceleration; // You might want to add this variable for quick aiming transitions
			}
		}
		else
		{
			// Already at target speed
			return;
		}
    
		// Smoothly transition to target speed
		_currentMoveSpeed = Mathf.Lerp(_currentMoveSpeed, targetSpeed, transitionRate * Time.deltaTime);
    
		// Optional: Add a small threshold check to snap to exact values when very close
		if (Mathf.Abs(_currentMoveSpeed - targetSpeed) < 0.05f)
		{
			_currentMoveSpeed = targetSpeed;
		}
	}
	
	private void SetADS()
	{
		if (_isAiming && _weaponInventory.CurrentWeapon.CanADS)
		{
			/*m_fAimSmoothingTime = m_fAimSmoothingTimeADS;*/
			_weaponPivot.localPosition = Vector3.Lerp(_weaponPivot.localPosition, _weaponInventory.CurrentWeapon.ADSPosition, _adsSmoothingTime * Time.deltaTime);
			HUDManager.Instance.DisableAimReticle();
			//only if I add snipers and scopes
			/*if (m_Type == WeaponType.Sniper || m_Type == WeaponType.DMR)
			{
				if (m_ScopeCamera)
				{
					m_ScopeCamera.fieldOfView = m_fOriginalZoom / m_fMagnificationFactor;
				}
			}*/
		}
		else
		{
			/*m_fAimSmoothingTime = m_fAimSmoothingTimeHip;*/
			_weaponPivot.localPosition = Vector3.Lerp(_weaponPivot.localPosition, _weaponPivotOriginalPosition, _adsSmoothingTime * Time.deltaTime);
			HUDManager.Instance.EnableAimReticle();
			//only if I add snipers and scopes
			/*if (m_ScopeCamera)
			{
				m_ScopeCamera.fieldOfView = m_fOriginalZoom;
			}*/
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

	private void TryToggleSemi()
	{
		if (_input.Player.ToggleFireMode.WasPressedThisFrame())
		{
			_weaponInventory.CurrentWeapon.ToggleSemi();
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
		if (_input.Player.SwitchWeapon.ReadValue<Vector2>().y > 0.1f)
		{
			_weaponInventory.IncrementWeaponIndex(); //this approach is overly simplistic - I am going to refactor this
		}
		if (_input.Player.SwitchWeapon.ReadValue<Vector2>().y < -0.1f)
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

	private void ToggleCrouch()
	{
		if (_input.Player.Crouch.WasCompletedThisFrame())
		{
			_isCrouching = !_isCrouching;
			if (_isCrouching)
			{
				Debug.Log("Currently crouching:" + _isCrouching);
			}
			else
			{
				Debug.Log("Currently crouching:" + _isCrouching);
			}
		}

	}
	
	private void UpdateFootsteps()
	{
		if (!_enableFootstepSounds || !_isGrounded) return;
   
		// Use the current velocity magnitude instead of moveDirection
		float movementSpeed = new Vector3(_currentVelocity.x, 0, _currentVelocity.z).magnitude;
   
		// Use a lower threshold when aiming to account for slower movement
		float currentThreshold = _isAiming ? _aimingSpeedTreshold : _footstepSpeedTreshold;
   
		if (movementSpeed < currentThreshold) 
		{
			_footstepTimer = 0;
			return;
		}
		_footstepTimer += Time.deltaTime;
   
		float currentInterval;
   
		// Determine the appropriate footstep interval based on movement state
		if (_isAiming)
		{
			currentInterval = _aimingFootstepsInterval;
		}
		else if (_isSprinting || movementSpeed > _runningSpeedThreshold)
		{
			currentInterval = _runningFootstepInterval;
		}
		else
		{
			currentInterval = _footstepInterval;
		}
   
		// Optionally adjust interval based on actual speed for smoother transitions
		float speedRatio = Mathf.Clamp01((movementSpeed - currentThreshold) / 
		                                 (_runningSpeedThreshold - currentThreshold));
    
		if (_isSprinting && !_isAiming)
		{
			// Keep sprint interval as is
		}
		else if (!_isAiming)
		{
			// Smoothly transition between walking and running intervals
			currentInterval = Mathf.Lerp(_footstepInterval, _runningFootstepInterval, speedRatio);
		}
   
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
