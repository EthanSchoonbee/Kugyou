using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// Player stat-related variables
	public const float Speed = 200.0f;
	public const float JumpVelocity = -300.0f;
	public const float DashSpeed = 300.0f;
	public const float DashDuration = 0.3f; // Converted to seconds
	public const float DoubleTapTime = 0.3f; // Max time between taps (seconds)

	// Animation-related variables
	private AnimatedSprite2D _animationPlayer;

	// Attack-related variables
	private bool _isAttacking = false;
	private float _attackCooldown = 0.1f;
	private float _attackCooldownTimer = 0;

	// Landing-related variables
	private bool _wasOnFloor = true;
	private bool _isLanding = false;
	private Timer _landingTimer;

	// Dash-related variables
	private bool _isDashing = false;
	private float _dashTimeLeft = 0;
	private Vector2 _dashDirection = Vector2.Zero;
	private float _lastLeftTapTime = -1;
	private float _lastRightTapTime = -1;

	public override void _Ready()
	{
		// Initialize the AnimatedSprite2D reference
		_animationPlayer = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Initialize and add a timer for landing animation
		_landingTimer = new Timer();
		_landingTimer.OneShot = true;
		_landingTimer.Timeout += OnLandingAnimationFinished;
		AddChild(_landingTimer);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Update the player's facing direction based on the mouse position
		// UpdateFacingDirection();

		// Handle dashing
		if (_isDashing)
		{
			_dashTimeLeft -= (float)delta;

			if (_dashTimeLeft > 0)
			{
				velocity = _dashDirection * DashSpeed;
				Velocity = velocity;
				MoveAndSlide();
				return; // Skip the rest of the logic while dashing
			}
			else
			{
				_isDashing = false;
				if (!_isAttacking)
					_animationPlayer?.Play("idle");
			}
		}

		// Handle double-tap for dashing
		HandleDoubleTap();

		// Add gravity if not on floor
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle attack input
		if (Input.IsActionPressed("basic_attack") && _attackCooldownTimer <= 0 && !_isDashing)
		{
			PerformAttack();
			_attackCooldownTimer = _attackCooldown;
		}

		// Reduce attack cooldown timer
		if (_attackCooldownTimer > 0)
		{
			_attackCooldownTimer -= (float)delta;
		}

		// Handle movement and jumping
		HandleMovement(ref velocity);

		// Detect landing
		if (!_wasOnFloor && IsOnFloor() && !_isAttacking && !_isLanding)
		{
			TriggerLandingAnimation();
		}

		// Update previous floor state
		_wasOnFloor = IsOnFloor();

		// Update velocity and move
		Velocity = velocity;
		MoveAndSlide();
	}

	private void HandleMovement(ref Vector2 velocity)
	{
		// Allow jumping even during a dash, which cancels the dash
		if (Input.IsActionPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;

			if (_isDashing)
			{
				_isDashing = false; // Cancel the dash
			}

			if (!_isAttacking)
			{
				_animationPlayer?.Play("jump");
			}
		}

		// Skip handling other movement animations if currently dashing
		if (_isDashing)
		{
			return;
		}

		Vector2 direction = Input.GetVector("left", "right", "up", "down");

		// Horizontal movement
		if (direction != Vector2.Zero && !_isLanding)
		{
			velocity.X = direction.X * Speed;

			_animationPlayer.FlipH = direction.X < 0;

			if (!_isAttacking && IsOnFloor())
			{
				_animationPlayer?.Play("run");
			}
		}
		else if (IsOnFloor() && !_isLanding)
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			if (!_isAttacking)
			{
				_animationPlayer?.Play("idle");
			}
		}

		// Play jump animation if airborne and not attacking
		if (!IsOnFloor() && !_isAttacking)
		{
			_animationPlayer?.Play("jump");
		}
	}

	private void HandleDoubleTap()
	{
		float currentTime = Time.GetTicksMsec() / 1000.0f;

		if (Input.IsActionJustPressed("left"))
		{
			if (currentTime - _lastLeftTapTime <= DoubleTapTime)
			{
				StartDash(Vector2.Left);
			}
			_lastLeftTapTime = currentTime;
		}

		if (Input.IsActionJustPressed("right"))
		{
			if (currentTime - _lastRightTapTime <= DoubleTapTime)
			{
				StartDash(Vector2.Right);
			}
			_lastRightTapTime = currentTime;
		}
	}

	private void StartDash(Vector2 direction)
	{
		// Cancel attack if currently attacking
		if (_isAttacking)
		{
			_isAttacking = false;
			_animationPlayer.AnimationFinished -= OnAttackAnimationFinished;
		}

		if (_isDashing)
		{
			return; // Prevent starting another dash while already dashing
		}

		_isDashing = true;
		_dashTimeLeft = DashDuration;
		_dashDirection = direction.Normalized();
		
		if (IsOnFloor())
		{
			_animationPlayer?.Play("roll");
		}
		else
		{
			_animationPlayer?.Play("dash");
		}
	}

	private void PerformAttack()
	{
		// Cancel dash if currently dashing
		if (_isDashing)
		{
			_isDashing = false;
		}

		if (!_isAttacking)
		{
			_isAttacking = true;

			Vector2 mousePosition = GetGlobalMousePosition();
			_animationPlayer.FlipH = mousePosition.X < GlobalPosition.X;

			_animationPlayer?.Play("attack");

			_animationPlayer.AnimationFinished += OnAttackAnimationFinished;
		}
	}
	
	private void UpdateFacingDirection()
	{
		// Only update the direction when not attacking
		if (!_isAttacking)
		{
			Vector2 mousePosition = GetGlobalMousePosition();
			_animationPlayer.FlipH = mousePosition.X < GlobalPosition.X;
		}
	}

	private void OnAttackAnimationFinished()
	{
		_isAttacking = false;

		if (!IsOnFloor())
			_animationPlayer?.Play("jump");
		else
			_animationPlayer?.Play("idle");

		_animationPlayer.AnimationFinished -= OnAttackAnimationFinished;
	}

	private void TriggerLandingAnimation()
	{
		_isLanding = true;
		_animationPlayer?.Play("land");

		_landingTimer.WaitTime = 0.2f;
		_landingTimer.Start();
	}

	private void OnLandingAnimationFinished()
	{
		_isLanding = false;
	}
}
