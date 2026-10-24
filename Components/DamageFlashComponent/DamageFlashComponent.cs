using Godot;

namespace TouhouFuujinroku.Components
{
    /// <summary>
    /// Plays a quick solid-color flash (white by default) on a visual node, as feedback for damage taken.
    /// <para>
    /// The component only handles the visual effect. It knows nothing about health: the owner decides when
    /// a hit deserves feedback and calls <see cref="Flash"/>. Calling it again while a flash is still playing
    /// restarts it, so every hit produces a visible blink.
    /// </para>
    /// <para>
    /// The effect is a small canvas item shader that blends the target's pixels toward the flash color
    /// while preserving their alpha, so transparent areas of the sprite stay transparent. A dedicated
    /// <see cref="ShaderMaterial"/> is created per component and assigned to the target on <c>_Ready</c>,
    /// replacing any material the target had.
    /// </para>
    /// </summary>
    [GlobalClass]
    public partial class DamageFlashComponent : Node
    {
        #region Fields

        /// <summary>
        /// Visual node that blinks. Typically the enemy's <see cref="AnimatedSprite2D"/>.
        /// </summary>
        [ExportGroup("Target")]
        [Export] private CanvasItem _target;

        /// <summary>
        /// Color the target blends toward at the peak of the flash. Its alpha channel is ignored.
        /// </summary>
        [ExportGroup("Flash")]
        [Export] private Color _flashColor = Colors.White;

        /// <summary>
        /// Seconds the flash takes to fade back to the normal look. Keep it short so rapid hits stay readable.
        /// </summary>
        [Export(PropertyHint.Range, "0.02,0.5,0.01")]
        private float _duration = 0.08f;

        /// <summary>
        /// Shader source: blends the sprite color toward <c>flash_color</c> by <c>flash_amount</c>
        /// (0 = untouched, 1 = solid color) without touching alpha.
        /// </summary>
        private const string ShaderCode = @"
                                            shader_type canvas_item;

                                            uniform vec4 flash_color : source_color = vec4(1.0);
                                            uniform float flash_amount : hint_range(0.0, 1.0) = 0.0;

                                            void fragment() {
                                                COLOR.rgb = mix(COLOR.rgb, flash_color.rgb, flash_amount);
                                            }";

        /// <summary>
        /// Name of the shader uniform animated by the flash, as a property path for tweening.
        /// </summary>
        private const string FlashAmountPath = "shader_parameter/flash_amount";

        /// <summary>
        /// Compiled shader shared by every component. Each component still owns its own material,
        /// so flashes on different enemies never affect each other.
        /// </summary>
        private static Shader _sharedShader;

        /// <summary>
        /// Material owned by this component and assigned to the target.
        /// </summary>
        private ShaderMaterial _material;

        /// <summary>
        /// Current flash animation, retained so a new flash can restart it cleanly.
        /// </summary>
        private Tween _tween;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            if (_target == null) return;

            _sharedShader ??= new Shader { Code = ShaderCode };

            _material = new ShaderMaterial { Shader = _sharedShader };
            _material.SetShaderParameter("flash_color", _flashColor);
            _material.SetShaderParameter("flash_amount", 0f);

            _target.Material = _material;
        }

        #endregion // Godot overrides

        #region Public API

        /// <summary>
        /// Plays the flash. It starts at full intensity immediately and fades out over the configured duration.
        /// An existing flash is restarted from full intensity.
        /// </summary>
        public void Flash()
        {
            if (_material == null) return;

            _tween?.Kill();

            _material.SetShaderParameter("flash_amount", 1f);

            // Bound to this node, so the tween stops automatically when the component is freed.
            _tween = CreateTween();
            _tween.TweenProperty(_material, FlashAmountPath, 0f, _duration);
        }

        #endregion // Public API
    }
}