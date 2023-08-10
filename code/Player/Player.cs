namespace DeathCard;

public partial class Player : ModelEntity
{
	static float scale = Utility.Scale * 0.8f;

	public BBox BBox = new BBox(
		new Vector3( -scale, -scale, 0 ) / 2f,
		new Vector3( scale, scale, scale * 4f ) / 2f
	);

	public Vector3 EyePosition { get; set; }

	public override void Spawn()
	{
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		Tags.Add( "player" );

		this.SetVoxelModel( "resources/player.vxmdl" );
		SetupPhysicsFromOBB( PhysicsMotionType.Keyframed, BBox.Mins, BBox.Maxs );
	}

	public override void ClientSpawn()
	{
		this.SetVoxelModel( "resources/player.vxmdl" );
	}

	public override void Simulate( IClient cl )
	{
		// Simulate movement.
		SimulateMovement();

		// Throw Grenade by pressing E.
		if ( Input.Pressed( "use" ) && Game.IsServer )
		{
			var force = 2000f;
			_ = new Bomb()
			{
				Size = Game.Random.Int( 2, 12 ),
				Delay = Game.Random.Float( 1, 5 ),
				Position = EyePosition + ViewAngles.Forward * 50f,
				Velocity = force * ViewAngles.Forward
			};
		}

		// Destruction
		var ray = new Ray( EyePosition, ViewAngles.Forward );
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

	public override void FrameSimulate( IClient cl )
	{
		Rotation = Rotation.FromYaw( ViewAngles.yaw );
		EyePosition = Position
			+ Vector3.Up * Utility.Scale * 2 * 0.7f;

		Camera.Position = Position 
			+ Vector3.Up * Utility.Scale * 2 * 0.7f;
		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 90f );
		Camera.FirstPersonViewer = this;
	}
}
