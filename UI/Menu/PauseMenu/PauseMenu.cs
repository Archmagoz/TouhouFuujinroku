using Godot;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI.Menu.PauseMenu
{
    /// <summary>
    /// Pause screen shown on top of a level. Listens for the pause action, toggles the tree pause state and its
    /// own visibility, and resumes the game through either the same action or the resume button.
    /// <para>
    /// The menu is instantiated hidden together with the level. It runs with <see cref="Node.ProcessModeEnum.Always"/>
    /// so it keeps receiving input while the <see cref="SceneTree"/> is paused, which is required to unpause.
    /// </para>
    /// <para>
    /// The resume button's <c>Pressed</c> signal must be connected in the editor to <see cref="OnResumePressed"/>.
    /// </para>
    /// </summary>
    public partial class PauseMenu : MenuScreen
    {
        #region Constants

        /// <summary>Name of the Input Map action that toggles the pause menu.</summary>
        private static readonly StringName PauseAction = "pause";

        #endregion // Constants

        #region Fields

        /// <summary>
        /// Button that receives focus when the menu opens, so keyboard and gamepad navigation work immediately.
        /// </summary>
        [Export] private TextureButton _resumeButton;

        #endregion // Fields

        #region Properties

        /// <summary>Whether the pause menu is currently open and the game is paused by it.</summary>
        public bool IsOpen => Visible;

        #endregion // Properties

        #region Godot overrides

        public override void _Ready()
        {
            base._Ready();

            // Must keep processing while the tree is paused, otherwise it could never unpause the game.
            ProcessMode = ProcessModeEnum.Always;
            Visible = false;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!@event.IsActionPressed(PauseAction)) return;

            Toggle();

            // Prevents other nodes from reacting to the same key press.
            GetViewport().SetInputAsHandled();
        }

        public override void _ExitTree()
        {
            // Safety net: leaving the level while paused (e.g. via a quit button) must not leave the next
            // scene frozen, since SceneTree.Paused survives scene changes.
            if (IsOpen)
            {
                GetTree().Paused = false;
            }
        }

        #endregion // Godot overrides

        #region Public API

        /// <summary>
        /// Opens the menu, pauses the game and focuses the resume button. Does nothing if it is already open.
        /// </summary>
        public void Open()
        {
            if (IsOpen) return;

            GetTree().Paused = true;
            Visible = true;
            _resumeButton?.GrabFocus();
        }

        /// <summary>
        /// Closes the menu and resumes the game. Does nothing if it is already closed.
        /// </summary>
        public void Close()
        {
            if (!IsOpen) return;

            Visible = false;
            GetTree().Paused = false;
        }

        #endregion // Public API

        #region Signal handlers

        /// <summary>
        /// Resumes the game. Connected in the editor to the resume button's <c>Pressed</c> signal.
        /// It is an instance method on purpose: editor-connected handlers must not be static.
        /// </summary>
        private void OnResumePressed()
        {
            Close();
        }

        /// <summary>
        /// Quits the level and returns to the main menu. Connected in the editor to the 
        /// quit button's <c>Pressed</c> signal. It is an instance method on purpose: 
        /// editor-connected handlers must not be static. 
        /// </summary>
        private void OnQuitPressed()
        {
            GetTree().Paused = false;
            SceneController.Instance.TransitionTo(Scene.MainMenu);
        }

        /// <summary>
        /// Restarts the current level. Connected in the editor to the retry button's <c>Pressed</c> signal.
        /// It is an instance method on purpose: editor-connected handlers must not be static.
        /// </summary>
        private void OnRetryPressed()
        {
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        }

        #endregion // Signal handlers

        #region Helpers

        /// <summary>
        /// Opens the menu if it is closed, closes it otherwise.
        /// </summary>
        private void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        #endregion // Helpers
    }
}