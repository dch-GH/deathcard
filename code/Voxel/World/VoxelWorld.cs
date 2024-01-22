﻿namespace Deathcard;

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

public class ChunkObject : IEquatable<ChunkObject>
{
	public Vector3S Position3D { get; set; }
	public VoxelWorld Parent { get; set; }

	public PhysicsBody Body { get; }
	public PhysicsShape Shape { get; set; }

	public Model Model
	{
		get => obj?.Model;
		set
		{
			obj ??= new( GameManager.ActiveScene.SceneWorld, value ) { Batchable = false };
			if ( obj.Model != value )
				obj.Model = value;

			var transform = Parent.Transform.World.Add( Position3D * (Vector3)Chunk.Size * Parent.VoxelScale, true );
			obj.Transform = transform;

			Parent.AssignAttributes( obj.Attributes );
		}
	}

	private SceneObject obj;

	public ChunkObject()
	{
		// Body = GameManager.ActiveScene.PhysicsWorld.Body;
	}

	public void Delete()
	{
		// Let's delete our Model SceneObject for the chunk.
		//obj?.Delete();
		Shape?.Remove();
	}
	
	public void BuildCollision( CollisionBuffer buffer )
	{
		// Shape?.Remove();
		// Shape = Body.AddMeshShape( buffer.Vertex, buffer.Index );
		// Log.Error( (Shape, Body) );
		// 
		// Shape.Tags.Add( "solid" );
	}

	public bool Equals( ChunkObject other )
	{
		return other.Position3D.Equals( Position3D );
	}

	public override bool Equals( object obj )
	{
		return obj is ChunkObject other
			&& Equals( other );
	}

	public override int GetHashCode()
	{
		return Position3D.GetHashCode();
	}
}

public partial class VoxelWorld : Component
{
	[Property] public string Path { get; set; }

	public static IReadOnlyList<VoxelWorld> All => all;
	private static List<VoxelWorld> all = new();

	public Dictionary<Vector3S, Chunk> Chunks { get; private set; }

	private Dictionary<Vector3S, ChunkObject> objects = new();

	private Material material = Material.FromShader( "shaders/voxel.shader" );

	public Vector3 VoxelScale { get; set; } = Utility.Scale;
	public TextureAtlas Atlas { get; set; } = TextureAtlas.Get( "resources/textures/default.atlas" );

	public void AssignAttributes( RenderAttributes attributes )
	{
		attributes.Set( "VoxelScale", VoxelScale );
		attributes.Set( "Albedo", Atlas.Albedo );
		attributes.Set( "RAE", Atlas.RAE );
		attributes.Set( "TextureSize", Atlas.TextureSize );
		attributes.Set( "AtlasSize", Atlas.Size );
	}

	public VoxelWorld()
	{
		if ( all.Contains( this ) )
			return;
		
		all.Add( this );
	}

	protected override void OnDestroy()
	{
		all.Remove( this );

		foreach ( var (_, child) in objects )
			child.Delete();

		Chunks = null;
	}

	/// <summary>
	/// Generates a chunk.
	/// </summary>
	/// <param name="chunk"></param>
	public void GenerateChunk( Chunk chunk )
	{
		// Get our chunk's entity.
		if ( chunk == null )
			return;

		ChunkObject chunkObj;
		if ( !objects.TryGetValue( chunk.Position, out chunkObj ) )
			objects.Add( chunk.Position, chunkObj = new ChunkObject() { Parent = this, Position3D = chunk.Position } );
		
		// Let's create a mesh.
		var mesh = new Mesh( material );
		var vertices = new List<VoxelVertex>();
		var indices = new List<int>();
		var offset = 0;

		var tested = new bool[Chunk.DEFAULT_WIDTH, Chunk.DEFAULT_DEPTH, Chunk.DEFAULT_HEIGHT];
		var buffer = new CollisionBuffer();
		buffer.Init( true );

		chunk.Empty = true;
		for ( ushort x = 0; x < Chunk.DEFAULT_WIDTH; x++ )
		for ( ushort y = 0; y < Chunk.DEFAULT_DEPTH; y++ )
		for ( ushort z = 0; z < Chunk.DEFAULT_HEIGHT; z++ )
		{
			var voxel = chunk.GetVoxel( x, y, z );
			if ( voxel == null )				
				continue;

			chunk.Empty = false;

			var position = new Vector3B( x, y, z );

			// Let's start checking for collisions.
			if ( !tested[x, y, z] )
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

			// Generate all visible faces for our voxel.
			var drawCount = 0;
			for ( var i = 0; i < Utility.Faces; i++ )
			{
				var direction = Utility.Directions[i];
				var neighbour = chunk.GetDataByOffset( x + direction.x, y + direction.y, z + direction.z ).Voxel;
				if ( neighbour != null )
					continue;
			
				for ( var j = 0; j < 4; ++j )
				{
					var vertexIndex = Utility.FaceIndices[(i * 4) + j];
					var ao = Utility.BuildAO( chunk, position, i, j );

					vertices.Add( new VoxelVertex( position, vertexIndex, (byte)i, ao, voxel.Value.Color, 0 ) );
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

		// Check if we actually end up with vertices.
		if ( vertices.Count > 0 )
		{
			mesh.CreateVertexBuffer<VoxelVertex>( vertices.Count, VoxelVertex.Layout, vertices.ToArray() );
			mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

			chunkObj.Model = Model.Builder
				.AddMesh( mesh )
				.Create();
		}

		// Do physics.
		if ( vertices.Count > 0 && GameManager.IsPlaying )
			chunkObj.BuildCollision( buffer );
	}

	public override async void Reset()
	{
		Chunks = await BaseImporter.Get<VoxImporter>()
			.BuildAsync( Path );

		foreach ( var (_, chunk) in Chunks )
			GenerateChunk( chunk );
	}

	#region DEBUG
	protected override void DrawGizmos()
	{
		// Display all chunks.
		foreach ( var (_, chunk) in objects )
		{
			if ( chunk == null )
				continue;

			var pos = Transform.Position
				+ (Vector3)chunk?.Position3D * VoxelScale * Chunk.Size;

			AssignAttributes( Gizmo.Camera.Attributes );
			Gizmo.Draw.Model( chunk.Model, new Transform( pos + VoxelScale / 2 ) );

			Gizmo.Draw.Color = Color.Yellow;
			Gizmo.Draw.LineThickness = 0.1f;
			Gizmo.Draw.LineBBox( new BBox( pos, pos + (Vector3)Chunk.Size * VoxelScale ) );
		}

		// Focus on hovered VoxelWorld.
		/*var ray = new Ray( Camera.Position, Camera.Rotation.Forward );
		var tr = Trace.Ray( ray, 10000f )
			.WithTag( "chunk" )
			.Run();

		var parent = (tr.Entity as ChunkEntity)?.Parent;
		if ( parent == null )
			return;

		var position = parent.WorldToVoxel( tr.EndPosition - tr.Normal * parent.VoxelScale / 2f );
		var data = parent.GetByOffset( position.x, position.y, position.z );

		// Debug
		DebugOverlay.ScreenText( $"{data.Voxel?.Color ?? default}" );
		DebugOverlay.ScreenText( $"XYZ: {position}", 1 );
		DebugOverlay.ScreenText( $"Chunk: {data.Chunk?.Position}", 2 );

		var voxelCenter = (Vector3)position * parent.VoxelScale
			+ parent.VoxelScale / 2f
			+ parent.Position;

		Gizmo.Draw.Color = Color.Black;
		Gizmo.Draw.LineThickness = 1;
		Gizmo.Draw.LineBBox( new BBox( voxelCenter - parent.VoxelScale / 2f, voxelCenter + parent.VoxelScale / 2f ) );*/
	}
	#endregion
}
