namespace DeathCard.Formats;

public abstract class BaseFormat
{
	public abstract string Extension { get; }

	public virtual async Task<Dictionary<Vector3S, Chunk>> Build( string path )
	{
		throw new NotImplementedException();
	}
}
