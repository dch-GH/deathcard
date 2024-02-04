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

	public override void OnUpdate()
	{
		if ( !Selection.OfType<GameObject>().Any() ) return;
	}
}
