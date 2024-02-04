namespace Deathcard;

/*
if ( voxel != null )
{
	var col = voxel.Value.Color.ToColor();
	var pos = parent.Position 
		+ position * parent.VoxelScale 
		+ (Vector3)data.Chunk.Position * parent.ChunkSize * parent.VoxelScale
		+ parent.VoxelScale / 2f;
	var particle = Particles.Create( "particles/voxel_break.vpcf", pos );
	particle.SetPosition( 2, col );
}
*/

public partial class VoxelWorld : Component, Component.ExecuteInEditor
{
	public static bool ChunkGizmo { get; set; } = true;

	[Property] public string Path { get; set; }
	[Property] public TextureAtlas Atlas { get; set; } = TextureAtlas.Get( "resources/textures/default.atlas" );
	[Property] public Vector3 VoxelScale { get; set; } = Utility.Scale;
	[Property, Hide] public byte[] Data { get; set; }

	public static IReadOnlyList<VoxelWorld> All => all;
	private static List<VoxelWorld> all = new();

	public Dictionary<Vector3S, Chunk> Chunks { get; private set; }

	private Dictionary<Chunk, VoxelChunk> objects = new();

	private Material material = Material.FromShader( "shaders/voxel.shader" );

	public void AssignAttributes( RenderAttributes attributes )
	{
		attributes.Set( "VoxelScale", VoxelScale );
		attributes.Set( "Albedo", Atlas.Albedo );
		attributes.Set( "RAE", Atlas.RAE );
		attributes.Set( "TextureSize", Atlas.TextureSize );
		attributes.Set( "AtlasSize", Atlas.Size );
	}

	protected override void OnAwake()
	{
		if ( all.Contains( this ) )
			return;
		
		if ( GameManager.IsPlaying )
			all.Add( this );

		Atlas.Build();
		Reset();
	}

	protected override void OnDestroy()
	{
		all.Remove( this );

		foreach ( var (_, child) in objects )
			child.GameObject?.Destroy();

		Chunks = null;
	}

	public VoxelChunk GetVoxelChunk( Chunk chunk )
	{
		VoxelChunk vxChunk;
		if ( !objects.TryGetValue( chunk, out vxChunk ) )
		{
			var obj = Scene.CreateObject();
			obj.Parent = GameObject;
			obj.Name = $"Chunk {chunk.Position}";
			obj.Transform.LocalPosition = (Vector3)chunk.Position * VoxelScale * Chunk.Size + VoxelScale / 2f;
			
			objects.Add( chunk, vxChunk = obj.Components.GetOrCreate<VoxelChunk>() );
		}

		return vxChunk;
	}

	/// <summary>
	/// Generates a chunk.
	/// </summary>
	/// <param name="chunk"></param>
	/// <param name="withPhysics"></param>
	public async Task GenerateChunk( Chunk chunk, bool withPhysics = true )
	{
		// Get our chunk's entity.
		if ( chunk == null )
			return;

		var vxChunk = GameManager.IsPlaying ? GetVoxelChunk( chunk ) : null;

		// Let's create a mesh.
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
		var offset = 0;

		var tested = new bool[Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT];
		var buffer = new CollisionBuffer();
		buffer.Init( true );

		chunk.Empty = true;
		for ( byte x = 0; x < Chunk.DEFAULT_WIDTH; x++ )
		for ( byte y = 0; y < Chunk.DEFAULT_DEPTH; y++ )
		for ( byte z = 0; z < Chunk.DEFAULT_HEIGHT; z++ )
		{
			var voxel = chunk.GetVoxel( x, y, z );
			if ( voxel == null )				
				continue;

			chunk.Empty = false;

			var position = new Vector3B( x, y, z );

			// Let's start checking for collisions.
			if ( !tested[x, y, z] && GameManager.IsPlaying )
			{
				tested[x, y, z] = true;

				var start = (x: x, y: y, z: z);
				var size = (x: 1, y: 1, z: 1);
				var canSpread = (x: true, y: true, z: true);

				// Calculate how much we can fill.
				while ( canSpread.x || canSpread.y || canSpread.z )
				{
					canSpread.x = trySpreadX( chunk, canSpread.x, ref tested, start, ref size );
					canSpread.y = trySpreadY( chunk, canSpread.y, ref tested, start, ref size );
					canSpread.z = trySpreadZ( chunk, canSpread.z, ref tested, start, ref size );
				}

				var scale = new Vector3( size.x, size.y, size.z ) * VoxelScale;
				var pos = new Vector3( start.x, start.y, start.z ) * VoxelScale
					+ scale / 2f
					- VoxelScale / 2f;

				buffer.AddCube( pos, scale, Rotation.Identity );
			}

			// Build vertices.
			voxel.Build( this, chunk, position, ref vertices, ref indices, ref offset, buffer );
		}

		// Check if we actually end up with vertices.
		if ( vertices.Count > 0 && !chunk.Empty )
		{
			mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
			mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

			var builder = Model.Builder
				.AddMesh( mesh );

			if ( vxChunk == null ) // We are in editor.
			{
				gizmoCache.Add( chunk.Position, builder.Create() );
				return;
			}

			if ( withPhysics )
				builder = builder.AddCollisionMesh( buffer.Vertex.ToArray(), buffer.Index.ToArray() );

			vxChunk.Rebuild( builder.Create(), !withPhysics );

			return;
		}

		// Let's remove our empty chunk!
		objects.Remove( chunk );
		Chunks.Remove( chunk.Position );

		vxChunk?.GameObject?.Destroy();
	}
	
	public override async void Reset()
	{
		gizmoCache.Clear();
		objects.Clear();

		foreach ( var child in GameObject.Children )
			child.Destroy();

		var t = DateTime.Now;

		Chunks = await BaseImporter.Get<VoxImporter>()
			.BuildAsync( Path );

		foreach ( var (_, chunk) in Chunks )
			await GenerateChunk( chunk );

		Log.Info( $"Importing and generating VoxelWorld mesh took {(DateTime.Now - t).Milliseconds}ms." );
	}

	#region DEBUG
	private readonly Dictionary<Vector3S, Model> gizmoCache = new();

	protected override void DrawGizmos()
	{
		// Display all chunks.
		Gizmo.Draw.Color = Color.Yellow;
		Gizmo.Draw.LineThickness = 0.1f;

		if ( !GameManager.IsPlaying )
			foreach ( var (chunk, model) in gizmoCache )
			{
				var pos = (Vector3)chunk * VoxelScale * Chunk.Size;
				var bounds = new BBox( pos, pos + (Vector3)Chunk.Size * VoxelScale );

				var obj = Gizmo.Draw.Model( model, new Transform( pos + VoxelScale / 2 ) );
				AssignAttributes( obj.Attributes );

				Gizmo.Hitbox.BBox( bounds ); // Gizmo.Model doesn't work sadly...
				
				if ( ChunkGizmo )
					Gizmo.Draw.LineBBox( bounds );
			}
		else if ( ChunkGizmo )
			foreach ( var (_, chunk) in objects )
				Gizmo.Draw.LineBBox( new BBox( chunk.Transform.Position, chunk.Transform.Position + (Vector3)Chunk.Size * VoxelScale ) );

		if ( !Gizmo.IsSelected )
			return;

		// Focus on hovered VoxelWorld.
		var tr = Trace( Gizmo.CurrentRay, 50000f );
		if ( !tr.Hit )
			return;

		// Debug
		var center = (Vector3)tr.GlobalPosition * VoxelScale + VoxelScale / 2f;
		var bbox = new BBox( center - VoxelScale / 2f, center + VoxelScale / 2f );
		/*var surface = center + tr.Normal * VoxelScale / 2f;

		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineThickness = 5;
		Gizmo.Draw.Line( surface, surface + tr.Normal * 50f );*/

		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.ScreenText( $"{(tr.Voxel?.GetType().Name ?? "unknown")}", 20, "Consolas", 18, TextFlag.LeftTop );
		Gizmo.Draw.ScreenText( $"XYZ: {tr.GlobalPosition}", 20 + Vector2.Up * 20, "Consolas", 18, TextFlag.LeftTop );
		Gizmo.Draw.ScreenText( $"Chunk: {tr.Chunk?.Position}", 20 + Vector2.Up * 40, "Consolas", 18, TextFlag.LeftTop );

		Gizmo.Draw.Color = Color.Black;
		Gizmo.Draw.LineThickness = 1;
		Gizmo.Draw.LineBBox( bbox );

		Gizmo.Draw.Color = Color.Black.WithAlpha( 0.5f );
		Gizmo.Draw.SolidBox( bbox );
	}
#endregion
}
