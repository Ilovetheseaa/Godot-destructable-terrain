using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlanetC : Node
{
	[Export]
	int radius { get; set; } = 100;
	[Export]
	int pol_count { get; set; } = 8;

	[Export]
	float tile_row_count { get; set; } = 4;

	float distance_of_tile;

	PackedScene collision_poly ;
	StaticBody2D static_body ;

	Vector2[] quadrant_poly;


	public override void _Ready()
	{
		collision_poly = GD.Load<PackedScene>("res://Scenes/ColPol.tscn");
		static_body = GetNode<StaticBody2D>("MainSurface");

		// Make a square polygon
		quadrant_poly = new Vector2[] {new Vector2 (radius*2 / tile_row_count, radius*2 / tile_row_count),
		 new Vector2 (radius*2 / tile_row_count, -radius*2 / tile_row_count),
		 new Vector2 (-radius*2 / tile_row_count, -radius*2 / tile_row_count),
		 new Vector2 (-radius*2 / tile_row_count, radius*2 / tile_row_count)};
		distance_of_tile = radius*2 * Mathf.Sqrt(2) / tile_row_count ;
		generate_tilemap();
	}
	public void generate_tilemap()
	{
		// Make a single circle polygon

		Vector2[] surface_pols = new Vector2[pol_count];
		for (int i = 0; i < pol_count; i++) 
		{
			float angle = Mathf.DegToRad(i) * 360 / pol_count;
			Vector2 surface_pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			surface_pols[i] = surface_pos;
		}


		// Makes a tilemap from square polygons and intersects the circle polygon with it

		float d = radius * 2 / tile_row_count;
		for (int i = 0; i < tile_row_count; i++)
		{
			for (int j = 0; j < tile_row_count; j++)
			{
				Vector2[] tilemap = new Vector2[] {new Vector2(i * d - radius , j * d - radius),
				new Vector2((i + 1) * d - radius, j * d - radius),
				new Vector2(d * (i + 1) - radius, d * (j + 1) - radius),
				new Vector2(d * i - radius, d * (j + 1) - radius)};

				Godot.Collections.Array<Vector2[]> intersect = Geometry2D.IntersectPolygons(tilemap, surface_pols);
				if (intersect.Any()){
					static_body.AddChild(_new_colpol(intersect[0]));
				}
			}
		}


	}
	
	public void clip(Vector2[] poly, Vector2 pos, float affect_radius)
	{

		// offsets the polygon
		var offset_poly = new Transform2D(0, pos) * poly;

		List<CollisionPolygon2D> affected_pols = new List<CollisionPolygon2D>() ;
		foreach (CollisionPolygon2D all_pols in static_body.GetChildren()){
			var d = all_pols.Get("avg_position");
			if (d.GetType() != null && d.AsVector2().DistanceTo(pos) < distance_of_tile + affect_radius){
				affected_pols.Add(all_pols);
			}
		}

		foreach (CollisionPolygon2D affected_poly in affected_pols){
			// If the polygon has less than three point delete the polygon
			if (affected_poly.Polygon.Length < 3){
				affected_poly.Free();
			}
		
			Godot.Collections.Array<Vector2[]> des = Geometry2D.ClipPolygons(affected_poly.Polygon, offset_poly);


			if ( des.Count() == 0){
				// poly overlaps the colpol
				affected_poly.QueueFree();
			}
			else if (des.Count() == 1){
				// produces 1 polygon
				if(des[0] != affected_poly.Polygon){
					affected_poly.Call("update_pol", des[0]);
				}
				
			}
			else if (des.Count() == 2){
				// Cheks if affected_poly has a hole (is clockwise) if so splits the polygon in half
				if (Geometry2D.IsPolygonClockwise(des[0]) || Geometry2D.IsPolygonClockwise(des[1])){
					foreach (var p in _split_poly(poly)){
						var offset_p = new Transform2D(0, pos) * p;
						var new_pol = Geometry2D.IntersectPolygons( offset_p , affected_poly.Polygon);
						static_body.CallDeferred("add_child", _new_colpol(new_pol[0]));						
					}
					affected_poly.QueueFree();
					
				}

				else{
					// adds the new polygon to body
					affected_poly.Call("update_pol", des[0]);
					for  (int j = 0; j < des.Count()-1; j++){
						if (des[j + 1].IsEmpty() == false){
							static_body.CallDeferred("add_child", _new_colpol(des[j + 1]));
						}
					}
				}
			}
			else {
				// if more than 2 polygons made, adds them to body
				affected_poly.Call("update_pol", des[0]);
				for  (int j = 0; j < des.Count()-1; j++){
					if (des[j + 1].IsEmpty() == false){
						static_body.CallDeferred("add_child", _new_colpol(des[j + 1]));
					}
				}
			}
		}
	}

	Node _new_colpol(Vector2[] polygon){
		// makes a new poly
		var colpol = collision_poly.Instantiate<CollisionPolygon2D>();
		colpol.Polygon = polygon;
		colpol.Call("set_avg_position", _avg_position(polygon));
		return colpol;
	}

	Godot.Collections.Array<Vector2[]> _split_poly(Vector2[] hole_pol){

		var avg_x = _avg_position(hole_pol).X;

		Vector2[] right_subquadrant = (Vector2[])quadrant_poly.Clone();
		right_subquadrant[3] = new Vector2(avg_x, right_subquadrant[3].Y);
		right_subquadrant[2] = new Vector2(avg_x, right_subquadrant[2].Y);

		Vector2[] left_subquadrant = (Vector2[])quadrant_poly.Clone();
		left_subquadrant[0] = new Vector2(avg_x, left_subquadrant[0].Y);
		left_subquadrant[1] = new Vector2(avg_x, left_subquadrant[1].Y);

		Vector2[] pol1 = Geometry2D.ClipPolygons(left_subquadrant, hole_pol)[0];
		Vector2[] pol2 = Geometry2D.ClipPolygons(right_subquadrant, hole_pol)[0];
		Godot.Collections.Array<Vector2[]> pol_1_2 = new Godot.Collections.Array<Vector2[]> {pol1, pol2};
		return  pol_1_2;
	}

	Vector2 _avg_position(Vector2[] array){
		Vector2 sum = new Vector2();

		for (int i = 0; i < array.Length; i++){
			sum += array[i];
		}

		Vector2 value = sum / array.Length ;
		return value;
	}
}
