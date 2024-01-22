namespace Deathcard;

public class CollisionBuffer
{
	public List<Vector3> Vertex = new List<Vector3>( 32 );
	public List<int> Index = new List<int>( 32 );

	public bool Indexed { get; private set; }

	public void Clear()
	{
		this.Vertex.Clear();
		this.Index.Clear();
	}

	public virtual void Init( bool useIndexBuffer )
	{
		this.Indexed = useIndexBuffer;
		this.Clear();
	}

	/// <summary>
	/// Add a vertex.
	/// </summary>
	public void Add( Vector3 v ) => this.Vertex.Add( v );

	/// <summary>
	/// Add an index. 
	/// This is relative to the top of the vertex buffer. So 0 is Vertex.Count., 1 is Vertex.Count -1
	/// </summary>
	public void AddIndex( int i ) => this.AddRawIndex( this.Vertex.Count - i );

	/// <summary>
	/// Add an index. This is relative to the top of the vertex buffer. So 0 is Vertex.Count.
	/// </summary>
	public void AddTriangleIndex( int a, int b, int c )
	{
		this.AddIndex( a );
		this.AddIndex( b );
		this.AddIndex( c );
	}

	/// <summary>
	/// Add an index. This is relative to the top of the vertex buffer. So 0 is Vertex.Count.
	/// </summary>
	public void AddRawIndex( int i ) => this.Index.Add( i );

	/// <summary>
	/// Add a triangle to the vertex buffer. Will include indices if they're enabled.
	/// </summary>
	public void AddTriangle( Vector3 a, Vector3 b, Vector3 c )
	{
		Add( a );
		Add( b );
		Add( c );

		if ( Indexed )
		{
			AddTriangleIndex( 3, 2, 1 );
		}
	}

	/// <summary>
	/// Add a quad to the vertex buffer. Will include indices if they're enabled.
	/// </summary>
	public void AddQuad( Vector3 a, Vector3 b, Vector3 c, Vector3 d )
	{
		if ( Indexed )
		{
			Add( a );
			Add( b );
			Add( c );
			Add( d );

			AddTriangleIndex( 4, 3, 2 );
			AddTriangleIndex( 2, 1, 4 );
		}
		else
		{
			Add( a );
			Add( b );
			Add( c );

			Add( c );
			Add( d );
			Add( a );
		}
	}

	/// <summary>
	/// Add a quad to the vertex buffer. Will include indices if they're enabled.
	/// </summary>
	public void AddQuad( Ray origin, Vector3 width, Vector3 height )
	{
		AddQuad( origin.Position - width - height, origin.Position + width - height,
			origin.Position + width + height, origin.Position - width + height );
	}

	/// <summary>
	/// Add a cube to the vertex buffer. Will include indices if they're enabled.
	/// </summary>
	public void AddCube( Vector3 center, Vector3 size, Rotation rot )
	{
		var f = rot.Forward * size.x * 0.5f;
		var l = rot.Left * size.y * 0.5f;
		var u = rot.Up * size.z * 0.5f;

		// Forward & backward.
		AddQuad( new Ray( center + f, f.Normal ), l, u );
		AddQuad( new Ray( center - f, -f.Normal ), l, -u );

		// Left & right.
		AddQuad( new Ray( center + l, l.Normal ), -f, u );
		AddQuad( new Ray( center - l, -l.Normal ), f, u );

		// Up & down.
		AddQuad( new Ray( center + u, u.Normal ), f, l );
		AddQuad( new Ray( center - u, -u.Normal ), f, -l );
	}
}
