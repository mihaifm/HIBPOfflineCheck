namespace HIBPOfflineCheck
{
    public class Options
    {
        public enum CheckModeType
        {
            Offline = 0,
            Online = 1,
            BloomFilter = 2
        }

        public static class Names
        {
            private const string PLUGIN_NAMESPACE = "HIBPOfflineCheck";

            public const string HIBP_FILE_NAME = PLUGIN_NAMESPACE + ".HIBPFileName";
            public const string COLUMN_NAME = PLUGIN_NAMESPACE + ".ColumnName";
            public const string SECURE_TEXT = PLUGIN_NAMESPACE + ".SecureText";
            public const string INSECURE_TEXT = PLUGIN_NAMESPACE + ".InsecureText";
            public const string BREACH_COUNT_DETAILS = PLUGIN_NAMESPACE + ".BreachCountDetails";
            public const string WARNING_DIALOG = PLUGIN_NAMESPACE + ".WarningDialog";
            public const string WARNING_DIALOG_TEXT = PLUGIN_NAMESPACE + ".WarningDialogText";
            public const string CHECK_MODE = PLUGIN_NAMESPACE + ".CheckMode";
            public const string BLOOM_FILTER = PLUGIN_NAMESPACE + ".BloomFilter";
        }

        public string HIBPFileName { get; set; }
        public string ColumnName { get; set; }
        public string SecureText { get; set; }
        public string InsecureText { get; set; }
        public bool BreachCountDetails { get; set; }
        public bool WarningDialog { get; set; }
        public string WarningDialogText { get; set; }
        public CheckModeType CheckMode { get; set; }
        public string BloomFilter { get; set; }
    }
}
