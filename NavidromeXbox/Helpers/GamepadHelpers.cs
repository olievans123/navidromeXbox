using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace NavidromeXbox.Helpers
{
    /// <summary>
    /// Small helpers for the 10-foot experience. XYFocus keyboard/gamepad navigation
    /// is on by default in UWP, but a couple of conveniences make the app feel native.
    /// </summary>
    public static class GamepadHelpers
    {
        /// <summary>Give an element initial focus once it is loaded (so a controller has something to land on).</summary>
        public static void FocusOnLoad(this Control control)
        {
            if (control == null) return;
            // Detail pages call this after an await, by which point Loaded has already fired.
            if (control.IsLoaded) { control.Focus(FocusState.Programmatic); return; }
            control.Loaded += (s, e) => control.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Drop focus onto the first item of a list/grid so the controller lands on content
        /// rather than nothing. Safe to call right after binding — it forces a layout pass so
        /// the first container exists, and falls back to focusing the list itself.
        /// </summary>
        public static void FocusFirstItem(this ListViewBase list)
        {
            if (list == null) return;
            void Try()
            {
                if (list.Items == null || list.Items.Count == 0) return;
                list.UpdateLayout();
                if (list.ContainerFromIndex(0) is Control c) c.Focus(FocusState.Programmatic);
                else list.Focus(FocusState.Programmatic);
            }
            if (list.IsLoaded) Try();
            else list.Loaded += (s, e) => Try();
        }

        /// <summary>True when a text field has focus — used to suppress media accelerators while typing.</summary>
        public static bool IsTextInputFocused()
        {
            var f = FocusManager.GetFocusedElement();
            return f is TextBox || f is PasswordBox || f is AutoSuggestBox;
        }

        public static bool IsRunningOnXbox =>
            Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox";
    }
}
