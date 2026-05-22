using Godot;

namespace Global
{
	[GlobalClass]
	public partial class Fullscreen : Node
	{
		public override void _Input(InputEvent @event)
		{
			if (@event.IsActionPressed("toggle_fullscreen"))
			{
				var mode = DisplayServer.WindowGetMode();

				if (mode == DisplayServer.WindowMode.Fullscreen)
					DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
				else
					DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			}
		}
	}
}