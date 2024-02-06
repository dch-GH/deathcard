namespace Deathcard.Editor;

class VoxelWorldEditor : EditorTool<VoxelWorld>
{
	private static VoxelWorldEditor instance;
	public static VoxelWorld Selected { get; set; }
	public static bool Open
	{
		get => windowOpen;
		set
		{
			windowOpen = value;
			window?.Close();

			if ( value )
				instance.CreateWindow();
		}
	}

	private static bool windowOpen;
	private static WidgetWindow window;

	public VoxelWorldEditor() : base()
	{
		instance = this;
	}

	void CreateWindow()
	{
		window = new WidgetWindow( SceneOverlay, "VoxelWorld Tools" );
		window.MinimumWidth = 400;
		window.MinimumHeight = SceneOverlay.Height;

		window.Layout = Layout.Row();
		AddOverlay( window, TextFlag.RightCenter, 0 );
	}

	public override void OnSelectionChanged()
	{
		base.OnSelectionChanged();

		Selected = GetSelectedComponent<VoxelWorld>();
	}

	// Gizmo stuff.
	public override void OnUpdate()
	{
		if ( Selected == null )
			return;

		// Focus on hovered VoxelWorld.
		var tr = Selected.Trace( Gizmo.CurrentRay, 50000f );
		if ( !tr.Hit )
			return;

		using ( Gizmo.Scope( "VoxelWorld", Selected.Transform.World ) )
		{
			// Debug
			var center = (Vector3)tr.GlobalPosition * Selected.VoxelScale + Selected.VoxelScale / 2f;
			var bbox = new BBox( center - Selected.VoxelScale / 2f, center + Selected.VoxelScale / 2f );
			/*var surface = center + tr.Normal * VoxelScale / 2f;

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.LineThickness = 5;
			Gizmo.Draw.Line( surface, surface + tr.Normal * 50f );*/

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.ScreenText( $"{(tr.Voxel?.GetType().Name ?? "unknown")}", 20, "Consolas", 18, TextFlag.LeftTop );
			Gizmo.Draw.ScreenText( $"XYZ: {tr.GlobalPosition}", 20 + Vector2.Up * 20, "Consolas", 18, TextFlag.LeftTop );
			Gizmo.Draw.ScreenText( $"Chunk: {tr.Chunk?.Position}", 20 + Vector2.Up * 40, "Consolas", 18, TextFlag.LeftTop );

			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.LineThickness = 1;
			Gizmo.Draw.LineBBox( bbox );

			Gizmo.Draw.Color = Color.Black.WithAlpha( 0.5f );
			Gizmo.Draw.SolidBox( bbox );
		}
	}
}
