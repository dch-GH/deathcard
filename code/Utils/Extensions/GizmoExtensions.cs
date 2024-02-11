namespace Deathcard;

public static class GizmoDrawExtensions
{
	public static void Plane( this Gizmo.GizmoDraw self, Vector3 pos, Vector3 normal, float size = 10f )
	{
		// Normal line
		var color = Gizmo.Draw.Color;
		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.Line( pos, pos + normal * size / 2f );
		Gizmo.Draw.Color = color;

		// 2 perpendicular vectors
		var side1 = Vector3.Cross( normal, Vector3.Up ).Normal;
		var side2 = Vector3.Cross( normal, side1 ).Normal;

		// Calculate the vertices of the plane
		var v1 = pos - side1 * size + side2 * size;
		var v2 = pos + side1 * size + side2 * size;
		var v3 = pos + side1 * size - side2 * size;
		var v4 = pos - side1 * size - side2 * size;

		// Connect vertices by lines
		Gizmo.Draw.Line( v1, v2 );
		Gizmo.Draw.Line( v2, v3 );
		Gizmo.Draw.Line( v3, v4 );
		Gizmo.Draw.Line( v4, v1 );
	}
}
