using Sandbox;
using System.Diagnostics;
using System.Linq;

namespace Deathcard;

public class Player : Component
{
	[Property] public GameObject Shootable { get; set; }

	public const float SPEED = 500f;
	public const float JUMP_POWER = 100f;
	public const float STEP_SIZE = Utility.Scale / 4f;
	public const float FLY_SPEED = 1500f;
	public const float GRAVITY = 800f;

	public const float MOUSE_SENSITIVITY = 0.15f;

	public ModelRenderer Renderer { get; private set; }
	public CameraComponent Camera { get; private set; }

	public Vector3 Velocity { get; private set; }
	public GameObject GroundObject { get; private set; }
	public bool Grounded => GroundObject.IsValid();
	public bool Flying { get; private set; }
	public BBox Bounds { get; } = new(
		new Vector3( -Utility.Scale / 2f, -Utility.Scale / 2f, 0 ),
		new Vector3( Utility.Scale / 2f, Utility.Scale / 2f, 72 ) );

	protected override void OnAwake()
	{
		Renderer = Components.Get<ModelRenderer>( FindMode.InDescendants );
		Camera = Components.Get<CameraComponent>( FindMode.InDescendants );
	}

	protected override void OnUpdate()
	{
		// Toggle noclip
		if ( Input.Pressed( "Noclip" ) )
			Flying = !Flying;

		// Rotation
		var delta = Mouse.Delta * MOUSE_SENSITIVITY;
		var ang = Camera.Transform.Rotation.Angles();
		var pitch = MathX.Clamp( ang.pitch + delta.y, -89, 89 );
		Renderer.Transform.LocalRotation = Rotation.FromYaw( ang.yaw );
		Camera.Transform.LocalRotation = Rotation.From( pitch, ang.yaw - delta.x, 0 );

		// Movement
		Move();

		// Shoot explosives
		if ( Input.Pressed( "Interact" ) )
		{
			const float FORCE = 2500f;

			var dir = Camera.Transform.Rotation.Forward;
			var obj = Shootable.Clone();
			obj.Transform.Position = Camera.Transform.Position + dir * 100f;

			var c = obj.Components.Get<Explosive>();
			c.Velocity += dir * FORCE;
		}

		// Place and remove spheres.
		var ray = new Ray( Camera.Transform.Position, Camera.Transform.Rotation.Forward );
		var tr = Scene.Trace.Ray( ray, 10000f )
			.Run();

		var parent = tr.GameObject?.Components.Get<VoxelChunk>()?.Parent;
		var position = parent?.WorldToVoxel( tr.EndPosition - tr.Normal * parent.VoxelScale / 2f );
		if ( position == null )
			return;

		var ce = parent.Transform.Position + position.Value * parent.VoxelScale + parent.VoxelScale / 2f;
		var bbox = new BBox( ce - parent.VoxelScale / 2f, ce + parent.VoxelScale / 2f );

		Gizmo.Draw.Color = Color.Black;
		Gizmo.Draw.LineThickness = 1;
		Gizmo.Draw.LineBBox( bbox );

		Gizmo.Draw.Color = Color.Black.WithAlpha( 0.5f );
		Gizmo.Draw.SolidBox( bbox );

		var value = position.Value;
		var pos = parent.GetLocalSpace( value.x, value.y, value.z, out var local );

		var size = 8;
		var set = Input.Down( "Primary_Attack" )
			? 1
			: Input.Down( "Secondary_Attack" )
				? -1
				: 0;

		if ( set != 0 )
		{
			var chunks = new Collection<Chunk>();

			for ( int x = 0; x <= size; x++ )
			for ( int y = 0; y <= size; y++ )
			for ( int z = 0; z <= size; z++ )
			{
				var center = (Vector3)pos;
				var target = center
					+ new Vector3( x, y, z )
					- size / 2f;

				if ( target.Distance( center ) >= size / 2f + 0.5f )
					continue;

				var voxel = set == 1
					? new Block() { TextureId = (ushort)Game.Random.Int( 0, 2 ) }
					: (IVoxel)null;

				var data = parent.SetVoxel(
					target.x.FloorToInt(),
					target.y.FloorToInt(),
					target.z.FloorToInt(), voxel, local );

				if ( data.Chunk != null && !chunks.Contains( data.Chunk ) )
					chunks.Add( data.Chunk );

				var neighbors = data.Chunk.GetNeighbors( data.Position.x, data.Position.y, data.Position.z, true );
				foreach ( var neighbor in neighbors )
				{
					if ( neighbor != null || chunks.Contains( neighbor ) )
						continue;

					chunks.Add( neighbor );
				}
			}

			foreach ( var chunk in chunks )
				_ = parent.GenerateChunk( chunk );
		}
	}

	private void Move()
	{
		var direction = InputExtensions.GetDirection( "Forward", "Backward", "Left", "Right" );

		// Flying
		if ( Flying )
		{
			Transform.Position += direction
				* FLY_SPEED
				* Camera.Transform.Rotation
				* Time.Delta;

			Velocity = 0;

			return;
		}

		// Jump
		if ( Input.Down( "Jump" ) && Grounded )
			Velocity += JUMP_POWER * Vector3.Up;

		// Normal movement
		Velocity = (direction * SPEED * Renderer.Transform.Rotation)
			.WithZ( Velocity.z ); // Maintain gravity.

		var tr = Scene.Trace.Box( Bounds, Transform.Position, Transform.Position )
			.IgnoreGameObjectHierarchy( GameObject );

		var helper = new CharacterControllerHelper( tr, Transform.Position, Velocity );
	
		if ( helper.TryMoveWithStep( Time.Delta, STEP_SIZE ) > 0 )
		{
			Transform.Position = helper.Position;
			Velocity = helper.Velocity;
		}

		// Gravity
		var down = helper.TraceFromTo( Transform.Position, Transform.Position + Vector3.Down * 2f );
		GroundObject = down.GameObject;

		if ( !Grounded )
			Velocity += Vector3.Down * GRAVITY * Time.Delta;
		else
			Transform.Position = down.EndPosition;
	}
}
