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

	private const float size = 1f / 0.0254f;
	private TimeSince sinceSpawn;

	private static Vector2 Planar( Vector3 pos, Vector3 uAxis, Vector3 vAxis )
	{
		return new Vector2()
		{
			x = Vector3.Dot( uAxis, pos ),
			y = Vector3.Dot( vAxis, pos )
		};
	}

	public override void Spawn()
	{
		Tags.Add( "bomb" );
		sinceSpawn = 0f;

		const int faces = 6;
		var builder = Model.Builder;

		var material = Material.FromShader( "shaders/voxel.vmat" );
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();

		var positions = new Vector3[8]
		{
			new Vector3( -0.5f, -0.5f, 0.5f ) * size,
			new Vector3( -0.5f, 0.5f, 0.5f ) * size,
			new Vector3( 0.5f, 0.5f, 0.5f ) * size,
			new Vector3( 0.5f, -0.5f, 0.5f ) * size,
			new Vector3( -0.5f, -0.5f, -0.5f ) * size,
			new Vector3( -0.5f, 0.5f, -0.5f ) * size,
			new Vector3( 0.5f, 0.5f, -0.5f ) * size,
			new Vector3( 0.5f, -0.5f, -0.5f ) * size
		};

		var faceIndices = new int[4 * faces]
		{
			0, 1, 2, 3,
			7, 6, 5, 4,
			0, 4, 5, 1,
			1, 5, 6, 2,
			2, 6, 7, 3,
			3, 7, 4, 0,
		};

		var uAxis = new Vector3[]
		{
			Vector3.Forward,
			Vector3.Left,
			Vector3.Left,
			Vector3.Forward,
			Vector3.Right,
			Vector3.Backward,
		};

		var vAxis = new Vector3[]
		{
			Vector3.Left,
			Vector3.Forward,
			Vector3.Down,
			Vector3.Down,
			Vector3.Down,
			Vector3.Down,
		};

		var offset = 0;
		for ( var i = 0; i < faces; i++ )
		{
			var tangent = uAxis[i];
			var binormal = vAxis[i];
			var normal = Vector3.Cross( tangent, binormal );

			for ( var j = 0; j < 4; ++j )
			{
				var vertexIndex = faceIndices[(i * 4) + j];
				var pos = positions[vertexIndex];

				vertices.Add( new VoxelVertex()
				{
					position = pos,
					color = Color32.White
				} );
			}

			indices.Add( offset + 0 );
			indices.Add( offset + 2 );
			indices.Add( offset + 1 );
			indices.Add( offset + 2 );
			indices.Add( offset + 0 );
			indices.Add( offset + 3 );

			offset += 4;
		}

		mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

		// Create a model for the mesh.
		builder.AddMesh( mesh );
		
		Model = builder.Create();
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
			.Where( bomb => bomb.Position.Distance( Position ) < Size * size / 2f );

		var force = 1000f;
		foreach ( var entity in nearby )
		{
			if ( entity == this || entity is not Bomb bomb || !bomb.IsValid )
				continue;

			var normal = (bomb.Position - Position).Normal;
			bomb.GroundEntity = null;
			bomb.Velocity += normal * force;
		}

		// Find our closest chunk.
		var ray = new Ray( Position, Vector3.Down );
		var results = Trace.Box( Size * size / 2f, ray, 0f )
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
				.ToColor() * 0.05f).ToColor32();
			var replace = dist >= Size / 2f - 1f && old != null
				? new Voxel( new Color32( byte.Clamp( col.r, 20, 255 ), byte.Clamp( col.g, 20, 255 ), byte.Clamp( col.b, 20, 255 ) ) )
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
			helper.ApplyFriction( 10f, Time.Delta );

		helper.TryUnstuck();
		helper.TryMove( Time.Delta );

		// Apply new helper values and bounce.
		Position = helper.Position;

		var velocity = helper.Velocity;
		if ( Velocity.z < helper.Velocity.z ) // Bounce
		{
			velocity *= 0.25f;
			velocity += Vector3.Up * -Velocity.z * 0.5f;
		}

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

		var force = Game.Random.Float( 500f, 2000f );

		_ = new Bomb()
		{
			Size = Game.Random.Int( 2, 12 ),
			Delay = Game.Random.Float( 1, 5 ),
			Position = pawn.Position + pawn.ViewAngles.Forward * 50f,
			Velocity = pawn.ViewAngles.Forward * force
		};
	}
}
