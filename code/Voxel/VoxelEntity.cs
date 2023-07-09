namespace DeathCard;

public struct VoxelData
{
	public Voxel? Voxel;
	public Chunk Chunk;

	public ushort x;
	public ushort y;
	public ushort z;
}

public partial class VoxelEntity : ModelEntity
{
	public Dictionary<Chunk, ModelEntity> Entities = new();

	public float VoxelScale { get; set; } = 1f / 0.0254f;

	public Chunk[,,] Chunks { get; private set; }
	public int Width { get; private set; }
	public int Depth { get; private set; }
	public int Height { get; private set; }

	public VoxelEntity( Vector3? position = null )
	{
		Position = position ?? Vector3.Zero;

		Chunks = new Chunk[4, 4, 4];

		Width = Chunks.GetLength( 0 );
		Depth = Chunks.GetLength( 1 );
		Height = Chunks.GetLength( 2 );

		for ( ushort x = 0; x < Chunks.GetLength( 0 ); x++ )
		for ( ushort y = 0; y < Chunks.GetLength( 1 ); y++ )
		for ( ushort z = 0; z < Chunks.GetLength( 2 ); z++ )
			Chunks[x, y, z] = new( x, y, z, entity: this );

		foreach ( var chunk in Chunks )
			GenerateChunk( chunk );
	}

	public void OnChunkChanged( Chunk chunk, ushort x, ushort y, ushort z )
	{
		GenerateChunk( chunk );
	}

	public void GenerateChunk( Chunk chunk )
	{
		// Get our chunk's entity.
		ModelEntity chunkEntity;
		if ( !Entities.TryGetValue( chunk, out chunkEntity ) )
			Entities.Add( chunk, chunkEntity = new ModelEntity() );

		var positions = new Vector3[]
		{
			new Vector3( -0.5f, -0.5f, 0.5f ) * VoxelScale,
			new Vector3( -0.5f, 0.5f, 0.5f ) * VoxelScale,
			new Vector3( 0.5f, 0.5f, 0.5f ) * VoxelScale,
			new Vector3( 0.5f, -0.5f, 0.5f ) * VoxelScale,
			new Vector3( -0.5f, -0.5f, -0.5f ) * VoxelScale,
			new Vector3( -0.5f, 0.5f, -0.5f ) * VoxelScale,
			new Vector3( 0.5f, 0.5f, -0.5f ) * VoxelScale,
			new Vector3( 0.5f, -0.5f, -0.5f ) * VoxelScale,
		};

		var faceIndices = new int[]
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

		// Let's create a mesh.
		var material = Material.FromShader( "shaders/voxel.shader" );
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
		var offset = 0;

		for ( ushort x = 0; x < chunk.Width; x++ )
		for ( ushort y = 0; y < chunk.Depth; y++ )
		for ( ushort z = 0; z < chunk.Height; z++ )
		{
			var voxel = chunk.GetVoxel( x, y, z );
			if ( voxel == null )
				continue;

			var faces = 6;
			var shouldHide = new bool[faces];
			var neighbors = new (short x, short y, short z)[]
			{
				(0, 0, 1),
				(0, 0, -1),
				(-1, 0, 0),
				(0, 1, 0),
				(1, 0, 0),
				(0, -1, 0),
			};

			var drawCount = 0;
			for ( var i = 0; i < faces; i++ )
			{
				var direction = neighbors[i];
				var neighbour = chunk.GetVoxelByOffset( (short)(x + direction.x), (short)(y + direction.y), (short)(z + direction.z) );
				if ( neighbour != null )
					continue;

				var tangent = uAxis[i];
				var binormal = vAxis[i];
				var normal = Vector3.Cross( tangent, binormal );

				for ( var j = 0; j < 4; ++j )
				{
					var vertexIndex = faceIndices[(i * 4) + j];
					var pos = positions[vertexIndex]
						+ new Vector3( x, y, z ) * VoxelScale;

					vertices.Add( new VoxelVertex( pos, normal, voxel.Value.Color ) );
				}

				indices.Add( offset + drawCount * 4 + 0 );
				indices.Add( offset + drawCount * 4 + 2 );
				indices.Add( offset + drawCount * 4 + 1 );
				indices.Add( offset + drawCount * 4 + 2 );
				indices.Add( offset + drawCount * 4 + 0 );
				indices.Add( offset + drawCount * 4 + 3 );

				drawCount++;
			}

			offset += 4 * drawCount;
		}

		var ind = indices.ToArray();
		mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, ind );

		// Create a model for the mesh.
		var model = Model.Builder
			.AddMesh( mesh )
			.AddCollisionMesh( vertices.Select( v => v.position ).ToArray(), ind )
			.Create();

		chunkEntity.Model = model;
		chunkEntity.Tags.Set( "voxelChunk", true );
		chunkEntity.Position = Position + new Vector3( chunk.x * chunk.Width, chunk.y * chunk.Depth, chunk.z * chunk.Height ) * VoxelScale + VoxelScale / 2f;
		chunkEntity.SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	public VoxelData? GetClosestVoxel( Vector3 position, ushort x = Chunk.DEFAULT_WIDTH, ushort y = Chunk.DEFAULT_DEPTH, ushort z = Chunk.DEFAULT_HEIGHT )
	{
		var relative = position - Position;
		var chunkPosition = new Vector3( relative.x / (x * VoxelScale), relative.y / (y * VoxelScale), relative.z / (z * VoxelScale) );
		if ( chunkPosition.x < 0 || chunkPosition.y < 0 || chunkPosition.z < 0 )
			return null;

		var w = Chunks.GetLength( 0 );
		var d = Chunks.GetLength( 1 );
		var h = Chunks.GetLength( 2 );

		var voxelIndex = (
			x: (short)(chunkPosition.x * x).FloorToInt(), 
			y: (short)(chunkPosition.y * y).FloorToInt(), 
			z: (short)(chunkPosition.z * z).FloorToInt()
		);
		var chunkIndex = (
			x: voxelIndex.x / x,
			y: voxelIndex.y / y,
			z: voxelIndex.z / z
		);

		if ( chunkIndex.x < 0 || chunkIndex.y < 0 || chunkIndex.z < 0
		  || chunkIndex.x >= w || chunkIndex.y >= d || chunkIndex.z >= h ) return null;

		var chunk = Chunks[chunkIndex.x, chunkIndex.y, chunkIndex.z];
		var voxel = (
			x: (ushort)(voxelIndex.x - chunkIndex.x * x),
			y: (ushort)(voxelIndex.y - chunkIndex.y * y),
			z: (ushort)(voxelIndex.z - chunkIndex.z * z)
		);

		return new VoxelData
		{
			Chunk = chunk,
			Voxel = chunk?.GetVoxel( voxel.x, voxel.y, voxel.z ),
			x = voxel.x,
			y = voxel.y,
			z = voxel.z
		};
	}

	[GameEvent.Client.Frame]
	private void tick()
	{
		if ( Game.LocalPawn is not Pawn pawn )
			return;

		var ray = new Ray( pawn.Position, pawn.ViewAngles.Forward );
		var tr = Trace.Ray( ray, 10000f )
			.StaticOnly()	
			.IncludeClientside()
			.Run();

		DebugOverlay.TraceResult( tr );

		var voxelData = ent?.GetClosestVoxel( tr.EndPosition - tr.Normal * VoxelScale / 2f );
		if ( voxelData?.Voxel != null )
		{
			var data = voxelData.Value;
			if ( Input.Down( "attack2" ) )
				data.Chunk.TrySetVoxel( data.x, data.y, data.z, null );
			else if ( Input.Down( "attack1" ) )
				data.Chunk.TrySetVoxel( data.x, data.y, data.z, new Voxel( Color32.Black ) );
		}
	}

	static VoxelEntity ent;

	[ConCmd.Client( "testvox" )]
	public static void Test()
	{
		if ( ent != null )
		{
			foreach ( var child in ent.Entities.Values )
				child.Delete();

			ent.Delete();
		}

		ent = new VoxelEntity( 0 );
	}
}
