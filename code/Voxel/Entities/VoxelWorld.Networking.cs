namespace DeathCard;

partial class VoxelWorld
{
	/// <summary>
	/// The current map file used for this VoxelWorld.
	/// </summary>
	public string Map { get; private set; }

	/// <summary>
	/// The total amount of payloads we need to finish.
	/// </summary>
	public int Payloads { get; private set; } = 0;

	/// <summary>
	/// The amount of payloads we have loaded.
	/// </summary>
	public int Loaded { get; private set; } = 0;

	/// <summary>
	/// Are we finished loading the map?
	/// </summary>
	public bool Finished { get; private set; } = false;

	#region Fields
	private Dictionary<Vector3I, Change?> changes = new();
	private Dictionary<Vector3I, Voxel?> totalChanges = new();
	private Collection<IClient> clients = new();
	#endregion

	/// <summary>
	/// Initializes a VoxelWorld, should be called on server.
	/// </summary>
	/// <returns></returns>
	public static async Task<VoxelWorld> Create( string map, Vector3I? chunkSize = null )
	{
		Game.AssertServer();

		// Delete earlier instances.
		foreach ( var old in Entity.All.OfType<VoxelWorld>() )
			old.Delete();

		// Create a VoxelWorld and load a map for it.
		var size = chunkSize ?? new( Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT );
		var world = new VoxelWorld()
		{
			ChunkSize = size,
			Map = map
		};

		world.Chunks = await Importer.VoxImporter.Load( map, size.x, size.y, size.z );
		world.Size = new Vector3I( world.Chunks.GetLength( 0 ), world.Chunks.GetLength( 1 ), world.Chunks.GetLength( 2 ) );

		// Initial chunk generation.
		foreach ( var chunk in world.Chunks )
			world.GenerateChunk( chunk );

		// Send the map to all clients.
		world.LoadAsMap( To.Everyone, map ?? string.Empty );
		world.Finished = true;

		return world;
	}

	[ConCmd.Server( "load_map" )]
	public static async void LoadMap( string map = "vox/maps/monument.vox" )
		=> await VoxelWorld.Create( map );
	
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
		public Voxel? Revert;
	}

	private bool RegisterChange( Chunk chunk, Vector3I pos, Voxel? voxel )
	{
		// Check if we are within chunk bounds.
		if ( pos.x > ChunkSize.x - 1 || pos.y > ChunkSize.y - 1 || pos.z > ChunkSize.z - 1
		  || pos.x < 0 || pos.y < 0 || pos.z < 0 || chunk == null ) return false;

		var previous = chunk.GetVoxel( pos.x, pos.y, pos.z );
		if ( previous == null && voxel == null )
			return false;

		var position = GetGlobalSpace( pos.x, pos.y, pos.z, chunk );

		// Keep track of all changes.
		if ( totalChanges.ContainsKey( position ) )
			totalChanges[position] = voxel;
		else
			totalChanges.Add( position, voxel );

		// Keep track of changes in 1 tick.
		if ( changes.TryGetValue( position, out var change ) )
		{
			changes[position] = new Change()
			{
				Revert = change?.Voxel,
				Voxel = voxel,
				State = voxel == null
					? VoxelState.Invalid
					: VoxelState.Valid
			};
			
			return true;
		}

		changes.Add( position, new Change()
		{
			Voxel = voxel,
			Revert = previous,
			State = voxel == null
				? VoxelState.Invalid
				: VoxelState.Valid
		} );

		return true;
	}

	/// <summary>
	/// Creates a chunk at some position if there isn't one yet.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="local"></param>
	/// <param name="relative"></param>
	/// <returns>The newly created chunk or null.</returns>
	private Chunk CreateChunk( int x, int y, int z, Vector3I? local = null, Chunk relative = null )
	{
		// Calculate new chunk position.
		var position = (
			x: ((relative?.Position.x ?? 0) + (float)x / ChunkSize.x - (float)(local?.x ?? 0) / ChunkSize.x).CeilToInt(),
			y: ((relative?.Position.y ?? 0) + (float)y / ChunkSize.y - (float)(local?.y ?? 0) / ChunkSize.y).CeilToInt(),
			z: ((relative?.Position.z ?? 0) + (float)z / ChunkSize.z - (float)(local?.z ?? 0) / ChunkSize.z).CeilToInt()
		);
		
		// For now we don't want to extend.
		/*if ( position.x >= Size.x || position.y >= Size.y || position.z >= Size.z )
		{
			Extend( Math.Max( position.x - Size.x + 1, 0 ),
					Math.Max( position.y - Size.y + 1, 0 ),
					Math.Max( position.z - Size.z + 1, 0 ) );
			//return null;
		}*/

		// Check if we have a chunk already or are out of bounds.
		if ( position.x >= Size.x || position.y >= Size.y || position.z >= Size.z
		  || position.x < 0 || position.y < 0 || position.z < 0
		  || Chunks[position.x, position.y, position.z] != null ) return null;

		// Create new chunk.
		var chunk = new Chunk(
			(ushort)position.x, (ushort)position.y, (ushort)position.z,
			ChunkSize.x, ChunkSize.y, ChunkSize.z,
			Chunks );

		Chunks[position.x, position.y, position.z] = chunk;

		return chunk;
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

		// Create new chunk if needed.
		if ( chunk == null && voxel != null )
			chunk = CreateChunk( x, y, z, pos, relative );

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

		// TODO: Chunk sent data, send data in smaller payloads.
		// Start writing data.
		var count = changes.Count;
		writer.Write( count );

		foreach ( var (position, nullChange) in changes )
		{
			var change = nullChange.Value; // This shouldn't fail ever or something...

			writer.Write( (byte)change.State );
			writer.Write( position.x );
			writer.Write( position.y );
			writer.Write( position.z );

			var pos = GetLocalSpace( position.x, position.y, position.z, out var chunk );
			if ( chunk != null && !chunks.Contains( chunk ) )
				chunks.Add( chunk );

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
			chunk?.SetVoxel( pos.x, pos.y, pos.z, voxel );

			// Check if our Voxel matches on client & server.
			var condition = changes.TryGetValue( position, out var change )
				&& change?.State == state
				&& change?.Voxel?.Color == voxel?.Color;

			if ( condition || (voxel == null && change?.Voxel == null) )
				changes.Remove( position );
			else if ( change != null )
				changes[position] = change.Value with
				{
					Revert = voxel,
				};

			var neighbors = chunk?.GetNeighbors( pos.x, pos.y, pos.z );
			if ( neighbors == null )
				continue;

			foreach ( var neighbor in neighbors )
				if ( neighbor != null && !chunks.Contains( neighbor ) )
					chunks.Add( neighbor );
		}

		// Revert failed changes.
		// TODO: Implement this properly.
		foreach ( var (pos, change) in changes )
		{
			var position = GetLocalSpace( pos.x, pos.y, pos.z, out var chunk );
			if ( chunk == null )
				continue;

			chunk.SetVoxel( position.x, position.y, position.z, change?.Revert );
		}

		changes.Clear();

		// Update changed chunks.
		foreach ( var chunk in chunks )
			GenerateChunk( chunk );
	}

	[ClientRpc]
	public async void LoadAsMap( string map )
	{
		// Create same map as server.
		var chunks = await Importer.VoxImporter.Load( map, ChunkSize.x, ChunkSize.y, ChunkSize.z );
		Chunks = chunks;
		Map = map;

		// Request all changes.
		RequestChanges( NetworkIdent );
	}

	[ClientRpc]
	public void ApplyChanges( byte[] data )
	{
		// Start reading data in chunks.
		using var stream = new MemoryStream( data );
		using var reader = new BinaryReader( stream );

		Payloads = reader.ReadUInt16();
		Loaded = reader.ReadUInt16();

		// Just generate chunks if we don't have any changes.
		if ( Payloads == 0 && Loaded == 0 )
		{
			Finished = true;

			foreach ( var chunk in Chunks )
				GenerateChunk( chunk );

			return;
		}

		// Go through all of this payload's changes.
		var count = reader.ReadInt32();
		for ( int i = 0; i < count; i++ )
		{
			// Read a few variables from the MemoryStream.
			var position = new Vector3I( reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16() );
			var pos = GetLocalSpace( position.x, position.y, position.z, out var chunk );
			var state = (VoxelState)reader.ReadByte();
			var voxel = state == VoxelState.Valid
				? new Voxel( new Color32( reader.ReadByte(), reader.ReadByte(), reader.ReadByte() ) )
				: (Voxel?)null;

			// Create a new chunk if we need it.
			if ( chunk == null && voxel != null )
				chunk = CreateChunk( position.x, position.y, position.z, pos );

			// Skip newly assigned voxels.
			if ( totalChanges.ContainsKey( position ) )
				continue;

			chunk?.SetVoxel( pos.x, pos.y, pos.z, voxel );
		}

		// Check if we are finished and can load the chunks.
		if ( Loaded >= Payloads )
		{
			Finished = true;

			foreach ( var chunk in Chunks )
				GenerateChunk( chunk );
		}
	}

	[ConCmd.Server( "request_changes" )]
	public static async void RequestChanges( int ident )
	{
		// Check if we have map.
		if ( Entity.FindByIndex( ident ) is not VoxelWorld world )
			return;

		// Check if client already has map.
		var cl = ConsoleSystem.Caller;
		if ( world.clients.Contains( cl ) )
			return;

		world.clients.Add( cl );

		// Initialize our MemoryStream and name a few variables.
		var stream = new MemoryStream();
		var writer = new BinaryWriter( stream );

		var delay = 200;

		var maxPerChunk = 500;
		var payloads = (ushort)(world.totalChanges.Count / (float)maxPerChunk).CeilToInt();
		var count = 0;
		var sent = (ushort)0;

		// If we have no changes, let's just tell the client to build the map.
		if ( payloads == 0 )
		{
			writer.Write( payloads );
			writer.Write( sent );

			world.ApplyChanges( To.Single( cl ), stream.ToArray() );

			return;
		}

		Log.Info( $"{cl.Name} is requesting {payloads} payloads!" );

		// Let's assign this function so we can use it many times.
		void sendPayload()
		{
			sent++;

			using var _stream = new MemoryStream();
			using var _writer = new BinaryWriter( _stream );
			_writer.Write( payloads );
			_writer.Write( sent );
			_writer.Write( count );
			_writer.Write( stream.ToArray() );

			Log.Info( $"Sent payload {sent}/{payloads} to {cl.Name}!" );
			world.ApplyChanges( To.Single( cl ), _stream.ToArray() );

			// Clear current stream and make a new one.
			var buffer = stream.GetBuffer();
			Array.Clear( buffer, 0, buffer.Length );
			stream.Position = 0;
			stream.SetLength( 0 );
			stream.Capacity = 0;

			count = 0;
		}

		// Go through all changes.
		var changes = new Dictionary<Vector3I, Voxel?>( world.totalChanges );
		foreach ( var (position, voxel) in changes )
		{
			// Start new chunk and send current data to client..
			if ( count >= maxPerChunk )
			{
				sendPayload();
				await GameTask.Delay( delay );
			}

			// Append count.
			count++;

			// Write current voxel's information.
			writer.Write( position.x );
			writer.Write( position.y );
			writer.Write( position.z );

			var state = voxel == null
				? VoxelState.Invalid
				: VoxelState.Valid;
			writer.Write( (byte)state );

			if ( state != VoxelState.Valid )
				continue;

			writer.Write( voxel.Value.R );
			writer.Write( voxel.Value.G );
			writer.Write( voxel.Value.B );
		}

		await GameTask.Delay( delay );

		// Send last bit of data.
		if ( stream.Length > 0 )
			sendPayload();

		// Dispose unused shit.
		writer.Dispose();
		writer.Flush();

		stream.Dispose();
		stream.Flush();
	}

	[GameEvent.Server.ClientJoined]
	private void ClientJoined( ClientJoinedEvent @event )
	{
		if ( !@event.Client.IsValid )
			return;

		// Load map information first.
		LoadAsMap( To.Single( @event.Client ), Map );
	}

	[GameEvent.Tick]
	private void Tick()
	{
		// If we don't have any changes this tick, just skip it.
		if ( changes.Count <= 0 )
			return;
		
		// Check all changes and send them to clients.
		if ( Game.IsServer )
		{
			Update();
			changes.Clear();
		}

		// Check if we changed voxels on client and need to update.
		// If LastUpdated (from ClientRPC) > tickrate, rebuild chunks here.
		else
		{ 
		}
	}
	#endregion
}
