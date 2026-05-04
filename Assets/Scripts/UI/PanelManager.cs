namespace LushWorld.UI
{
    // Ensures only one full-screen panel is open at a time.
    // Panels call RequestOpen(this) when opening and NotifyClosed(this) when closing.
    public static class PanelManager
    {
        private static IPanelUI _activePanel;

        public static void RequestOpen(IPanelUI incoming)
        {
            if (_activePanel != null && !ReferenceEquals(_activePanel, incoming) && _activePanel.IsOpen)
                _activePanel.ForceClose();
            _activePanel = incoming;
        }

        public static void NotifyClosed(IPanelUI panel)
        {
            if (ReferenceEquals(_activePanel, panel))
                _activePanel = null;
        }
    }
}
