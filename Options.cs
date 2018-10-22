using System;
using System.Collections.Generic;
using System.Text;

namespace HIBPOfflineCheck
{
    public class Options
    {
        public static class Names
        {
            public static string PluginNamespace = "HIBPOfflineCheck";
            public static string HIBPFileName = PluginNamespace + ".HIBPFileName";
            public static string ColumnName = PluginNamespace + ".ColumnName";
            public static string SecureText = PluginNamespace + ".SecureText";
            public static string InsecureText = PluginNamespace + ".InsecureText";
            public static string BreachCountDetails = PluginNamespace + ".BreachCountDetails";
            public static string WarningDialog = PluginNamespace + ".WarningDialog";
            public static string WarningDialogText = PluginNamespace + ".WarningDialogText";
        }

        public Options() { }

        public string HIBPFileName { get; set; }
        public string ColumnName { get; set; }
        public string SecureText { get; set; }
        public string InsecureText { get; set; }
        public bool BreachCountDetails { get; set; }
        public bool WarningDialog { get; set; }
        public string WarningDialogText { get; set; }
    }
}
