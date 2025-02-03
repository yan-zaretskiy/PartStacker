namespace PartStacker
{
    public static class Constants
    {
        public const int VersionMajor = 0;
        public const int VersionMinor = 1;
        public const int VersionPatch = 0;
        public static string VersionString = (VersionPatch == 0)
            ? $"{VersionMajor}.{VersionMinor}"
            : $"{VersionMajor}.{VersionMinor}.{VersionPatch}";

        public const int InitialWidth = 1030;
        public const int InitialHeight = 654;

        public const int PanelWidth = 380;
        public const int PanelPadding = 20;
        public const int PanelTotalWidth = PanelPadding + PanelWidth + PanelPadding;

        public const int PartsListInitialHeight = 240;
    }
}
