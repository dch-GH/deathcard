namespace Deathcard;

[Flags]
public enum RotationFlags
{
	None = 0,
	Velocity = 1 << 0,
	Roll = 1 << 1,
}

public class Explosive : Component
{
	[Property] public float Duration { get; set; } = 4f;
	[Property] public bool ExplodeOnImpact { get; set; } = false;
	[Property, Range(1, 100)] public int Radius { get; set; } = 5;

	[Property, Category( "Physics" )] public Vector3 Gravity { get; set; } = Vector3.Down * 650f;
	[Property, Category( "Physics" )] public float FrictionMultiplier { get; set; } = 0.9f;
	[Property, Category( "Physics" )] public RotationFlags RotationMode { get; set; } = RotationFlags.Velocity;

	public Vector3 Velocity { get; set; }
	public GameObject GroundObject { get; set; }

	public float Angle { get; private set; }
	public ModelRenderer Renderer { get; private set; }
	public BoxCollider Collider { get; private set; }

	TimeUntil _shouldExplode;

	protected override void OnAwake()
	{
		Collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndDescendants );
		Renderer = Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		_shouldExplode = Duration;
	}

	protected override void OnUpdate()
	{
		// Radius gizmo
		Gizmo.Draw.Color = Color.Red.WithAlpha( 0.3f );	
		Gizmo.Draw.SolidSphere( Transform.Position, Utility.Scale * Radius / 2 );

		Gizmo.Draw.Color = Color.White.WithAlpha( 0.1f );
		Gizmo.Draw.LineSphere( new Sphere( Transform.Position, Utility.Scale * Radius / 2 ) );

		// Blink tint
		if ( ExplodeOnImpact ) return;

		var t = _shouldExplode.Passed;
		var f = MathF.Sin( t * t ) / 2f + 0.5f;
		Renderer.Tint = Color.Lerp( Color.White, Color.Red, f );
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
		Angle += 360f * Time.Delta;

		if ( !Velocity.IsNearlyZero( 1f ) )
		{
			Transform.Rotation = RotationMode.HasFlag( RotationFlags.Roll )
				? Rotation.LookAt( Velocity.Normal ).RotateAroundAxis( Velocity.Normal, Angle )
				: RotationMode.HasFlag( RotationFlags.Velocity )
					? Rotation.LookAt( Velocity.Normal )
					: Transform.Rotation;
		}

		// Apply Gravity.
		if ( !GroundObject.IsValid() )
			Velocity += Gravity * Time.Delta;

		// Use move helper to advance.
		var tr = Scene.Trace
			.Size( Collider.Scale / 2 )
			.IgnoreGameObjectHierarchy( GameObject );

		var helper = new CharacterControllerHelper( tr, Transform.Position, Velocity );
		if ( GroundObject.IsValid() )
			helper.Velocity *= FrictionMultiplier;

		// Apply new helper values and bounce.
		if ( helper.TryMove( Time.Delta ) > 0 )
			Transform.Position = helper.Position;

		// Let's apply some bounce.
		var velocity = helper.Velocity;
		if ( ExplodeOnImpact && velocity.Length < Velocity.Length )
		{
			Explode();
			GameObject.Destroy();
		}

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
		const float MIN_FORCE = 1000f;
		const float MAX_FORCE = 2000f;

		var world = (VoxelWorld)null;

		foreach ( var obj in Scene.FindInPhysics( new Sphere( Transform.Position, Radius * Utility.Scale ) ) )
		{
			// Ignore self.
			if ( obj == GameObject )
				continue;

			// Get VoxelWorld.
			if ( world == null && obj.Components.TryGet<VoxelChunk>( out var chunk ) )
			{
				world = chunk.Parent;
				continue;
			}

			// Force variables.
			var normal = (obj.Transform.Position - Transform.Position).Normal;
			var distance = obj.Transform.Position.Distance( Transform.Position );
			var force = MathX.LerpTo( MAX_FORCE, MIN_FORCE, distance / (Radius * Utility.Scale) );

			// Apply velocity to nearby explosives.
			if ( obj.Components.TryGet<Explosive>( out var explosive ) )
			{
				explosive.GroundObject = null;
				explosive.Velocity += (normal + Vector3.Up * 0.5f) * force;
			}
		}

		if ( world == null )
			return false;

		// Get position in voxel space.
		var position = world.WorldToVoxel( Transform.Position + Vector3.Down * world.VoxelScale / 2f );

		// Remove voxels in a sphere.
		var center = new Vector3( position.x, position.y, position.z );
		var chunks = new Collection<Chunk>();

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

			var data = world.SetVoxel( target.x, target.y, target.z, null );
			if ( data.Chunk == null ) 
				continue;

			var neighbors = data.Chunk.GetNeighbors( data.Position.x, data.Position.y, data.Position.z );
			foreach ( var neighbor in neighbors )
			{
				if ( neighbor == null || chunks.Contains( neighbor ) )
					continue;

				chunks.Add( neighbor );
			}
		}

		foreach ( var chunk in chunks )
			_ = world.GenerateChunk( chunk );

		return true;
	}
}
