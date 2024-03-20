using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ColPolC : CollisionPolygon2D
{
	public override void _Ready()
	{
		base._Ready();
		GetNode<Polygon2D>("Polygon2D").Polygon = Polygon;
	}
	
	public void update_pol(Vector2[] polygon_points){

		// Rounds every point to int values because if not triangulation issues may appear while clipping
		Vector2[] int_polygon_points = (Vector2[])polygon_points.Clone();
		for (int i = 0; i < polygon_points.Length; i++){
			int_polygon_points[i] = new Vector2 (  (float)Math.Round(polygon_points[i].X) ,  (float)Math.Round(polygon_points[i].Y) );
		}

		// Removes duplicate points
		Vector2[] double_polygon_points = (Vector2[])int_polygon_points.Distinct().ToArray().Clone();
		
		if (double_polygon_points.Length < 3){
			// if polygon has less than three point deletes it
			QueueFree();
		}
		else if (Geometry2D.DecomposePolygonInConvex(Polygon).Count() == 0){
			// If convex decomposition fails, use convex hull to kinda fix it
			// The error in Debugger is expected
			GD.Print("Error expected");
			SetDeferred("polygon", Geometry2D.ConvexHull(double_polygon_points));
		}
		else{
			// if everything is fine set the polygon
			SetDeferred("polygon", double_polygon_points);
		}
		
		GetNode<Polygon2D>("Polygon2D").Polygon = Polygon;
	}

    public void make_circle(int pol_count, float radius){
		// Makes a circle of Vector2 array
		Vector2[] surface_pols = new Vector2[pol_count];
		for (int i = 0; i < pol_count; i++) 
		{
			float angle = Mathf.DegToRad(i) * 360 / pol_count;
			Vector2 surface_pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			surface_pols[i] = surface_pos;
		}
		Polygon = surface_pols;
		GetNode<Polygon2D>("Polygon2D").Polygon = Polygon;
	}
	
}
