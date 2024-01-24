namespace Deathcard;

public class Explosive : Component
{
	[Property] public float Duration { get; set; } = 4f;
	[Property, Range(1, 100)] public int Radius { get; set; } = 5;

	public Rigidbody Rigidbody { get; private set; }

	TimeUntil _shouldExplode;

	protected override void OnAwake()
	{
		_shouldExplode = 1;
		Rigidbody = Components.Get<Rigidbody>();
	}

	protected override void OnUpdate()
	{
		Move();

		Gizmo.Draw.LineSphere( new Sphere( Transform.Position, Utility.Scale * Radius ), 6 );

		if ( _shouldExplode )
		{
			Explode();
			GameObject.Destroy();
		}
	}

	private void Move() // todo @ceitine: custom physics
	{
		var target = Rotation.LookAt( Rigidbody.Velocity );
		Transform.Rotation = Rotation.Lerp( Transform.Rotation, target, 10f * Time.Delta );
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
