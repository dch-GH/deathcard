namespace Deathcard;

public class Player : Component
{
	TimeSince lastShot;

	protected override void OnUpdate()
	{
		const float SENSITIVITY = 0.15f;
		const float SPEED = 1250f;

		// Rotation
		var delta = Mouse.Delta * SENSITIVITY;
		var ang = Transform.Rotation.Angles();
		var pitch = MathX.Clamp( ang.pitch + delta.y, -89, 89 );
		Transform.Rotation = Rotation.From( pitch, ang.yaw - delta.x, 0 );

		// Movement
		var direction = InputExtensions.GetDirection( "Forward", "Backward", "Left", "Right" );
		Transform.Position += direction 
			* SPEED 
			* Transform.Rotation 
			* Time.Delta;

		// Shoot boxes
		if ( Input.Down( "Primary_Attack" ) && lastShot > 0.1f )
		{
			lastShot = 0f;

			var obj = new GameObject();
			obj.Transform.Position = Transform.Position + Transform.Rotation.Forward * 10f;
			obj.Transform.Scale = Game.Random.Float( 0.5f, 5f );

			var renderer = obj.Components.GetOrCreate<ModelRenderer>();
			renderer.Model = Model.Load( "models/dev/box.vmdl" );

			var collider = obj.Components.GetOrCreate<BoxCollider>();
			var rigidbody = obj.Components.GetOrCreate<Rigidbody>();
			rigidbody.Velocity += Transform.Rotation.Forward * 10000f;

			GameTask.RunInThreadAsync( async () => { await GameTask.Delay( 5000 ); obj.Destroy(); } );
		}
	}
}
