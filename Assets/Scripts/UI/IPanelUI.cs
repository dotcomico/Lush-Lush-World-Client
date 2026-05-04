namespace LushWorld.UI
{
    public interface IPanelUI
    {
        bool IsOpen { get; }
        void ForceClose();
    }
}
