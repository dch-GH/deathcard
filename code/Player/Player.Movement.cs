namespace DeathCard;

partial class Player
{
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

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
		if ( Input.Down( "reload" ) )
		{
			Position += (InputDirection * ViewAngles.ToRotation()).Normal * 1000f * Time.Delta;
			return;
		}

		// Apply gravity & jumping.
		if ( GroundEntity == null )
			Velocity += Game.PhysicsWorld.Gravity * Time.Delta;
		else if ( Input.Pressed( "jump" ) )
		{
			Velocity = Utility.Scale * 8 * Vector3.Up;
			GroundEntity = null;
		}

		// Start calculating velocity.
		var speed = 250f;
		var wishVelocity = (InputDirection * Rotation)
			.Normal;

		Velocity = (wishVelocity.WithZ( 0 ) * speed)
			.WithZ( Velocity.z );

		// Initialize MoveHelper and set new values.
		var step = Utility.Scale / 2f;
		var helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace
			.Size( BBox.Mins, BBox.Maxs )
			.Ignore( this )
			.WithAnyTags( "chunk", "solid" )
			.IncludeClientside();

		helper.TryUnstuck();

		if ( helper.TryMoveWithStep( Time.Delta, step ) > 0 )
		{
			Position = helper.Position;
			Velocity = helper.Velocity;
		}

		// Check for ground collision.
		if ( Velocity.z <= step )
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
