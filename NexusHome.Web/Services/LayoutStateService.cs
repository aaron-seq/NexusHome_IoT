using System;

namespace NexusHome.Web.Services
{
    public class LayoutStateService
    {
        public enum LayoutMode
        {
            Standard,
            Relax,
            Emergency
        }

        public LayoutMode CurrentMode { get; private set; } = LayoutMode.Standard;

        public event Action? OnChange;

        public void SetMode(LayoutMode mode)
        {
            if (CurrentMode != mode)
            {
                CurrentMode = mode;
                NotifyStateChanged();
            }
        }

        public string GetModeCssClass()
        {
            return CurrentMode switch
            {
                LayoutMode.Relax => "mode-relax",
                LayoutMode.Emergency => "mode-emergency",
                _ => ""
            };
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
