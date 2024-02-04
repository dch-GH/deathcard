using Sandbox;
using Sandbox.Citizen;
using System.Diagnostics;
using System.Linq;

namespace Deathcard;

public class Player : Component
{
	[Property] public GameObject Shootable { get; set; }

	[Property]
	[Category( "Components" )]
	public GameObject Camera { get; set; }

	[Property]
	[Category( "Components" )]
	public Controller WalkController { get; set; }

	/// <summary>
	/// It's better to have this here and disable when not used so we can tweak the stats in the editor
	/// </summary>
	[Property]
	[Category( "Components" )]
	public Controller FlyingController { get; set; }

	/// <summary>
	/// How fast you can walk (Units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 400f, 1f )]
	public float WalkSpeed { get; set; } = 120f;

	/// <summary>
	/// How fast you can run (Units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 800f, 1f )]
	public float RunSpeed { get; set; } = 250f;

	/// <summary>
	/// How powerful you can jump (Units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000f, 10f )]
	public float JumpStrength { get; set; } = 400f;

	/// <summary>
	/// Where the camera is placed and where rays are casted from
	/// </summary>
	[Property]
	public Vector3 EyePosition { get; set; }

	public Angles EyeAngles { get; set; }

	public bool IsFlying { get; set; } = false;

	protected override void DrawGizmos()
	{
		var draw = Gizmo.Draw;

		draw.Arrow( EyePosition, EyePosition + Transform.Rotation.Forward * 25f, arrowWidth: 2f ); // Draw an arrow coming off of the eye position
	}

	protected override void OnStart()
	{
		if ( FlyingController != null )
			FlyingController.Enabled = IsFlying;
		if ( WalkController != null )
			WalkController.Enabled = !IsFlying;
	}

	protected override void OnUpdate()
	{
		EyeAngles += Input.AnalogLook;
		EyeAngles = EyeAngles.WithPitch( MathX.Clamp( EyeAngles.pitch, -80f, 80f ) );
		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

		if ( Camera != null )
		{
			Camera.Transform.Position = Transform.World.PointToWorld( EyePosition );
			Camera.Transform.Rotation = EyeAngles;
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		// Toggle noclip
		if ( Input.Pressed( "Noclip" ) )
		{
			IsFlying = !IsFlying;

			if ( WalkController != null )
				WalkController.Enabled = !IsFlying;
			if ( FlyingController != null )
				FlyingController.Enabled = IsFlying;
		}

		Log.Info( $"Walk: {WalkController.Enabled}" );
		Log.Info( $"Fly: {FlyingController.Enabled}" );

		/*
		if ( WalkController == null || !WalkController.Enabled ) return;

		var wishSpeed = Input.Down( "Sprint" ) ? RunSpeed : WalkSpeed;
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Transform.Rotation;

		WalkController.Accelerate( wishVelocity );

		if ( WalkController.IsOnGround )
		{
			WalkController.Acceleration = 10f;
			WalkController.ApplyFriction( 10f, 10f );

			if ( Input.Pressed( "Jump" ) )
				WalkController.Punch( Vector3.Up * JumpStrength );
		}
		else
		{
			WalkController.Acceleration = 5f;
			WalkController.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		}

		WalkController.Move();

		Log.Info( WalkController.Velocity.Length );*/
	}

	/*
	protected override void OnUpdate()
	{
		// Toggle noclip
		if ( Input.Pressed( "Noclip" ) )
		{
			IsFlying = !IsFlying;
		}

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
			const float FORCE = 500f;

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
	}*/
}
