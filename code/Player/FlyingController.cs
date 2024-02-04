using Sandbox;
using Sandbox.Citizen;
using System.Diagnostics;
using System.Linq;

namespace Deathcard;

public class FlyingController : Controller
{
	[Property]
	[Category( "Components" )]
	public Player Player { get; set; }

	/// <summary>
	/// How fast you fly
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 2000f, 10f )]
	public float FlySpeed { get; set; } = 300f;

	/// <summary>
	/// How fast you fly when sprinting
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 4000f, 10f )]
	public float SprintFlySpeed { get; set; } = 600f;

	/// <summary>
	/// How fast you fly when walking
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000f, 10f )]
	public float WalkFlySpeed { get; set; } = 150f;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Player == null ) return;

		var isWalking = Input.Down( "Walk" );
		var isSprinting = Input.Down( "Sprint" );
		var isCrouching = Input.Down( "Crouch" );
		var isJumping = Input.Down( "Jump" );

		var wishSpeed = isWalking ? WalkFlySpeed : (isSprinting ? SprintFlySpeed : FlySpeed); // If walking use walk speed, else if sprinting use sprint speed, else use normal speed
		var wishVerticalSpeed = wishSpeed * ((isCrouching ? -1 : 0) + (isJumping ? 1 : 0)); // If crouching go down, if jumping go up, if both do nothing
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Player.EyeAngles + Vector3.Up * wishVerticalSpeed;

		WishVelocity = wishVelocity;
	}
}
