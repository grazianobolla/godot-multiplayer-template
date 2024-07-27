using Godot;
using System.Collections.Generic;
using ImGuiNET;
using System.Linq;
using System.Runtime.InteropServices;
using System.Data;

public partial class ServerPlayer : CharacterBody3D
{
	public int MultiplayerID { get; set; } = 0;
	public int InstantLatency { get; set; } = 0;
	public float LateralLookAngle { get; set; } = 0;

	private Dictionary<int, NetMessage.UserInput> _pendingInputs = new();
	private int _skippedTicks = 0;
	private int _inputQueueSize = 0;


#nullable enable
	private NetMessage.UserInput? _lastInputProcessed = null;
#nullable disable

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
	}

	public void ProcessPendingCommands(int currentTick)
	{
		if (_pendingInputs.TryGetValue(currentTick, out NetMessage.UserInput input))
		{
			AdvancePhysics(input);
			_lastInputProcessed = input;

			_pendingInputs = _pendingInputs.Where(pair => pair.Key > currentTick)
			.ToDictionary(pair => pair.Key, pair => pair.Value);
			/* TODO: Using dictionaries for this is probably the worst and most unefficient
				way of queueing non-duplicated inputs, this must be changed in the future. */

			_inputQueueSize = _pendingInputs.Count;
		}
		else if (_lastInputProcessed.HasValue)
		{
			AdvancePhysics((NetMessage.UserInput)_lastInputProcessed);
			_skippedTicks++;
		}
	}

	public void PushCommand(NetMessage.UserCommand command)
	{
		int offset = command.Inputs.Length - 1;

		foreach (var input in command.Inputs)
		{
			int tick = command.Tick - offset;

			if (!_pendingInputs.ContainsKey(tick))
			{
				_pendingInputs.Add(tick, input);
			}

			offset--;
		}
	}

	private void AdvancePhysics(NetMessage.UserInput input)
	{
		this.Velocity = MovementCalculator.ComputeVelocity(this, input);
		this.LateralLookAngle = input.LateralLookAngle;
		MoveAndSlide();
	}

	public NetMessage.EntityState GetCurrentState()
	{
		return new NetMessage.EntityState
		{
			Id = MultiplayerID,
			PosArray = [this.Position.X, this.Position.Y, this.Position.Z],
			VelArray = [this.Velocity.X, this.Velocity.Y, this.Velocity.Z],
			LateralLookAngle = this.LateralLookAngle
		};
	}

	private void DisplayDebugInformation()
	{
		ImGui.Begin($"Server Player {MultiplayerID}");
		ImGui.Text($"Instant Latency {InstantLatency}");
		ImGui.Text($"Input Queue Count {_inputQueueSize}");
		ImGui.Text($"Missed Frames {_skippedTicks}");
		ImGui.End();
	}
}
