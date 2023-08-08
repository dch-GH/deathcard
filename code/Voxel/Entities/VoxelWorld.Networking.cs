namespace DeathCard;

partial class VoxelWorld
{
	#region Fields
	private static VoxelWorld instance;
	private Dictionary<Vector3I, Change> changes = new();
	#endregion

	/// <summary>
	/// Initializes a VoxelWorld, should be called on server.
	/// </summary>
	/// <returns></returns>
	public static async Task<VoxelWorld> Create( string map = null, Vector3I? chunkSize = null )
	{
		// Create a VoxelWorld and load a map for it.
		var world = new VoxelWorld()
		{
			ChunkSize = chunkSize ?? new( Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT ),
		};

		var chunks = await Importer.VoxImporter.Load( map, world.ChunkSize.x, world.ChunkSize.y, world.ChunkSize.z );
		world.Size = new Vector3I( chunks.GetLength( 0 ), chunks.GetLength( 0 ), chunks.GetLength( 0 ) );
		world.Chunks = chunks;

		// Initial chunk generation.
		foreach ( var chunk in world.Chunks )
			world.GenerateChunk( chunk );

		// Send the map to all clients.
		world.LoadAsMap( To.Everyone, world.NetworkIdent, map ?? string.Empty );

		return world;
	}

	[ClientRpc]
	public async void LoadAsMap( int ident, string map )
	{
		// Make sure we have a valid VoxelWorld.
		if ( Entity.FindByIndex( ident ) is not VoxelWorld entity )
		{
			Log.Error( $"Failed to find VoxelWorld[{ident}]." );
			return;
		}

		// Create same map as server.
		var chunks = await Importer.VoxImporter.Load( map, entity.ChunkSize.x, entity.ChunkSize.y, entity.ChunkSize.z );
		entity.Chunks = chunks;

		// TODO: Apply server's changes here.
		// Maybe multithread?

		// Initial chunk generation.
		foreach ( var chunk in entity.Chunks )
			entity.GenerateChunk( chunk );
	}

	[ConCmd.Server( "loadmap" )]
	public static async void LoadMap( string map = "vox/monument.vox" )
	{
		instance?.Delete();
		instance = await VoxelWorld.Create( "vox/monument.vox" );
	}

	#region Networking
	public enum VoxelState
	{
		Invalid,
		Valid
	}

	public struct Change
	{
		public VoxelState State;
		public Voxel? Voxel;
		public Voxel? Previous;
	}

	private bool RegisterChange( Chunk chunk, Vector3I pos, Voxel? voxel )
	{
		// Check if we are within chunk bounds.
		if ( pos.x > ChunkSize.x - 1 || pos.y > ChunkSize.y - 1 || pos.z > ChunkSize.z - 1
		  || pos.x < 0 || pos.y < 0 || pos.z < 0 ) return false;

		var previous = chunk.GetVoxel( pos.x, pos.y, pos.z );
		if ( previous == null && voxel == null )
			return false;

		var position = GetGlobalSpace( pos.x, pos.y, pos.z, chunk );
		if ( changes.TryGetValue( position, out var change ) )
		{
			change.Previous = previous;
			change.Voxel = voxel;
			change.State = voxel == null
				? VoxelState.Invalid
				: VoxelState.Valid;

			return true;
		}

		changes.Add( position, new Change()
		{
			Voxel = voxel,
			Previous = previous,
			State = voxel == null
				? VoxelState.Invalid
				: VoxelState.Valid
		} );

		return true;
	}

	/// <summary>
	/// Sets voxel relative to a chunk, coordinates cannot be greater or equal to ChunkSize or less than 0.
	/// </summary>
	/// <param name="chunk"></param>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="voxel"></param>
	public void SetVoxel( Chunk chunk, ushort x, ushort y, ushort z, Voxel? voxel )
	{
		if ( !RegisterChange( chunk, new( x, y, z ), voxel ) )
			return;

		chunk.SetVoxel( x, y, z, voxel );
	}

	/// <summary>
	/// Sets voxel by offset, relative to the chunk parameter or Chunks[0, 0, 0].
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="voxel"></param>
	/// <param name="relative"></param>
	public void SetVoxel( int x, int y, int z, Voxel? voxel, Chunk relative = null )
	{
		// Convert to local space.
		var pos = GetLocalSpace( x, y, z, out var chunk, relative );
		if ( chunk == null )
			return;

		// Set voxel.
		SetVoxel( chunk, pos.x, pos.y, pos.z, voxel );
	}

	private void Update()
	{
		// Initialize our MemoryStream.
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter( stream );

		// Let's keep track of all the chunks that we need to update.
		var chunks = new Collection<Chunk>();

		// Start writing data.
		var count = changes.Count;
		writer.Write( count );

		foreach ( var (position, change) in changes )
		{
			writer.Write( (byte)change.State );
			writer.Write( position.x );
			writer.Write( position.y );
			writer.Write( position.z );

			var pos = GetLocalSpace( position.x, position.y, position.z, out var chunk );
			var neighbors = chunk.GetNeighbors( pos.x, pos.y, pos.z );
			foreach ( var neighbor in neighbors )
				if ( neighbor != null && !chunks.Contains( neighbor ) )
					chunks.Add( neighbor );

			if ( change.State == VoxelState.Invalid )
				continue;

			var voxel = change.Voxel.Value;
			writer.Write( voxel.R );
			writer.Write( voxel.G );
			writer.Write( voxel.B );
		}

		// Rebuild all affected chunks.
		foreach ( var chunk in chunks )
			GenerateChunk( chunk );

		// Send update to all clients.
		SendUpdate( To.Everyone, stream.ToArray() );
	}

	/// <summary>
	/// Sends an update of voxel changes to a client.
	/// </summary>
	[ClientRpc]
	public void SendUpdate( byte[] data )
	{
		// Initialize our MemoryStream.
		using var stream = new MemoryStream( data );
		using var reader = new BinaryReader( stream );

		// Let's keep track of all the chunks that we need to update.
		var chunks = new Collection<Chunk>();

		// Start reading data.
		var count = reader.ReadInt32();
		for ( int i = 0; i < count; i++ )
		{
			var state = (VoxelState)reader.ReadByte();
			var position = new Vector3I( reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16() );

			var voxel = state == VoxelState.Invalid
				? (Voxel?)null
				: new Voxel( new Color32( reader.ReadByte(), reader.ReadByte(), reader.ReadByte() ) );

			var pos = GetLocalSpace( position.x, position.y, position.z, out var chunk );
			chunk.SetVoxel( pos.x, pos.y, pos.z, voxel );

			var neighbors = chunk.GetNeighbors( pos.x, pos.y, pos.z );
			foreach ( var neighbor in neighbors )
				if ( neighbor != null && !chunks.Contains( neighbor ) )
					chunks.Add( neighbor );

			// Check if our Voxel matches on client & server.
			if ( changes.TryGetValue( position, out var change ) && change.State == state && change.Voxel?.Color == voxel?.Color )
				changes.Remove( position );
		}

		// TODO: Go through all client changes, revert.
		// This doesn't work properly yet.
		/*foreach ( var (pos, change) in changes )
		{
			var position = GetLocalSpace( pos.x, pos.y, pos.z, out var chunk );
			if ( chunk == null )
				continue;
			Log.Error( $"Voxel mismatch at {pos}!" );
			chunk.SetVoxel( position.x, position.y, position.z, change.Previous );
		}*/

		changes.Clear();

		// Update changed chunks.
		foreach ( var chunk in chunks )
			GenerateChunk( chunk );
	}

	[GameEvent.Tick]
	private void Tick()
	{
		// Let's apply existing changes on server and network them.
		if ( Game.IsServer && changes.Count > 0 )
		{
			Update();
			changes.Clear();
		}
	}
	#endregion
}
