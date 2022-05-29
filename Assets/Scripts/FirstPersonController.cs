using System.Collections;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif








namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;








		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		//private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		private PlayerInput _playerInput;
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;
		
		private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";


	
		private float base_x;
		private float base_y;

		private Quaternion base_rot;



		UnityEngine.Gyroscope gyro;

		private bool gyro_enabled = true;




		public TMP_Text text;





		IEnumerator InitializeGyro()
		{
			gyro.enabled = false;
			yield return new WaitForSeconds(4);
			gyro.enabled = true;
			yield return new WaitForSeconds(4);

			Debug.Log(Input.gyro.attitude); // attitude has data now
		}








		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
			
			gyro = Input.gyro;

		}

		void  Start()
		{


			Debug.Log("Has Gyro " + SystemInfo.supportsGyroscope);


			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
			_playerInput = GetComponent<PlayerInput>();


			base_rot = transform.rotation;



			if (gyro_enabled)
			{


				StartCoroutine(InitializeGyro());





				text.text = Input.gyro.enabled.ToString();

				Debug.Log(Input.gyro.attitude); // attitude has data now



				Quaternion q = gyro.attitude;

				base_x = q.eulerAngles.x;
				base_y = 90;

			}

			

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;




			Debug.Log(Input.gyro.enabled);
		}



		private void Update()
		{
			Fire();
			GroundedCheck();
			ResetGyro();
			Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}





		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
				base_rot = base_rot * Quaternion.Euler(0.0f, 1.0f * _rotationVelocity, 0.0f);
			}

			if (gyro.enabled)
			{
				//Quater
				Quaternion q = gyro.attitude;

				Debug.Log(q);

				float change_y = q.eulerAngles.y - base_y;

				if (change_y > 180) change_y -= 360;

				float change_x = q.eulerAngles.x - base_x;  //= q.eulerAngles.x - base_gyro.eulerAngles.x;
				transform.rotation = base_rot * Quaternion.Euler(0.0f, change_x, 0.0f);

				CinemachineCameraTarget.transform.localRotation = //Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
					Quaternion.Euler(ClampAngle(_cinemachineTargetPitch - change_y, BottomClamp, TopClamp * 2.0f), 0.0f, 0.0f);
			}

			else {
				transform.rotation = base_rot;
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
			}
		}


		private void Fire()
		{

			if (_input.jump)
			{

				RaycastHit hit;

				Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));

				if (Physics.Raycast(ray, out hit))
				{
					Debug.Log("I hit " + hit.collider.name);

					if (hit.transform.tag == "Target")
					{
						hit.transform.GetComponent<Renderer>().material.color = Color.red;
					}
				}
			}
		}



		private void ResetGyro() {

			if (_input.sprint)
            {
	

				Quaternion q = Input.gyro.attitude;

				text.text = SystemInfo.supportsGyroscope + " Boop " + Input.gyro.enabled.ToString();


				float change_x = q.eulerAngles.x - base_x;  //= q.eulerAngles.x - base_gyro.eulerAngles.x;
				base_rot *= Quaternion.Euler(0.0f, change_x, 0.0f);


				base_x = q.eulerAngles.x;

				float change_y = q.eulerAngles.y - base_y;

				if (change_y > 180) change_y -= 360;


				float current_angle = ClampAngle(_cinemachineTargetPitch - change_y, BottomClamp, TopClamp * 2.0f);


				if (current_angle > TopClamp)
				{
					base_y = q.eulerAngles.y - (current_angle - TopClamp);
					_cinemachineTargetPitch = TopClamp;
				}
				else if (current_angle < BottomClamp)
				{
					base_y = q.eulerAngles.y + (current_angle - BottomClamp) ;
					_cinemachineTargetPitch = BottomClamp;
				}
				else {
					base_y = q.eulerAngles.y;
					_cinemachineTargetPitch = current_angle;
				}

			}
		}


		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}


		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}