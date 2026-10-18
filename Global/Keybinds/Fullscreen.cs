using Godot;

namespace Global
{
	/// <summary>
	/// Toggles the game window between fullscreen and windowed mode when the
	/// <c>toggle_fullscreen</c> input action is pressed.
	/// <para>
	/// Intended to live in a scene that is always loaded (e.g. as an Autoload), so the shortcut works
	/// from every screen. It requires the <c>toggle_fullscreen</c> action to be defined in the Input Map.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class Fullscreen : Node
	{
		/// <summary>
		/// Listens for the toggle action and switches the window mode. Any non-fullscreen mode
		/// (windowed, maximized, minimized) switches to fullscreen; fullscreen returns to windowed.
		/// </summary>
		/// <param name="event">Input event dispatched by the engine.</param>
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