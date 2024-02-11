namespace Deathcard.Editor;

public enum VoxelTool
{
	Sphere,
	Rectangle,
	Line
}

public enum ToolMode
{
	Place,
	Paint,
	Erase
}

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
			if ( window == null )
				(instance ?? new()).CreateWindow();

			window.Visible = value;
		}
	}

	private static bool windowOpen;
	private static WidgetWindow window;

	private EnumProperty<VoxelTool> tool;
	private EnumProperty<ToolMode> mode;
	private AtlasItem selected;

	struct AtlasItem
	{
		public Guid Guid;
		public SerializedTexture Data;
		public Pixmap Icon;
	}

	public VoxelWorldEditor() : base()
	{
		instance = this;
	}

	void CreateWindow()
	{
		window = new WidgetWindow( SceneOverlay, "VoxelWorld Tools" );
		window.MinimumWidth = 400;
		window.MinimumHeight = SceneOverlay.Height;

		var layout = Layout.Column();
		layout.Margin = 8f;
		layout.Alignment = TextFlag.Top;

		if ( Selected == null )
		{
			layout.Add( new global::Editor.Label( "WARNING!: You need to select any VoxelWorld.", window )
			{
				Color = Theme.Red,
				Position = new Vector2( 10, 30 ),
			} );

			window.Layout = layout;
			AddOverlay( window, TextFlag.RightCenter, 0 );

			return;
		}

		// Tools
		tool = layout.Add( new EnumProperty<VoxelTool>( window ) );
		layout.AddSpacingCell( 8f );
		mode = layout.Add( new EnumProperty<ToolMode>( window ) );
		layout.AddSpacingCell( 8f );

		// TextureAtlas
		layout.Add( new global::Editor.Label( "Textures", window ) );

		var atlas = Selected.Atlas;
		var list = layout.Add( new ListView( window ) 
		{
			ItemSize = 60, 
			ItemSpacing = 10,
			ItemAlign = Align.FlexStart
		} );

		list.ItemPaint = ( widget ) =>
		{
			var item = (AtlasItem)widget.Object;
			Paint.SetFont( "Consolas", 8 );

			Paint.Antialiasing = true;
			Paint.TextAntialiasing = true;

			Paint.BilinearFiltering = false;
			Paint.Draw( widget.Rect, item.Icon );

			if ( item.Guid == selected.Guid )
			{
				Paint.SetBrush( Theme.Blue.WithAlpha( 0.30f ) );
				Paint.SetPen( Theme.Blue.WithAlpha( 0.90f ) );
				Paint.DrawRect( widget.Rect.Grow( 0 ) );
			}

			var text = $"{item.Data.Name}";
			var rect = Paint.MeasureText( widget.Rect, text, TextFlag.Left );

			Paint.ClearPen();
			Paint.SetBrush( Color.Black.WithAlpha( 0.8f ) );
			Paint.DrawRect( widget.Rect.Shrink( 0f, widget.Rect.Height - 20, 0f, 0f ) );
			Paint.SetPen( Theme.White );

			var pos = rect.Position + Vector2.Up * (widget.Rect.Height - 20);
			Paint.DrawText( new Rect( pos, widget.Rect.Size.WithY( 20 ) ), text );
		};

		list.ItemSelected = ( oitem ) =>
		{
			if ( oitem is not AtlasItem item )
				return;

			selected = item;
		};

		var items = atlas.Items
			.Select( x => (object)new AtlasItem()
			{
				Data = x,
				Icon = AssetSystem.FindByPath( x.Albedo ).GetAssetThumb(),
				Guid = Guid.NewGuid()
			} );

		list.SetItems( items );
		list.Update();

		// Assign layout to window.
		window.Layout = layout;
	}

	public override void OnSelectionChanged()
	{
		base.OnSelectionChanged();

		Selected = GetSelectedComponent<VoxelWorld>();
		if ( Open )
		{
			window?.Close();
			CreateWindow();
		}
	}

	// Gizmo stuff.
	public override void OnUpdate()
	{
		if ( Selected == null )
		{
			Selected = VoxelWorld.All.FirstOrDefault();
			return;
		}

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
