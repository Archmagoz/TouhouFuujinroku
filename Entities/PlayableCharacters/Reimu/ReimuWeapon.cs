using Godot;
using Godot.Collections;

namespace TohouFuuujinoku.Entities.PlayableCharacters
{
	public partial class ReimuWeapon : Node2D
	{
		[Export] private Array<Sprite2D> _sprites;

		// Offset applied while in focus mode.
		[Export] private float _focusOffset = 8f;

		private bool _isFocusMode;

		public void SetFocusMode(bool isFocused)
		{
			_isFocusMode = isFocused;

			for (var i = 0; i < _sprites.Count; i++)
			{
				var sprite = _sprites[i];

				sprite.Visible = _isFocusMode;

				// Reset local position.
				var position = Vector2.Zero;

				if (_isFocusMode)
				{
					switch (i)
					{
						// Top-left orb.
						case 0:
							position.X -= _focusOffset;
							position.Y -= _focusOffset;
							break;

						// Top-right orb.
						case 1:
							position.X += _focusOffset;
							position.Y -= _focusOffset;
							break;

						// Bottom-left orb.
						case 2:
							position.X -= _focusOffset;
							position.Y += _focusOffset;
							break;

						// Bottom-right orb.
						case 3:
							position.X += _focusOffset;
							position.Y += _focusOffset;
							break;
					}
				}

				sprite.Position = position;
			}
		}
	}
}