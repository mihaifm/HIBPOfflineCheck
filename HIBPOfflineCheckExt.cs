using System;
using System.Windows.Forms;
using KeePass.Plugins;
using System.IO;
using KeePassLib.Utility;
using KeePass.Util;

namespace HIBPOfflineCheck
{
    public sealed class HIBPOfflineCheckExt : Plugin
    {
        internal static IPluginHost Host { get; private set; }
        private HIBPOfflineColumnProv prov;
        private Options options;

        public override bool Initialize(IPluginHost host)
        {
            Terminate();

            if (host == null) return false;
            
            Host = host;
            prov = new HIBPOfflineColumnProv() { Host = host };

            options = LoadOptions();
            prov.PluginOptions = options;

            Host.ColumnProviderPool.Add(prov);

            return true;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t == PluginMenuType.Main)
            {
                ToolStripMenuItem tsMenuItem = new ToolStripMenuItem("HIBP Offline Check...");
                tsMenuItem.Click += ToolsMenuItemClick;
                return tsMenuItem;
            }

            return null;
        }

        private void ToolsMenuItemClick(object sender, EventArgs e)
        {
            HIBPOfflineCheckOptions optionsForm = new HIBPOfflineCheckOptions(this);
            optionsForm.Show();
        }

        private static string GetDefaultFileName()
        {
            string appdir = UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), false, true);
            var files = Directory.GetFiles(appdir, @"pwned-passwords-sha1-ordered*.txt");
            if (files.Length == 0)
            {
                return "";
            }

            var latestFile = files[0];

            if (files.Length > 1)
            {
                DateTime maxCreationTime = File.GetLastWriteTime(latestFile);

                for (int i = 1; i < files.Length; i++)
                {
                    DateTime creationTime = File.GetLastWriteTime(files[i]);

                    if (creationTime > maxCreationTime)
                    {
                        maxCreationTime = creationTime;
                        latestFile = files[i];
                    }
                }
            }

            return Path.GetFullPath(latestFile);
        }

        public override void Terminate()
        {
            if (Host == null) return;

            Host.ColumnProviderPool.Remove(prov);
            prov = null;

            Host = null;
        }

        public Options LoadOptions()
        {
            var config = Host.CustomConfig;

            Options options = new Options()
            {
                CheckMode = (Options.CheckModeType) config.GetLong(Options.Names.CHECK_MODE, 0),
                HIBPFileName = config.GetString(Options.Names.HIBP_FILE_NAME) ?? GetDefaultFileName(),
                ColumnName = config.GetString(Options.Names.COLUMN_NAME) ?? "Have I been pwned?",
                SecureText = config.GetString(Options.Names.SECURE_TEXT) ?? "Secure",
                InsecureText = config.GetString(Options.Names.INSECURE_TEXT) ?? "Pwned",
                BreachCountDetails = config.GetBool(Options.Names.BREACH_COUNT_DETAILS, true),
                WarningDialog = config.GetBool(Options.Names.WARNING_DIALOG, false),
                WarningDialogText = XmlUnescape(config.GetString(Options.Names.WARNING_DIALOG_TEXT) ?? "WARNING - INSECURE PASSWORD\r\n\r\nThis password is insecure and publicly known")
            };

            this.options = options;
            prov.PluginOptions = options;

            return options;
        }

        public void SaveOptions(Options options)
        {
            var config = Host.CustomConfig;

            config.SetLong(Options.Names.CHECK_MODE, (long) options.CheckMode);
            config.SetString(Options.Names.HIBP_FILE_NAME, options.HIBPFileName);
            config.SetString(Options.Names.COLUMN_NAME, options.ColumnName);
            config.SetString(Options.Names.SECURE_TEXT, options.SecureText);
            config.SetString(Options.Names.INSECURE_TEXT, options.InsecureText);
            config.SetBool(Options.Names.BREACH_COUNT_DETAILS, options.BreachCountDetails);
            config.SetBool(Options.Names.WARNING_DIALOG, options.WarningDialog);
            config.SetString(Options.Names.WARNING_DIALOG_TEXT, XmlEscape(options.WarningDialogText));

            this.options = options;
            prov.PluginOptions = options;
        }

        private static string XmlEscape(string unescaped)
        {
            return unescaped.Replace(Environment.NewLine, "&#xD;&#xA;");
        }

        private static string XmlUnescape(string escaped)
        {
            return escaped.Replace("&#xD;&#xA;", Environment.NewLine);
        }

        public override string UpdateUrl
        {
            get
            {
                return "https://raw.githubusercontent.com/mihaifm/HIBPOfflineCheck/master/version.txt";
            }
        }
    }
}
