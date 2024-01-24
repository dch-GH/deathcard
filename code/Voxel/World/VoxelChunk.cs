namespace Deathcard;

public class VoxelChunk : Component, IEquatable<VoxelChunk>
{
	public Vector3S Position3D { get; set; }

	public ModelRenderer Renderer { get; private set; }
	public VoxelWorld Parent { get; private set; }
	public ModelCollider Collider { get; private set; }

	public PhysicsBody Body { get; private set;  }
	public PhysicsShape Shape { get; set; }

	public Model Model
	{
		get => model;
		set
		{
			model = value;

			if ( Renderer?.SceneObject == null )
				return;

			Renderer.SceneObject.Batchable = false;
			Parent.AssignAttributes( Renderer.SceneObject.Attributes );
			Collider.Model = model;
			Renderer.Model = model;
		}
	}

	private Model model;

	protected override void OnAwake()
	{
		Collider = Components.GetOrCreate<ModelCollider>();
		Renderer = Components.GetOrCreate<ModelRenderer>();
		Parent = Components.Get<VoxelWorld>( FindMode.InParent );
	}

	public bool Equals( VoxelChunk other )
	{
		return other.Position3D.Equals( Position3D );
	}

	public override bool Equals( object obj )
	{
		return obj is VoxelChunk other
			&& Equals( other );
	}

	public override int GetHashCode()
	{
		return Position3D.GetHashCode();
	}
}
