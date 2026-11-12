namespace OmniSight.Base.UI.Utils
{
    /// <summary>Refresh state-dependent captions without changing application or configuration state.</summary>
    public interface ILocalizedView
    {
        void RefreshLocalizedText();
    }
}
