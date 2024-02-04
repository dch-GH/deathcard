using Sandbox;
using Sandbox.Citizen;
using System.Diagnostics;
using System.Linq;

namespace Deathcard;

public class WalkController : Controller
{
	[Property]
	[Category( "Components" )]
	public Player Player { get; set; }

	/// <summary>
	/// How fast you move normally
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 400f, 1f )]
	public float Speed { get; set; } = 140f;

	/// <summary>
	/// How fast you move when holding the sprint button
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 800f, 1f )]
	public float SprintSpeed { get; set; } = 280f;

	/// <summary>
	/// How fast you move when holding the walk button
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 200f, 1f )]
	public float WalkSpeed { get; set; } = 80f;

	/// <summary>
	/// How fast you move when holding the crouch button
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 200f, 1f )]
	public float CrouchSpeed { get; set; } = 60f;

	/// <summary>
	/// How high you can jump
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 800f, 1f )]
	public float JumpStrength { get; set; } = 200f;


	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Player == null ) return;

		var isWalking = Input.Down( "Walk" );
		var isSprinting = Input.Down( "Sprint" );
		var isCrouching = Input.Down( "Crouch" );

		var wishSpeed = isCrouching ? CrouchSpeed : (isWalking ? WalkSpeed : (isSprinting ? SprintSpeed : Speed));
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Player.EyeAngles.WithPitch( 0f );

		WishVelocity = wishVelocity;

		if ( Input.Pressed( "Jump" ) && IsOnGround )
			Punch( Vector3.Up * JumpStrength );
	}
}
