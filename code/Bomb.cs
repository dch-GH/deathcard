namespace DeathCard;

public partial class Bomb : ModelEntity
{
	/// <summary>
	/// The size of the explosion.
	/// </summary>
	[Net] 
	public float Size { get; set; } = 8f;

	/// <summary>
	/// The time it takes for the bomb to explode.
	/// </summary>
	[Net]
	public float Delay { get; set; } = 5f;

	private float size = Utility.Scale / 4f;
	private TimeSince sinceSpawn;

	public override void Spawn()
	{
		Tags.Add( "bomb" );
		sinceSpawn = 0f;
		Predictable = true;

		this.SetVoxelModel( "resources/grenade.vxmdl" );
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
		foreach ( var ent in nearby )
		{
			if ( ent == this || ent is not Bomb bomb || !bomb.IsValid || !IsAuthority )
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
		var result = results?.FirstOrDefault( tr => tr.Entity is ChunkEntity entity );
		if ( results == null )
			return false;

		var tr = result.Value;
		if ( tr.Entity is not ChunkEntity entity )
			return false;

		// Get position in voxel space.
		var parent = entity?.Parent;
		if ( parent == null )
			return false;

		var position = parent.WorldToVoxel( Position + Vector3.Down * parent.VoxelScale / 2f );

		// Remove voxels in a sphere.
		var center = new Vector3( position.x, position.y, position.z );

		for ( int x = 0; x <= Size; x++ )
		for ( int y = 0; y <= Size; y++ )
		for ( int z = 0; z <= Size; z++ )
		{
			var pos = center
				+ new Vector3( x, y, z )
				- Size / 2f;

			var dist = pos.Distance( center );
			if ( dist >= Size / 2f )
				continue;

			var target = (
				x: (pos.x + 0.5f).FloorToInt(),
				y: (pos.y + 0.5f).FloorToInt(),
				z: (pos.z + 0.5f).FloorToInt()
			);

			var local = parent.GetLocalSpace( target.x, target.y, target.z, out var chunk );
			var data = chunk?.GetDataByOffset( local.x, local.y, local.z );

			var col = (data?.Voxel?.Color ?? default).Multiply( 0.25f );
			var replace = dist >= Size / 2f - 1f && data?.Voxel != null
				? new Voxel( col.Clamp( 10 ) )
				: (Voxel?)null;

			// TODO: This should ideally be done on both,
			// fixing incorrect changes is broken at the moment.
			if ( Game.IsServer )
				parent.SetVoxel( target.x, target.y, target.z, replace );
		}

		return true;
	}

	private void Update()
	{
		if ( !IsAuthority )
			return;

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
			helper.Velocity *= 0.9f;

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
		GroundEntity = Velocity.z <= size / 2f
			? helper
				.TraceDirection( Vector3.Down * 2f )
				.Entity
			: null;
	}

	[GameEvent.Tick]
	private void Tick()
	{
		if ( !IsValid )
			return;

		// Call physics.
		Update();

		// Call explosion.
		if ( sinceSpawn < Delay )
			return;

		var exploded = Explode();
		if ( IsAuthority )
			Delete();

		sinceSpawn = 0;
	}
}
