﻿namespace DeathCard;

public static class Utility
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

	private static float occlusion( Chunk chunk, Vector3I pos, int x, int y, int z )
	{
		if ( chunk.GetVoxelByOffset( pos.x + x, pos.y + z, pos.z + y ) != null )
			return 0.75f;

		return 1f;
	}

	/// <summary>
	/// Builds AO values for a voxel based on the parameters.
	/// </summary>
	/// <param name="chunk"></param>
	/// <param name="pos"></param>
	/// <param name="face"></param>
	/// <param name="vertex"></param>
	/// <returns></returns>
	public static float BuildAO( Chunk chunk, Vector3I pos, int face, int vertex )
	{
		if ( !aoNeighbors.TryGetValue( face, out var values ) )
			return 1f;

		var table = values[
			vertex switch
			{
				0 => 0,
				1 => 2,
				2 => 3,
				3 => 1,
				_ => 0
			}];
		return occlusion( chunk, pos, table[0].x, table[0].y, table[0].z )
			* occlusion( chunk, pos, table[1].x, table[1].y, table[1].z )
			* occlusion( chunk, pos, table[2].x, table[2].y, table[2].z );
	}
	#endregion

	#region Fields
	public const float Scale = 1f / 0.0254f;
	public const int Faces = 6;

	public static readonly Vector3[]
		Positions = new Vector3[8]
	{
		new Vector3( -0.5f, -0.5f, 0.5f ),
		new Vector3( -0.5f, 0.5f, 0.5f ),
		new Vector3( 0.5f, 0.5f, 0.5f ),
		new Vector3( 0.5f, -0.5f, 0.5f ),
		new Vector3( -0.5f, -0.5f, -0.5f ),
		new Vector3( -0.5f, 0.5f, -0.5f ),
		new Vector3( 0.5f, 0.5f, -0.5f ),
		new Vector3( 0.5f, -0.5f, -0.5f )
	};

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

	public static readonly float[]
		FaceMultiply = new float[Faces]
	{
		1f, 1f,
		0.85f, 0.7f,
		0.85f, 0.7f
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

	/// <summary>
	/// Sets the voxel model of an entity using the VoxelModel formats.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="file"></param>
	/// <param name="scale"></param>
	/// <param name="depth"></param>
	/// <param name="occlusion"></param>
	public static async Task<Model> SetVoxelModel( this ModelEntity entity, string file, float scale = Utility.Scale, float? depth = null, bool occlusion = true )
	{
		var mdl = await VoxelModel.FromFile( file )
			.WithScale( scale )
			.WithDepth( depth )
			.BuildAsync( occlusion );

		return entity.Model = mdl;
	}

}