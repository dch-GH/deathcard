namespace Deathcard;

public static partial class Utility
{
	#region Ambient Occlusion
	private static readonly IReadOnlyDictionary<int, List<(sbyte x, sbyte y, sbyte z)[]>> aoNeighbors = new Dictionary<int, List<(sbyte x, sbyte y, sbyte z)[]>>()
	{
		// +z
		[0] = new() 
		{
			new (sbyte, sbyte, sbyte)[3] { (0, 1, -1), (-1, 1, 0), (-1, 1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (0, 1, -1), (1, 1, 0), (1, 1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (0, 1, 1), (-1, 1, 0), (-1, 1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (0, 1, 1), (1, 1, 0), (1, 1, 1) }
		},

		// -z
		[1] = new()
		{
			new (sbyte, sbyte, sbyte)[3] { (0, -1, -1), (1, -1, 0), (1, -1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (0, -1, -1), (-1, -1, 0), (-1, -1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (0, -1, 1), (1, -1, 0), (1, -1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (0, -1, 1), (-1, -1, 0), (-1, -1, 1) }
		},

		// -x
		[2] = new()
		{
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, -1), (-1, 1, 0), (-1, 1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, 1), (-1, 1, 0), (-1, 1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, -1), (-1, -1, 0), (-1, -1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, 1), (-1, -1, 0), (-1, -1, 1) }
		},

		// +y
		[3] = new()
		{
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, 1), (0, 1, 1), (-1, 1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (1, 0, 1), (0, 1, 1), (1, 1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, 1), (0, -1, 1), (-1, -1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (1, 0, 1), (0, -1, 1), (1, -1, 1) }
		},

		// +x
		[4] = new()
		{
			new (sbyte, sbyte, sbyte)[3] { (1, 0, 1), (1, 1, 0), (1, 1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (1, 0, -1), (1, 1, 0), (1, 1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (1, 0, 1), (1, -1, 0), (1, -1, 1) },
			new (sbyte, sbyte, sbyte)[3] { (1, 0, -1), (1, -1, 0), (1, -1, -1) }
		},

		// -y
		[5] = new()
		{
			new (sbyte, sbyte, sbyte)[3] { (1, 0, -1), (0, 1, -1), (1, 1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, -1), (0, 1, -1), (-1, 1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (1, 0, -1), (0, -1, -1), (1, -1, -1) },
			new (sbyte, sbyte, sbyte)[3] { (-1, 0, -1), (0, -1, -1), (-1, -1, -1) }
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

	public static readonly byte[]
		FaceIndices = new byte[4 * Faces]
	{
		0, 1, 2, 3,
		7, 6, 5, 4,
		0, 4, 5, 1,
		1, 5, 6, 2,
		2, 6, 7, 3,
		3, 7, 4, 0,
	};

	public static readonly (sbyte x, sbyte y, sbyte z)[]
		Directions = new (sbyte, sbyte, sbyte)[Faces]
	{
		(0, 0, 1),
		(0, 0, -1),
		(-1, 0, 0),
		(0, 1, 0),
		(1, 0, 0),
		(0, -1, 0),
	};
	#endregion
}
