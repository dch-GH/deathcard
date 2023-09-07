namespace DeathCard;

partial class Player
{
	[Net, Predicted] public bool Noclip { get; set; }

	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	private float speed;

	private float moveSpeed => 180f;
	private float runMultiplier => 1.65f;
	private float acceleration => 10f;
	private float jumpForce => Utility.Scale;
	
	public override void BuildInput()
	{
		// Handle directional movement.
		InputDirection = Input.AnalogMove;

		// Handle mouse movement.
		var look = Input.AnalogLook;
		var viewAngles = ViewAngles + look;
		var pitch = viewAngles.pitch.Clamp( -90, 90 );
		ViewAngles = viewAngles
			.WithPitch( pitch )
			.Normal;
	}

	private void SimulateMovement()
	{
		// Set rotation & eye position.
		Rotation = Rotation.FromYaw( ViewAngles.yaw );
		EyePosition = Position
			+ Vector3.Up * Utility.Scale * 2 * 0.7f;

		// Noclip
		if ( Input.Pressed( "reload" ) )
			Noclip = !Noclip;

		if ( Noclip )
		{
			var up = Input.Down( "jump" ) 
				? Vector3.Up 
				: Input.Down( "duck" )
					? Vector3.Down
					: 0;

			Position += ((InputDirection * ViewAngles.ToRotation()).Normal + up) 
				* 1000f 
				* Time.Delta;

			Velocity = 0f;

			return;
		}

		// Apply gravity & jumping.
		if ( GroundEntity == null )
		{
			Velocity += Game.PhysicsWorld.Gravity * Time.Delta;
		}
		else if ( Input.Down( "jump" ) )
		{
			Velocity += jumpForce * 7.5f * Vector3.Up;
			GroundEntity = null;
		}

		// Start calculating velocity.
		var wishVelocity = (InputDirection * Rotation)
			.Normal;

		var targetSpeed = wishVelocity.Normal.Length
			* moveSpeed
			* (Input.Down( "run" ) ? runMultiplier : 1);

		speed = speed.LerpTo( targetSpeed, acceleration * Time.Delta );

		Velocity = (wishVelocity * speed)
			.WithZ( Velocity.z );

		// Initialize MoveHelper and set new values.
		var helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace
			.Size( BBox.Mins, BBox.Maxs )
			.Ignore( this )
			.WithAnyTags( "chunk", "solid" )
			.IncludeClientside();

		if ( helper.TryMove( Time.Delta ) > 0 )
		{
			Position = helper.Position;
			Velocity = helper.Velocity;
		}

		// Check for ground collision.
		if ( Velocity.z <= Utility.Scale / 4f )
		{
			var tr = helper.TraceDirection( Vector3.Down * 2f );
			GroundEntity = tr.Entity;

			// Move to the ground if there is something.
			if ( GroundEntity != null )
			{
				Position += tr.Distance * Vector3.Down;

				if ( Velocity.z < 0.0f )
					Velocity = Velocity.WithZ( 0 );
			}
		}
		else
			GroundEntity = null;
	}
}
