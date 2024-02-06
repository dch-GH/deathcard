namespace Deathcard;

public struct AStarNode : IHeapItem<AStarNode>, IEquatable<AStarNode>
{
	public IVoxel Current { get; internal set; } = null;
	public float gCost { get; internal set; } = 0f;
	public float hCost { get; internal set; } = 0f;
	public float fCost => gCost + hCost;
	public int HeapIndex { get; set; }

	public AStarNode() { }

	public int CompareTo( AStarNode other )
	{
		var compare = fCost.CompareTo( other.fCost );
		if ( compare == 0 )
			compare = hCost.CompareTo( other.hCost );
		return -compare;
	}

	public override int GetHashCode() => Current.GetHashCode();

	public static bool operator ==( AStarNode a, AStarNode b ) => a.Equals( b );
	public static bool operator !=( AStarNode a, AStarNode b ) => !a.Equals( b );

	public override bool Equals( object obj )
	{
		if ( obj is not AStarNode node ) return false;

		if ( node.Current != Current ) return false;
		if ( node.gCost != gCost ) return false;
		if ( node.hCost != hCost ) return false;
		if ( node.HeapIndex != HeapIndex ) return false;

		return true;
	}

	public bool Equals( AStarNode other )
	{
		if ( other.Current != Current ) return false;
		if ( other.gCost != gCost ) return false;
		if ( other.hCost != hCost ) return false;
		if ( other.HeapIndex != HeapIndex ) return false;

		return true;
	}
}
