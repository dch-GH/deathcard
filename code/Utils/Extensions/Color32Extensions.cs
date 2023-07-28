namespace DeathCard;

public static partial class Extensions
{
	/// <summary>
	/// Multiplies a Color32 by a float value.
	/// </summary>
	/// <param name="color"></param>
	/// <param name="amount"></param>
	/// <returns></returns>
	public static Color32 Multiply( this Color32 color, float amount )
		=> new Color32( (byte)(color.r * amount), (byte)(color.g * amount), (byte)(color.b * amount) );

	/// <summary>
	/// Clamps a Color32 to a minimum and maximum value.
	/// </summary>
	/// <param name="color"></param>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	public static Color32 Clamp( this Color32 color, byte min = 0, byte max = 255 )
		=> new Color32( color.r.Clamp( min, max ), color.g.Clamp( min, max ), color.b.Clamp( min, max ) );
}
