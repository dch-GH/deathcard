namespace DeathCard.Importer;

public interface IChunk
{
	public string ChunkID { get; }
	public int Bytes { get; }

	public IChunk[] Children { get; }
}
