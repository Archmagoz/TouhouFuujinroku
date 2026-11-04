using Godot;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	/// <summary>
	/// Reimu Hakurei. Provides her own sprite, weapon, and hitbox and implements the presentation
	/// hooks of <see cref="PlayableCharacter"/> with them.
	/// </summary>
	public partial class Reimu : PlayableCharacter
	{
		/// <summary>Sprite that reflects movement direction and plays the death animation.</summary>
		[ExportGroup("Reimu")]
		[Export] private ReimuAnimatedSprite _sprite;

		/// <summary>Weapon fired by the shoot input and switched between normal and focus modes.</summary>
		[Export] private ReimuWeapon _weapon;

		/// <summary>Collision shape of the player, drawn only while focus is held so the hit area is visible.</summary>
		[Export] private ReimuHitbox _hitbox;

		protected override void OnDeathPresentation()
		{
			_sprite.SetVisible(false);
			_weapon.SetVisible(false);
			_hitbox.HideImmediately();
		}

		protected override void OnRespawnPresentation()
		{
			_sprite.SetVisible(true);
			_weapon.SetVisible(true);
			// Clears the death pose now instead of waiting a frame for the input-driven update.
			_sprite.UpdateSprite(0f);
			_weapon.SnapFormation();
		}

		protected override void UpdatePresentation(float horizontalInput) =>
			_sprite.UpdateSprite(horizontalInput);

		protected override void ApplyFocus(bool focused, double delta)
		{
			_weapon.ToggleFocusMode(focused, delta);
			_hitbox.SetFocused(focused);
		}

		protected override void Fire() =>
			_weapon.TryFire();

		protected override void ApplyTint(Color tint)
		{
			_sprite.Modulate = tint;
			_weapon.Modulate = tint;
		}
	}
}