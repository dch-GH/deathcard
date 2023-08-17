global using Sandbox;
global using Sandbox.UI.Construct;
global using System;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Text;
global using System.Runtime.InteropServices;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace DeathCard;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class MyGame : GameManager
{
	public MyGame()
	{
		// Load a map.
		if ( Game.IsServer )
			VoxelWorld.Create( "vox/maps/monument.vox" );
		else
			_ = new HUD();
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new Player();
		client.Pawn = pawn;
		pawn.Position = new Vector3( 2020f, 1991f, 3835f );
	}
}
