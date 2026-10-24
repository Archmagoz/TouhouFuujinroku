using Godot;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	/// <summary>
	/// The player's collision shape, able to draw itself. While focus mode is active the shape is drawn over
	/// the character, so the player can see exactly which area gets hit, as in the classic shoot 'em ups:
	/// a white dot with a bold red border. It fades in when focus starts and fades out when it ends.
	/// <para>
	/// The white core is the real collision shape, and the red border is drawn outside it, so the core
	/// is always what actually hurts. The styled look needs a <see cref="CircleShape2D"/>; any other shape
	/// falls back to a plain fill with the core color.
	/// </para>
	/// <para>
	/// It never reads input. <see cref="Reimu"/> owns the focus state and pushes it through
	/// <see cref="SetFocused"/>, which is safe to call every frame. Hiding the node only affects drawing:
	/// the collision shape itself is never disabled.
	/// </para>
	/// </summary>
	public partial class ReimuHitbox : CollisionShape2D
	{
		#region Fields

		/// <summary>
		/// Color of the core, which matches the real collision shape.
		/// </summary>
		[ExportGroup("Appearance")]
		[Export] private Color _fillColor = Colors.White;

		/// <summary>
		/// Color of the border drawn around the core.
		/// </summary>
		[Export] private Color _borderColor = new(1f, 0.1f, 0.1f);

		/// <summary>
		/// Thickness of the border in pixels, measured outward from the collision shape's edge.
		/// </summary>
		[Export(PropertyHint.Range, "0,8,0.25")]
		private float _borderThickness = 2f;

		/// <summary>
		/// Seconds to fade in and out when focus starts or ends.
		/// </summary>
		[ExportGroup("Animation")]
		[Export] private float _fadeDuration = 0.1f;

		/// <summary>
		/// Whether focus mode is active, meaning the shape is shown or fading in.
		/// </summary>
		private bool _focused;

		/// <summary>
		/// Current fade animation, retained so a new one can take over from the current opacity.
		/// </summary>
		private Tween _tween;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			HideImmediately();
		}

		public override void _Draw()
		{
			// Always draws: visibility and opacity are controlled by the fade, not by the focus flag,
			// so the shape stays on screen while it fades out.
			if (Shape == null) return;

			if (Shape is CircleShape2D circle)
				DrawCircleHitbox(circle.Radius);
			else
				Shape.Draw(GetCanvasItem(), _fillColor);
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Fades the drawn shape in or out. Does nothing if the state is unchanged, so it can be called
		/// every frame. An interrupted fade continues from the current opacity instead of jumping.
		/// </summary>
		/// <param name="focused">True while focus mode is held.</param>
		public void SetFocused(bool focused)
		{
			if (focused == _focused) return;

			_focused = focused;
			_tween?.Kill();
			_tween = CreateTween();

			if (focused)
			{
				Visible = true;
				_tween.TweenProperty(this, "modulate:a", 1f, _fadeDuration);
			}
			else
			{
				_tween.TweenProperty(this, "modulate:a", 0f, _fadeDuration);
				_tween.TweenCallback(Callable.From(Conceal));
			}
		}

		/// <summary>
		/// Hides the shape instantly, without a fade, and clears the focus state.
		/// Used when the player dies, since the rest of the character disappears at once too.
		/// </summary>
		public void HideImmediately()
		{
			_tween?.Kill();
			_focused = false;
			Modulate = new Color(Modulate, 0f);
			Conceal();
		}

		#endregion // Public API

		#region Helpers

		/// <summary>
		/// Stops drawing once the shape is fully transparent.
		/// </summary>
		private void Conceal()
		{
			Visible = false;
		}

		/// <summary>
		/// Draws the dot: a larger border-colored disc first, then the core on top, so only a ring of
		/// <see cref="_borderThickness"/> pixels remains visible around the core.
		/// </summary>
		/// <param name="radius">Radius of the collision circle, which becomes the radius of the core.</param>
		private void DrawCircleHitbox(float radius)
		{
			DrawCircle(Vector2.Zero, radius + _borderThickness, _borderColor);
			DrawCircle(Vector2.Zero, radius, _fillColor);
		}

		#endregion // Helpers
	}
}