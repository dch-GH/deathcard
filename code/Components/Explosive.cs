using Deathcard.Importer;

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

	protected override void OnFixedUpdate()
	{
		Move();

		if ( _shouldExplode )
			Explode();
	}

	private void Move() // todo @ceitine: custom physics
	{

	}

	public bool Explode()
	{
		var chunks = new Collection<Chunk>();
		var closest = VoxelWorld.All
			.OrderBy( v => v.Transform.Position.Distance( Transform.Position ) )
			.FirstOrDefault();

		// Get position in voxel space.
		if ( closest == null )
			return false;

		var position = closest.WorldToVoxel( Transform.Position + Vector3.Down * closest.VoxelScale / 2f );

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
			
			var data = closest.GetByOffset( target.x, target.y, target.z );
			data.Chunk?.SetVoxel( data.Position.x, data.Position.y, data.Position.z, null );
			// set voxel not working?
			if ( data.Chunk != null && !chunks.Contains( data.Chunk ) )
				chunks.Add( data.Chunk );
		}

		foreach ( var chunk in chunks )
			closest.GenerateChunk( chunk );

		GameObject.Destroy();

		return true;
	}
}
