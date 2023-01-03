using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;
using KeePassLib.Utility;
using System;
using System.IO;
using System.Windows.Forms;

namespace HIBPOfflineCheck
{
    public sealed class HIBPOfflineCheckExt : Plugin
    {
        internal static IPluginHost Host { get; private set; }
        private HIBPOfflineColumnProv prov;
        private Options options;

        private EventHandler<GwmWindowEventArgs> windowAddedHandler;
        public HIBPOfflineColumnProv Prov { get { return prov; } }

        public override bool Initialize(IPluginHost host)
        {
            Terminate();

            if (host == null) return false;
            
            Host = host;
            prov = new HIBPOfflineColumnProv() { Host = host };

            options = LoadOptions();
            prov.PluginOptions = options;

            Host.ColumnProviderPool.Add(prov);

            windowAddedHandler = new EventHandler<GwmWindowEventArgs>(WindowAddedHandler);
            GlobalWindowManager.WindowAdded += windowAddedHandler;

            CreateMenuItems();

            return true;
        }

        public void WindowAddedHandler(object sender, GwmWindowEventArgs e)
        {
            PwEntryForm form = e.Form as PwEntryForm;
            if (form == null) return;

            Prov.ClearEventHandlers(form);
            form.EntrySaved += prov.EntrySaved;
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
            optionsForm.ShowDialog();
        }

        private void CreateMenuItems()
        {
            string hibpMenuItemText = "Have I been pwned?";

            ContextMenuStrip entryContextMenu = Host.MainWindow.EntryContextMenu;

            entryContextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem hibpCtxMenuItem = new ToolStripMenuItem(hibpMenuItemText);
            entryContextMenu.Items.Add(hibpCtxMenuItem);

            ToolStripMenuItem hibpCheckCtx = new ToolStripMenuItem("Check");
            hibpCheckCtx.Click += new EventHandler(prov.OnMenuHIBP);
            hibpCtxMenuItem.DropDownItems.Add(hibpCheckCtx);

            ToolStripMenuItem hibpClearCtx = new ToolStripMenuItem("Clear");
            hibpClearCtx.Click += new EventHandler(prov.OnMenuHIBPClear);
            hibpCtxMenuItem.DropDownItems.Add(hibpClearCtx);

            ToolStripMenuItem hibpExcludeCtx = new ToolStripMenuItem("Exclude");
            hibpExcludeCtx.Click += new EventHandler(prov.OnMenuHIBPExclude);
            hibpCtxMenuItem.DropDownItems.Add(hibpExcludeCtx);

            var m_menuEntry = Host.MainWindow.MainMenu.Items.Find("m_menuEntry", true);

            if (m_menuEntry.Length > 0)
            {
                ToolStripMenuItem entryMenu = m_menuEntry[0] as ToolStripMenuItem;

                entryMenu.DropDownItems.Add(new ToolStripSeparator());

                ToolStripMenuItem hibpMenuItem = new ToolStripMenuItem(hibpMenuItemText);
                entryMenu.DropDownItems.Add(hibpMenuItem);

                ToolStripMenuItem hibpCheckMenuItem = new ToolStripMenuItem("Check");
                hibpCheckMenuItem.Click += new EventHandler(prov.OnMenuHIBP);
                hibpMenuItem.DropDownItems.Add(hibpCheckMenuItem);

                ToolStripMenuItem hibpClearMenuItem = new ToolStripMenuItem("Clear");
                hibpClearMenuItem.Click += new EventHandler(prov.OnMenuHIBPClear);
                hibpMenuItem.DropDownItems.Add(hibpClearMenuItem);

                ToolStripMenuItem hibpExcludeMenuItem = new ToolStripMenuItem("Exclude");
                hibpExcludeMenuItem.Click += new EventHandler(prov.OnMenuHIBPExclude);
                hibpMenuItem.DropDownItems.Add(hibpExcludeMenuItem);
            }

            var m_menuFind = Host.MainWindow.MainMenu.Items.Find("m_menuFind", true);

            if (m_menuFind.Length > 0)
            {
                var findMenu = m_menuFind[0] as ToolStripMenuItem;

                findMenu.DropDownItems.Add(new ToolStripSeparator());

                var findPwnedItem = new ToolStripMenuItem("Pwned Passwords");
                findPwnedItem.Click += new EventHandler(prov.OnMenuFindPwned);
                findMenu.DropDownItems.Add(findPwnedItem);
            }
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
                ExcludedText = config.GetString(Options.Names.EXCLUDED_TEXT) ?? "Excluded",
                BreachCountDetails = config.GetBool(Options.Names.BREACH_COUNT_DETAILS, true),
                ExcludeRecycleBin = config.GetBool(Options.Names.EXCLUDE_RECYCLE_BIN, false),
                ExcludeExpired = config.GetBool(Options.Names.EXCLUDE_EXPIRED, false),
                WarningDialog = config.GetBool(Options.Names.WARNING_DIALOG, false),
                AutoCheck = config.GetBool(Options.Names.AUTO_CHECK, true),
                WarningDialogText = XmlUnescape(config.GetString(Options.Names.WARNING_DIALOG_TEXT) ?? "WARNING - INSECURE PASSWORD\r\n\r\nThis password is insecure and publicly known"),
                BloomFilter = config.GetString(Options.Names.BLOOM_FILTER) ?? ""
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
            config.SetString(Options.Names.EXCLUDED_TEXT, options.ExcludedText);
            config.SetString(Options.Names.INSECURE_TEXT, options.InsecureText);
            config.SetBool(Options.Names.BREACH_COUNT_DETAILS, options.BreachCountDetails);
            config.SetBool(Options.Names.EXCLUDE_RECYCLE_BIN, options.ExcludeRecycleBin);
            config.SetBool(Options.Names.EXCLUDE_EXPIRED, options.ExcludeExpired);
            config.SetBool(Options.Names.WARNING_DIALOG, options.WarningDialog);
            config.SetBool(Options.Names.AUTO_CHECK, options.AutoCheck);
            config.SetString(Options.Names.WARNING_DIALOG_TEXT, XmlEscape(options.WarningDialogText));
            config.SetString(Options.Names.BLOOM_FILTER, options.BloomFilter);

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
