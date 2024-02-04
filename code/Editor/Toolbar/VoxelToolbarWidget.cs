namespace Deathcard.Editor;

public class VoxelToolbarWidget : ToolbarGroup
{
	public VoxelToolbarWidget( Widget parent ) : base( parent, "view_in_ar" )
	{
		ToolTip = "Voxel Editor";
	}

	public override void Build()
	{
		var gizmo = EditorScene.GizmoInstance;
		AddToggleButton( "Editor", "settings", () => VoxelWorldEditor.Open, ( v ) => VoxelWorldEditor.Open = v );
		AddToggleButton( "Display Chunks", "check_box_outline_blank", () => VoxelWorld.ChunkGizmo, ( v ) => VoxelWorld.ChunkGizmo = v );
	}

	[Event( "tools.headerbar.build", Priority = 200 )]
	public static void OnBuildHeaderToolbar( HeadBarEvent e )
	{
		e.RightCenter.AddSpacingCell( 8f );
		e.RightCenter.Add( new VoxelToolbarWidget( null ) );
	}
}
