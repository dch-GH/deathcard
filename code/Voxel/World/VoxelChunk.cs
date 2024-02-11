namespace Deathcard;

public class VoxelChunk : Component, IEquatable<VoxelChunk>
{
	public Vector3S Position3D { get; set; }

	public ModelRenderer Renderer { get; private set; }
	public VoxelWorld Parent { get; private set; }
	public ModelCollider Collider { get; private set; }

	public PhysicsBody Body { get; private set;  }
	public PhysicsShape Shape { get; set; }

	protected override void OnAwake()
	{
		Collider = Components.GetOrCreate<ModelCollider>();
		Renderer = Components.GetOrCreate<ModelRenderer>();
		Parent = Components.Get<VoxelWorld>( FindMode.InParent );
	}

	public void Rebuild( Model model, bool keepPhysics = false )
	{
		if ( Renderer?.SceneObject == null )
			return;

		Renderer.SceneObject.Batchable = false;		

		Parent.AssignAttributes( Renderer.SceneObject.Attributes );
		Renderer.Model = model;

		if ( !keepPhysics )
			Collider.Model = model;
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
