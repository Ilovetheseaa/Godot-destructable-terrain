using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GravityContol : Node2D
{
	List<CharacterBody2D> gravity_objects = new List<CharacterBody2D>();

	[Export]
	float gravity_power;
	int gravity_radius;
	CollisionShape2D gravity_col;
	public override void _Ready()
	{
		gravity_col = GetNode("GravityField").GetNode<CollisionShape2D>("GravityCol");
		gravity_radius = ((int)GetParent().Get("radius")) * 2;
		gravity_col.Shape.Set("radius", gravity_radius);
		gravity_power = gravity_radius * 2;
	}

	
	public override void _PhysicsProcess(double delta)
	{
		if (gravity_objects.Count > 0){
			foreach (CharacterBody2D obj in gravity_objects){
				obj.Set("gravity_pull", obj.GlobalPosition.DirectionTo(GlobalPosition) * Mathf.Round(gravity_power / ((int)Math.Round(obj.GlobalPosition.DistanceTo(GlobalPosition)/(gravity_radius/10)) + 1)));
				
				if ((bool)obj.Get("cam_motion_follow") == false){
					obj.Rotation = GetAngleTo(obj.GlobalPosition) + Mathf.Pi/2;
				}
			}
		}
		
	}
	
	public void _on_area_2d_body_entered(CharacterBody2D body){
		gravity_objects.Add(body);
		body.Set("cam_motion_follow", false);
	}
	public void _on_area_2d_body_exited(CharacterBody2D body){
		if (gravity_objects.Contains(body)){
			body.Set("gravity_pull", 0);
			body.Set("cam_motion_follow", true);
			gravity_objects.Remove(body);
		}
		
	}
}
