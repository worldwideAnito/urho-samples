﻿using System;

namespace Urho.Samples
{
	public class RoboMan : Component
	{
		// Grounded flag for movement.
		bool onGround;
		// Jump flag.
		bool okToJump;
		// In air timer. Due to possible physics inaccuracy, character can be off ground for max. 1/10 second and still be allowed to move.
		float inAirTimer;
		RigidBody body;
		AnimationController animCtrl;

		const float MoveForce = 1.4f;
		const float InairMoveForce = 0.02f;
		const float BrakeForce = 0.2f;
		const float JumpForce = 7.0f;
		const float InairThresholdTime = 0.1f;
		
		// Movement controls. Assigned by the main program each frame.
		public Controls Controls { get; set; } = new Controls();

		public RoboMan(Context context) : base(context)
		{
			okToJump = true;
		}

		public void Start()
		{
			// Component has been inserted into its scene node. Subscribe to events now
			SubscribeToNodeCollision(HandleNodeCollision);
		}

		public void FixedUpdate(float timeStep)
		{
			animCtrl = animCtrl ?? GetComponent<AnimationController>();
			body = body ?? GetComponent<RigidBody>();

			// Update the in air timer. Reset if grounded
			if (!onGround)
				inAirTimer += timeStep;
			else
				inAirTimer = 0.0f;
			// When character has been in air less than 1/10 second, it's still interpreted as being on ground
			bool softGrounded = inAirTimer < InairThresholdTime;

			// Update movement & animation
			var rot = Node.Rotation;
			Vector3 moveDir = Vector3.Zero;
			var velocity = body.LinearVelocity;
			// Velocity on the XZ plane
			Vector3 planeVelocity = new Vector3(velocity.X, 0.0f, velocity.Z);

			if (Controls.IsDown(_41_ToonTown.CtrlForward))
				moveDir += Vector3.UnitZ;
			if (Controls.IsDown(_41_ToonTown.CtrlBack))
				moveDir += new Vector3(0f, 0f, -1f);
			if (Controls.IsDown(_41_ToonTown.CtrlLeft))
				moveDir += new Vector3(-1f, 0f, 0f);
			if (Controls.IsDown(_41_ToonTown.CtrlRight))
				moveDir += Vector3.UnitX;

			// Normalize move vector so that diagonal strafing is not faster
			if (moveDir.LengthSquared > 0.0f)
				moveDir.Normalize();

			// If in air, allow control, but slower than when on ground
			body.ApplyImpulse(rot * moveDir * (softGrounded ? MoveForce : InairMoveForce));

			if (softGrounded)
			{
				// When on ground, apply a braking force to limit maximum ground velocity
				Vector3 brakeForce = -planeVelocity * BrakeForce;
				body.ApplyImpulse(brakeForce);

				// Jump. Must release jump control inbetween jumps
				if (Controls.IsDown(_41_ToonTown.CtrlJump))
				{
					if (okToJump)
					{
						body.ApplyImpulse(Vector3.UnitY * JumpForce);
						okToJump = false;
					}
				}
				else
					okToJump = true;
			}

			// Play walk animation if moving on ground, otherwise fade it out
			if (softGrounded && !moveDir.Equals(Vector3.Zero))
				animCtrl.PlayExclusive("Models/Jack_Walk.ani", 0, true, 0.2f);
			else
				animCtrl.Stop("Models/Jack_Walk.ani", 0.2f);
			// Set walk animation speed proportional to velocity
			animCtrl.SetSpeed("Models/Jack_Walk.ani", planeVelocity.Length * 0.3f);

			// Reset grounded flag for next frame
			onGround = false;
		}

		void HandleNodeCollision(NodeCollisionEventArgs args)
		{
			if (args.Body != body)
				return;

			foreach (var contact in args.Contacts)
			{
				// If contact is below node center and mostly vertical, assume it's a ground contact
				if (contact.ContactPosition.Y < (Node.Position.Y + 1.0f))
				{
					float level = Math.Abs(contact.ContactNormal.Y);
					if (level > 0.75)
						onGround = true;
				}
			}
		}
	}
}
