namespace Deathcard;

public class Player : Component
{
	[Property] public GameObject Shootable { get; set; }

	public const float SPEED = 500f;
	public const float JUMP_POWER = 250f;
	public const float STEP_SIZE = Utility.Scale / 4f;
	public const float FLY_SPEED = 1500f;
	public const float GRAVITY = 650f;

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
		if ( Input.Pressed( "Primary_Attack" ) )
		{
			const float FORCE = 2500f;

			var dir = Camera.Transform.Rotation.Forward;
			var obj = Shootable.Clone();
			obj.Transform.Position = Camera.Transform.Position + dir * 100f;

			var rb = obj.Components.Get<Rigidbody>();
			rb.Velocity += dir * FORCE;
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
