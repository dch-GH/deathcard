namespace DeathCard;

public static partial class Utility
{
	#region Ambient Occlusion
	private static IReadOnlyDictionary<int, List<(int x, int y, int z)[]>> aoNeighbors = new Dictionary<int, List<(int x, int y, int z)[]>>()
	{
		// +z
		[0] = new() 
		{
			new (int, int, int)[3] { (0, 1, -1), (-1, 1, 0), (-1, 1, -1) },
			new (int, int, int)[3] { (0, 1, -1), (1, 1, 0), (1, 1, -1) },
			new (int, int, int)[3] { (0, 1, 1), (-1, 1, 0), (-1, 1, 1) },
			new (int, int, int)[3] { (0, 1, 1), (1, 1, 0), (1, 1, 1) }
		},

		// -z
		[1] = new()
		{
			new (int, int, int)[3] { (0, -1, -1), (1, -1, 0), (1, -1, -1) },
			new (int, int, int)[3] { (0, -1, -1), (-1, -1, 0), (-1, -1, -1) },
			new (int, int, int)[3] { (0, -1, 1), (1, -1, 0), (1, -1, 1) },
			new (int, int, int)[3] { (0, -1, 1), (-1, -1, 0), (-1, -1, 1) }
		},

		// -x
		[2] = new()
		{
			new (int, int, int)[3] { (-1, 0, -1), (-1, 1, 0), (-1, 1, -1) },
			new (int, int, int)[3] { (-1, 0, 1), (-1, 1, 0), (-1, 1, 1) },
			new (int, int, int)[3] { (-1, 0, -1), (-1, -1, 0), (-1, -1, -1) },
			new (int, int, int)[3] { (-1, 0, 1), (-1, -1, 0), (-1, -1, 1) }
		},

		// +y
		[3] = new()
		{
			new (int, int, int)[3] { (-1, 0, 1), (0, 1, 1), (-1, 1, 1) },
			new (int, int, int)[3] { (1, 0, 1), (0, 1, 1), (1, 1, 1) },
			new (int, int, int)[3] { (-1, 0, 1), (0, -1, 1), (-1, -1, 1) },
			new (int, int, int)[3] { (1, 0, 1), (0, -1, 1), (1, -1, 1) }
		},

		// +x
		[4] = new()
		{
			new (int, int, int)[3] { (1, 0, 1), (1, 1, 0), (1, 1, 1) },
			new (int, int, int)[3] { (1, 0, -1), (1, 1, 0), (1, 1, -1) },
			new (int, int, int)[3] { (1, 0, 1), (1, -1, 0), (1, -1, 1) },
			new (int, int, int)[3] { (1, 0, -1), (1, -1, 0), (1, -1, -1) }
		},

		// -y
		[5] = new()
		{
			new (int, int, int)[3] { (1, 0, -1), (0, 1, -1), (1, 1, -1) },
			new (int, int, int)[3] { (-1, 0, -1), (0, 1, -1), (-1, 1, -1) },
			new (int, int, int)[3] { (1, 0, -1), (0, -1, -1), (1, -1, -1) },
			new (int, int, int)[3] { (-1, 0, -1), (0, -1, -1), (-1, -1, -1) }
		}
	};

	private static int occlusion( Chunk chunk, Vector3B pos, int x, int y, int z )
	{
		if ( chunk.GetDataByOffset( pos.x + x, pos.y + z, pos.z + y ).Voxel != null )
			return 1;

		return 0;
	}

	/// <summary>
	/// Builds AO values for a voxel based on the parameters.
	/// </summary>
	/// <param name="chunk"></param>
	/// <param name="pos"></param>
	/// <param name="face"></param>
	/// <param name="vertex"></param>
	/// <returns></returns>
	public static byte BuildAO( Chunk chunk, Vector3B pos, int face, int vertex )
	{
		if ( !aoNeighbors.TryGetValue( face, out var values ) )
			return 0;

		var table = values[
			vertex switch
			{
				0 => 0,
				1 => 2,
				2 => 3,
				3 => 1,
				_ => 0
			}];

		var ao = occlusion( chunk, pos, table[0].x, table[0].y, table[0].z )
			+ occlusion( chunk, pos, table[1].x, table[1].y, table[1].z )
			+ occlusion( chunk, pos, table[2].x, table[2].y, table[2].z );

		return (byte)ao;
	}
	#endregion

	#region Fields
	public const float Scale = 1f / 0.0254f;
	public const int Faces = 6;

	public static readonly int[]
		FaceIndices = new int[4 * Faces]
	{
		0, 1, 2, 3,
		7, 6, 5, 4,
		0, 4, 5, 1,
		1, 5, 6, 2,
		2, 6, 7, 3,
		3, 7, 4, 0,
	};

	public static readonly (short x, short y, short z)[]
		Neighbors = new (short, short, short)[Faces]
	{
		(0, 0, 1),
		(0, 0, -1),
		(-1, 0, 0),
		(0, 1, 0),
		(1, 0, 0),
		(0, -1, 0),
	};
	#endregion

	private static Dictionary<ModelEntity, string> models = new();

	/// <summary>
	/// Sets the voxel model of an entity if there is an existing VoxelResource model for it.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="path"></param>
	public static void SetVoxelModel( this ModelEntity entity, string path )
	{
		if ( Game.IsServer )
		{
			SetVoxelModelRPC( To.Everyone, entity.NetworkIdent, path );

			if ( !models.ContainsKey( entity) )
				models.Add( entity, path );
			else 
				models[entity] = path;

			return;
		}

		SetModel( entity, path );
	}

	[ClientRpc]
	public static void SetVoxelModelRPC( int ident, string path )
	{
		if ( Entity.FindByIndex( ident ) is not ModelEntity entity )
			return;

		SetModel( entity, path );
	}

	private static async void SetModel( ModelEntity entity, string path )
	{
		var resource = VoxelResource.Get( path );
		if ( resource == null )
		{
			Log.Error( $"VoxelResource doesn't exist at '{path}'." );
			return;
		}

		if ( !resource.Loaded )
			resource.Model = await VoxelBuilder.FromFile( resource.Path )
				.WithScale( resource.Scale )
				.WithDepth( resource.HasDepth
					? resource.Depth
					: null )
				.WithCenter( resource.Center )
				.FinishAsync();

		resource.Loaded = true;
		entity.Model = resource.Model;
	}

	[GameEvent.Server.ClientJoined]
	private static void ClientJoined( ClientJoinedEvent @event )
	{
		if ( Game.IsClient )
			return;

		// Send all current ModelEntities that use VoxelModels.
		foreach ( var (entity, path) in models )
		{
			if ( entity == null )
				continue;
			
			SetVoxelModelRPC( To.Single( @event.Client ), entity.NetworkIdent, path );
		}
	}
}
