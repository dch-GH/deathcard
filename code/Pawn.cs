namespace DeathCard;

partial class Pawn : AnimatedEntity
{
	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		base.Spawn();

		//
		// Use a watermelon model
		//
		SetModel( "models/sbox_props/watermelon/watermelon.vmdl" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		Position = new Vector3( 1000, 2000, 1500 );
	}

	// An example BuildInput method within a player's Pawn class.
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;

		var viewAngles = ViewAngles;
		viewAngles += look;
		ViewAngles = viewAngles.Normal;
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		Rotation = ViewAngles.ToRotation();

		// build movement from the input values
		var movement = InputDirection.Normal;

		// rotate it to the direction we're facing
		Velocity = Rotation * movement;

		// apply some speed to it
		Velocity *= Input.Down( "run" ) ? 1000 : 200;

		// apply it to our position using MoveHelper, which handles collision
		// detection and sliding across surfaces for us
		MoveHelper helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace.Size( 16 );
		if ( helper.TryMove( Time.Delta ) > 0 )
		{
			Position = helper.Position;
		}

		// Throw Grenade by pressing E.
		if ( Input.Pressed( "use" ) && Game.IsServer )
		{
			var force = 2000f;
			_ = new Bomb()
			{
				Size = Game.Random.Int( 2, 12 ),
				Delay = Game.Random.Float( 1, 5 ),
				Position = Position + ViewAngles.Forward * 50f,
				Velocity = force * ViewAngles.Forward
			};
		}

		// Destruction
		var ray = new Ray( Position, ViewAngles.Forward );
		var tr = Trace.Ray( ray, 10000f )
			.IncludeClientside()
			.WithTag( "chunk" )
			.Run();			

		var parent = (tr.Entity as ChunkEntity)?.Parent;
		var position = parent?.WorldToVoxel( tr.EndPosition - tr.Normal * parent.VoxelScale / 2f );
		if ( position == null )
			return;

		var value = position.Value;
		var chunk = (Chunk)null;
		var pos = parent?.GetLocalSpace( value.x, value.y, value.z, out chunk );

		if ( Input.Pressed( "attack1" ) )
		{
			var size = 8;
			for ( int x = 0; x < size; x++ )
			for ( int y = 0; y < size; y++ )
			for ( int z = 0; z < size; z++ )
			{
				var center = (Vector3)pos;
				var target = center
					+ new Vector3( x, y, z )
					- size / 2f;

				if ( target.Distance( center ) >= size / 2f )
					continue;

				parent.SetVoxel(
					target.x.FloorToInt(),
					target.y.FloorToInt(),
					target.z.FloorToInt(), null, chunk );
			}
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		Rotation = ViewAngles.ToRotation();

		Camera.Position = Position;
		Camera.Rotation = Rotation;

		// Set field of view to whatever the user chose in options
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 90f );

		// Set the first person viewer to this, so it won't render our model
		Camera.FirstPersonViewer = this;
	}
}
