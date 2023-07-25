namespace DeathCard;

public class Bomb : ModelEntity
{
	/// <summary>
	/// The size of the explosion.
	/// </summary>
	public float Size { get; set; } = 8f;

	/// <summary>
	/// The time it takes for the bomb to explode.
	/// </summary>
	public float Delay { get; set; } = 5f;

	private float size = Utility.Scale / 4f;
	private TimeSince sinceSpawn;

	public override void Spawn()
	{
		Tags.Add( "bomb" );
		sinceSpawn = 0f;

		this.SetVoxelModel( "vox/grenade.vox", 1f );
		SetupPhysicsFromOBB( PhysicsMotionType.Static, -size / 2f, size / 2f );
	}

	/// <summary>
	/// Makes the Bomb explode.
	/// </summary>
	/// <returns>True if the bomb succesfully exploded.</returns>
	public bool Explode()
	{
		// Get all nearby bombs and apply a force to them.
		var nearby = Entity.All.OfType<Bomb>()
			.Where( bomb => bomb.Position.Distance( Position ) < Size * Utility.Scale / 2f );

		var force = 1000f;
		foreach ( var entity in nearby )
		{
			if ( entity == this || entity is not Bomb bomb || !bomb.IsValid )
				continue;

			var normal = (bomb.Position - Position).Normal;
			bomb.GroundEntity = null;
			bomb.Velocity += normal * force 
				+ Vector3.Up * force / 5f;
		}

		// Find our closest chunk.
		var ray = new Ray( Position, Vector3.Down );
		var results = Trace.Box( Size * Utility.Scale / 2f, ray, 0f )
			.Ignore( this )
			.WithTag( "chunk" )
			.IncludeClientside()
			.RunAll();

		// Check if our bomb is near a chunk.
		var result = results?.FirstOrDefault( tr => tr.Entity is ChunkEntity chunk );
		if ( results == null )
			return false;

		var tr = result.Value;
		if ( tr.Entity is not ChunkEntity chunk )
			return false;

		// Get position in voxel space.
		var parent = chunk?.Parent;
		var voxelData = parent?.GetClosestVoxel( Position + Vector3.Down * (parent.VoxelScale + size) / 2f );
		if ( voxelData?.Chunk == null )
			return false;

		// Remove voxels in a sphere.
		var data = voxelData.Value;
		var chunks = new List<Chunk>();
		for ( int x = 0; x <= Size; x++ )
		for ( int y = 0; y <= Size; y++ )
		for ( int z = 0; z <= Size; z++ )
		{
			var center = new Vector3( data.x, data.y, data.z );
			var position = center
				+ new Vector3( x, y, z )
				- Size / 2f;

			var dist = position.Distance( center );
			if ( dist >= Size / 2f )
				continue;

			var pos = (
				x: (position.x + 0.5f).FloorToInt(),
				y: (position.y + 0.5f).FloorToInt(),
				z: (position.z + 0.5f).FloorToInt()
			);
			var old = data.Chunk.GetVoxelByOffset( pos.x, pos.y, pos.z );
			var col = ((old?.Color ?? default)
				.ToColor() * 0.25f).ToColor32();
			var replace = dist >= Size / 2f - 1f && old != null
				? new Voxel( new Color32( byte.Clamp( col.r, 10, 255 ), byte.Clamp( col.g, 10, 255 ), byte.Clamp( col.b, 10, 255 ) ) )
				: (Voxel?)null;

			var res = data.Chunk.TrySetVoxel( pos.x, pos.y, pos.z, replace );
			chunks.AddRange( res.Except( chunks ) );
		}

		// Rebuild affected chunks.
		foreach ( var c in chunks )
			parent.GenerateChunk( c );

		Delete();
		return true;
	}

	private void Update()
	{
		// Set rotation.
		if ( !Velocity.IsNearlyZero( 1f ) )
			Rotation = Rotation.LookAt( Velocity.Normal );

		// Apply Gravity.
		if ( GroundEntity == null )
			Velocity += Game.PhysicsWorld.Gravity * Time.Delta;

		// Use move helper to advance.
		var helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace
			.WithAnyTags( "chunk", "world", "bomb" )
			.IncludeClientside()
			.Size( size )
			.Ignore( this );

		if ( GroundEntity != null )
			helper.ApplyFriction( 2f, Time.Delta );

		helper.TryUnstuck();
		helper.TryMove( Time.Delta );

		// Apply new helper values and bounce.
		Position = helper.Position;

		// Let's apply some bounce.
		var velocity = helper.Velocity;
		var bounced = false;
		if ( MathF.Abs( Velocity.z - velocity.z ) > 10 ) // Bounce on Z-axis.
		{
			velocity += Vector3.Up * -Velocity.z * 0.4f;
			velocity *= 0.5f;
			bounced = true;
		}

		if ( MathF.Abs( Velocity.x - velocity.x ) > 10 && !bounced ) // Bounce on X-axis.
		{
			velocity += Vector3.Forward * -Velocity.x * 0.4f;
			velocity *= 0.5f;
		}

		if ( MathF.Abs( Velocity.y - velocity.y ) > 10 && !bounced ) // Bounce on Y-axis.
		{
			velocity += Vector3.Left * -Velocity.y * 0.4f;
			velocity *= 0.5f;
		}

		// Finally apply final velocity.
		Velocity = velocity;

		// Check for ground collision.
		if ( Velocity.z <= size / 2f )
		{
			var tr = helper.TraceDirection( Vector3.Down * 2f );
			GroundEntity = tr.Entity;
		}
		else
			GroundEntity = null;
	}

	[GameEvent.Tick.Client]
	private void Tick()
	{
		if ( !IsValid )
			return;

		// Call physics.
		Update();

		// Call explosion.
		if ( sinceSpawn < Delay )
			return;

		if ( !Explode() )
			Delete();

		sinceSpawn = 0;
	}

	[GameEvent.Tick.Client]
	private static void SpawnBomb()
	{
		if ( Game.LocalPawn is not Pawn pawn )
			return;

		if ( !Input.Pressed( "use" ) )
			return;

		var force = 2000f;
		_ = new Bomb()
		{
			Size = Game.Random.Int( 2, 12 ),
			Delay = Game.Random.Float( 1, 5 ),
			Position = pawn.Position + pawn.ViewAngles.Forward * 50f,
			Velocity = force * pawn.ViewAngles.Forward
		};
	}
}
