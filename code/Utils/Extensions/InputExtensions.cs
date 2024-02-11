namespace Deathcard;

public static class InputExtensions
{
	/// <summary>
	/// Gets a vector input direction based on input actions.
	/// </summary>
	/// <param name="forward"></param>
	/// <param name="backward"></param>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static Vector3 GetDirection( string forward, string backward, string left, string right )
	{
		var target = Vector3.Zero;
		if ( Input.Down( forward ) ) target.x++;
		if ( Input.Down( backward ) ) target.x--;
		if ( Input.Down( left ) ) target.y++;
		if ( Input.Down( right ) ) target.y--;
		return target;
	}
}
