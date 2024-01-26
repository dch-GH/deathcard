namespace Deathcard;

public class Explosive : Component
{
	[Property] public float Duration { get; set; } = 4f;
	[Property, Range(1, 100)] public int Radius { get; set; } = 5;

	public Vector3 Velocity { get; set; }
	public GameObject GroundObject { get; set; }
	public BoxCollider Collider { get; set; }

	TimeUntil _shouldExplode;

	protected override void OnAwake()
	{
		Collider = Components.Get<BoxCollider>( true );
		_shouldExplode = 1;
	}

	protected override void OnUpdate()
	{
		Gizmo.Draw.LineSphere( new Sphere( Transform.Position, Utility.Scale * Radius ), 6 );
	}

	protected override void OnFixedUpdate()
	{
		Move();

		if ( _shouldExplode )
		{
			Explode();
			GameObject.Destroy();
		}
	}

	private void Move() // todo @ceitine: custom physics
	{
		// Set rotation.
		if ( !Velocity.IsNearlyZero( 1f ) )
			Transform.Rotation = Rotation.LookAt( Velocity.Normal );

		// Apply Gravity.
		if ( !GroundObject.IsValid() )
			Velocity += Vector3.Down * Player.GRAVITY * Time.Delta;

		// Use move helper to advance.
		var tr = Scene.Trace
			.Size( Collider.Scale )
			.IgnoreGameObjectHierarchy( GameObject );

		var helper = new CharacterControllerHelper( tr, Transform.Position, Velocity );
		if ( GroundObject.IsValid() )
			helper.Velocity *= 0.9f;

		// Apply new helper values and bounce.
		if ( helper.TryMove( Time.Delta ) > 0 )
			Transform.Position = helper.Position;

		// Let's apply some bounce.
		var velocity = helper.Velocity;
		var bounced = false;
		if ( MathF.Abs( Velocity.z - velocity.z ) > 10 ) // Bounce on Z-axis.
		{
			velocity += Vector3.Up * -Velocity.z * 0.4f;
			velocity *= 0.5f;
			bounced = true;
		}

		if ( MathF.Abs( Velocity.x - velocity.x ) > 10 && !bounced ) // Bounce on X-axis.
		{
			velocity += Vector3.Forward * -Velocity.x * 0.4f;
			velocity *= 0.5f;
		}

		if ( MathF.Abs( Velocity.y - velocity.y ) > 10 && !bounced ) // Bounce on Y-axis.
		{
			velocity += Vector3.Left * -Velocity.y * 0.4f;
			velocity *= 0.5f;
		}

		// Finally apply final velocity.
		Velocity = velocity;

		// Check for ground collision.
		GroundObject = Velocity.z <= 2f
			? helper
				.TraceFromTo( Transform.Position, Transform.Position + Vector3.Down * 2f )
					.GameObject
			: null;
	}

	public bool Explode()
	{
		var chunks = new Collection<Chunk>();
		var world = VoxelWorld.All.FirstOrDefault();
		/*var hit = Scene.Trace.Sphere( Utility.Scale * Radius, new Ray( Transform.Position, Vector3.Down ), 5f )
			.IgnoreGameObject( GameObject )
			.RunAll();
		
		foreach ( var obj in hit )
		{
			if ( world == null && obj.Component is VoxelChunk chunk )
				world = chunk.Parent;
			Log.Error( obj.GameObject );
		}*/

		if ( world == null )
			return false;

		// Get position in voxel space.
		var position = world.WorldToVoxel( Transform.Position + Vector3.Down * world.VoxelScale / 2f );

		// Remove voxels in a sphere.
		var center = new Vector3( position.x, position.y, position.z );

		for ( int x = 0; x <= Radius; x++ )
		for ( int y = 0; y <= Radius; y++ )
		for ( int z = 0; z <= Radius; z++ )
		{
			var pos = center
				+ new Vector3( x, y, z )
				- Radius / 2f;

			var dist = pos.Distance( center );
			if ( dist >= Radius / 2f )
				continue;

			var target = (
				x: (pos.x + 0.5f).FloorToInt(),
				y: (pos.y + 0.5f).FloorToInt(),
				z: (pos.z + 0.5f).FloorToInt()
			);

			var chunk = world.SetVoxel( target.x, target.y, target.z, null ); // todo @ceitine: fix???
			if ( chunk != null && !chunks.Contains( chunk ) )
				chunks.Add( chunk );
		}

		foreach ( var chunk in chunks )
			world.GenerateChunk( chunk );

		return true;
	}
}
