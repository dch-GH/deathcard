namespace DeathCard.Importer;

public abstract class BaseImporter
{
	// Cache all of the importers that we can find with reflection.
	public static Dictionary<string, BaseImporter> Cache = TypeLibrary?
		.GetTypes<BaseImporter>()?
		.Where( type => !type.IsAbstract )
		.Select( type => type.Create<BaseImporter>() )
		.ToDictionary( instance => instance.Extension );

	/// <summary>
	/// The file extension this importer should look for.
	/// </summary>
	public abstract string Extension { get; }

	/// <summary>
	/// The method that is supposed to be implemented so that we can generate chunks based on our data.
	/// </summary>
	/// <param name="builder"></param>
	/// <returns></returns>
	public virtual Task<Dictionary<Vector3S, Chunk>> BuildAsync( VoxelBuilder builder )
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Gets type T of a BaseImporter.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static T Get<T>() where T : BaseImporter
	{
		// Try and find our BaseImporter from the cache.
		foreach ( var (_, importer) in Cache ) 
			if ( importer.GetType() == typeof ( T ) )
				return (T)importer;

		return null;
	}
}
