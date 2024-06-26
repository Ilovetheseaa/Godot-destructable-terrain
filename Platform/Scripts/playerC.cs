using Godot;
using System;

public partial class playerC : CharacterBody2D
{


	[Export]
    public int Speed { get; set; } = 400;


	[Export]
	public int Rotate_Speed { get; set; } = 4;


	[Export]
    public int circle_res { get; set; } = 6;


	[Export]
	public float circle_radius { get; set; } = 10;


	[Export]
	public int min_movement_update { get; set; } = 5;

	[Export]
	public float tool_use_distance { get; set; } = 20;

	public Vector2 old_position = new Vector2();


	CollisionPolygon2D debug_pol;


	Area2D debug_detect;


	Node ground;

	Node2D tool;
	Node2D hand;
	Node2D raycast_target;
	RayCast2D raycast;
	Timer debugtimer;


	bool cam_motion_follow = true;
	Vector2 gravity_pull = new Vector2();
    public override void _Ready()
    {
        base._Ready();
		
		tool = GetNode<Node2D>("Tools");

		hand = tool.GetNode<Node2D>("Hand");
		hand.Position = new Vector2(tool_use_distance, 0);

		debug_pol = hand.GetNode("DigColArea").GetNode<CollisionPolygon2D>("CollisionPolygon2D");

		debug_detect = hand.GetNode<Area2D>("DigDetArea");

		debugtimer = tool.GetNode<Timer>("UseCooldown");

		raycast = tool.GetNode<RayCast2D>("Raycast");
		raycast.TargetPosition = hand.Position;

		raycast_target = tool.GetNode<Node2D>("RaycastTarget");
		raycast_target.Position = hand.Position;

		// makes the clip polygon
		debug_pol.Call("make_circle", circle_res, circle_radius);
		
    }

    public override void _PhysicsProcess(double delta)
    {
		GetTool();
		GetInput(delta);
		
        MoveAndSlide();
    }

	private float input_x;
	private float input_y;
	public void GetInput(double delta)
	{
		input_x = Input.GetAxis("Move Left", "Move Right");
		input_y = Input.GetAxis("Move Up", "Move Down");

		if (Input.GetActionStrength("Walk") == 1){
			Speed = 40;
		}
		else if (Input.GetActionStrength("Sprint") == 1){
			Speed = 800;
		}
		else{
			Speed = 400;
		}
		if (Input.GetActionStrength("Fire") == 1 && debugtimer.IsStopped() == true){
			carve();
			debugtimer.Start();
		}

		Velocity = ((Transform.X * input_x + Transform.Y * input_y) * Speed) + gravity_pull;


		if (Input.GetActionStrength("Change Camera Mode")  == 1){
			cam_motion_follow = !cam_motion_follow;
		}

		if (cam_motion_follow == true){
			Rotation += Input.GetAxis("Rotate +", "Rotate -") * (float)delta * Rotate_Speed;
		}
		
		
	}

	public void GetTool()
	{
		// Uses raycast to detect the polygons in the way
		tool.LookAt(GetGlobalMousePosition());

		if (GlobalPosition.DistanceTo(GetGlobalMousePosition()) < tool_use_distance){
			hand.GlobalPosition = GetGlobalMousePosition();
		}
		else if (raycast.IsColliding()){
			hand.GlobalPosition = raycast.GetCollisionPoint();
		}

	}
	public void carve(){
		if (old_position.DistanceTo(hand.GlobalPosition) > min_movement_update && ground != null){
			ground.CallDeferred("clip", debug_pol.Polygon, hand.GlobalPosition, circle_radius);
			old_position = hand.GlobalPosition;
		}
	}

	public void _on_area_2d_body_entered(Node body)
	{
		if (body.GetParent().IsInGroup("Planet")){
			ground = body.GetParent();
			
		}
	}

	public void _on_area_2d_body_exited(Node body)
	{
		if (body.GetParent().IsInGroup("Planet")){
			ground = null;
		}
	}
}
