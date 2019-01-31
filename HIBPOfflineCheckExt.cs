using System;
using System.Windows.Forms;

using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;

using KeePassLib;
using System.Security.Cryptography;
using System.IO;
using KeePassLib.Security;
using System.Diagnostics;
using KeePassLib.Utility;
using KeePass.Util;
using System.Text;
using KeePass.Util.Spr;

namespace HIBPOfflineCheck
{
    public sealed class HIBPOfflineCheckExt : Plugin
    {
        private HIBPOfflineColumnProv prov = null;
        private static IPluginHost PluginHost = null;

        internal static IPluginHost Host
        {
            get { return PluginHost; }
        }

        private Options options = null;

        public override bool Initialize(IPluginHost host)
        {
            Terminate();

            if (host == null) return false;

            PluginHost = host;
            prov = new HIBPOfflineColumnProv() { Host = host };

            options = LoadOptions();
            prov.PluginOptions = options;

            PluginHost.ColumnProviderPool.Add(prov);

            return true;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t == PluginMenuType.Main)
            {
                ToolStripMenuItem tsMenuItem = new ToolStripMenuItem("HIBP Offline Check...");
                tsMenuItem.Click += new EventHandler(ToolsMenuItemClick);
                return tsMenuItem;
            }

            return null;
        }

        private void ToolsMenuItemClick(object sender, EventArgs e)
        {
            HIBPOfflineCheckOptions optionsForm = new HIBPOfflineCheckOptions(this);
            optionsForm.Show();
        }

        private string GetDefaultFileName()
        {
            string appdir = UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), false, true);
            var files = Directory.GetFiles(appdir, @"pwned-passwords-ordered*.txt");
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
            if (PluginHost == null) return;

            PluginHost.ColumnProviderPool.Remove(prov);
            prov = null;

            PluginHost = null;
        }

        public Options LoadOptions()
        {
            var config = PluginHost.CustomConfig;

            Options options = new Options()
            {
                HIBPFileName = config.GetString(Options.Names.HIBPFileName, GetDefaultFileName()),
                ColumnName = config.GetString(Options.Names.ColumnName, "Have I been pwned?"),
                SecureText = config.GetString(Options.Names.SecureText, "Secure"),
                InsecureText = config.GetString(Options.Names.InsecureText, "Pwned"),
                BreachCountDetails = config.GetBool(Options.Names.BreachCountDetails, true),
                WarningDialog = config.GetBool(Options.Names.WarningDialog, false),
                WarningDialogText = XmlUnescape(config.GetString(Options.Names.WarningDialogText,
                    "WARNING - INSECURE PASSWORD\r\n\r\nThis password is insecure and publicly known"))
            };

            this.options = options;
            prov.PluginOptions = options;

            return options;
        }

        public void SaveOptions(Options options)
        {
            var config = PluginHost.CustomConfig;

            config.SetString(Options.Names.HIBPFileName, options.HIBPFileName);
            config.SetString(Options.Names.ColumnName, options.ColumnName);
            config.SetString(Options.Names.SecureText, options.SecureText);
            config.SetString(Options.Names.InsecureText, options.InsecureText);
            config.SetBool(Options.Names.BreachCountDetails, options.BreachCountDetails);
            config.SetBool(Options.Names.WarningDialog, options.WarningDialog);
            config.SetString(Options.Names.WarningDialogText, XmlEscape(options.WarningDialogText));

            this.options = options;
            prov.PluginOptions = options;
        }

        public static string XmlEscape(string unescaped)
        {
            return unescaped.Replace(Environment.NewLine, "&#xD;&#xA;");
        }

        public static string XmlUnescape(string escaped)
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

    public sealed class HIBPOfflineColumnProv : ColumnProvider
    {
        private string Status { get; set; }
        private PwEntry PasswordEntry { get; set; }

        public IPluginHost Host { get; set; }
        public Options PluginOptions { get; set; }

        private bool insecureWarning = false;
        private bool passwordEdited = false;

        public HIBPOfflineColumnProv()
        {
            HIBPOfflineCheckExt.Host.MainWindow.EntryContextMenu.Opening += ContextMenuStrip_Opening;
        }

        public override string[] ColumnNames
        {
            get { return new string[] { PluginOptions.ColumnName }; }
        }

        public override bool SupportsCellAction(string strColumnName)
        {
            return (strColumnName == PluginOptions.ColumnName);
        }

        private void GetPasswordStatus()
        {
            var pwd_sha_str = String.Empty;

            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var context = new SprContext(PasswordEntry, Host.Database, SprCompileFlags.All);
                var password = SprEngine.Compile(PasswordEntry.Strings.GetSafe(PwDefs.PasswordField).ReadString(), context);

                var pwd_sha_bytes = sha1.ComputeHash(UTF8Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder(2 * pwd_sha_bytes.Length);

                foreach (byte b in pwd_sha_bytes)
                {
                    sb.AppendFormat("{0:X2}", b);
                }
                pwd_sha_str = sb.ToString();
            }

            var latestFile = PluginOptions.HIBPFileName;

            if (!File.Exists(latestFile))
            {
                Status = "HIBP file not found";
                return;
            }

            using (FileStream fs = File.OpenRead(latestFile))
            using (StreamReader sr = new StreamReader(fs))
            {
                try
                {
                    string line;
                    Status = PluginOptions.SecureText;
                    int sha_len = pwd_sha_str.Length;

                    var low = 0L;
                    var high = fs.Length;

                    while (low <= high)
                    {
                        var middle = (low + high + 1) / 2;
                        fs.Seek(middle, SeekOrigin.Begin);

                        // Resync with base stream after seek
                        sr.DiscardBufferedData();

                        line = sr.ReadLine();

                        if (sr.EndOfStream) break;

                        // We may have read only a partial line so read again to make sure we get a full line
                        if (middle > 0) line = sr.ReadLine() ?? String.Empty;

                        int compare = String.Compare(pwd_sha_str, line.Substring(0, sha_len), StringComparison.Ordinal);

                        if (compare < 0)
                        {
                            high = middle - 1;
                        }
                        else if (compare > 0)
                        {
                            low = middle + 1;
                        }
                        else
                        {
                            string[] tokens = line.Split(':');
                            Status = PluginOptions.InsecureText;
                            insecureWarning = true;

                            if (PluginOptions.BreachCountDetails)
                            {
                                Status += " (password count: " + tokens[1].Trim() + ")";
                            }

                            break;
                        }
                    }
                }
                catch
                {
                    Status = "Failed to read HIBP file";
                }
            }
        }

        public override void PerformCellAction(string strColumnName, PwEntry pe)
        {
            if (strColumnName == null || pe == null) { Debug.Assert(false); return; }
            if (strColumnName != PluginOptions.ColumnName) { return; }

            PasswordEntry = pe;

            GetPasswordStatus();
            UpdateStatus();
        }

        private void PwdTouchedHandler(object sender, ObjectTouchedEventArgs e)
        {
            PwEntry pe = sender as PwEntry;
            if (e.Modified)
            {
                passwordEdited = true;
                PerformCellAction(PluginOptions.ColumnName, pe);
            }
        }

        public override string GetCellData(string strColumnName, PwEntry pe)
        {
            pe.Touched -= this.PwdTouchedHandler;
            pe.Touched += this.PwdTouchedHandler;

            return pe.Strings.GetSafe(PluginOptions.ColumnName).ReadString();
        }

        private void UpdateStatus()
        {
            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            ListView lv = (mainForm.Controls.Find("m_lvEntries", true)[0] as ListView);

            UIScrollInfo scroll = UIUtil.GetScrollInfo(lv, true);

            PasswordEntry.Strings.Set(PluginOptions.ColumnName, new ProtectedString(true, Status));

            mainForm.UpdateUI(false, null, false, null, true, null, true);

            UIUtil.Scroll(lv, scroll, true);

            if (insecureWarning && passwordEdited && PluginOptions.WarningDialog)
            {
                MessageBox.Show(PluginOptions.WarningDialogText,
                    "HIBP Offline Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            insecureWarning = false;
            passwordEdited = false;
        }

        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            ToolStripItem[] items = mainForm.EntryContextMenu.Items.Find("m_ctxEntryMassModify", true);

            if (items.Length > 0)
            {
                ToolStripMenuItem ctxEntryMassModify = items[0] as ToolStripMenuItem;

                ToolStripItem[] hibpItems = ctxEntryMassModify.DropDownItems.Find("m_ctxEntryHIBP", true);

                if (hibpItems.Length == 0)
                {
                    ToolStripSeparator separator = new ToolStripSeparator();
                    ctxEntryMassModify.DropDownItems.Add(separator);

                    ToolStripMenuItem hibpMenuItem = new ToolStripMenuItem()
                    {
                        Name = "m_ctxEntryHIBP",
                        Text = "Have I been pwned?"
                    };

                    hibpMenuItem.Click += this.OnMenuHIBP;
                    ctxEntryMassModify.DropDownItems.Add(hibpMenuItem);

                    ToolStripMenuItem hibpClearMenuItem = new ToolStripMenuItem()
                    {
                        Name = "m_ctxEntryHIBPClear",
                        Text = "Clear pwned status"
                    };

                    hibpClearMenuItem.Click += this.OnMenuHIBPClear;
                    ctxEntryMassModify.DropDownItems.Add(hibpClearMenuItem);

                }
            }
        }

        private delegate void UpdateStatusDelegate();

        private void OnMenuHIBP(object sender, EventArgs e)
        {
            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            PwEntry[] selectedEntries = mainForm.GetSelectedEntries();

            foreach (PwEntry pwEntry in selectedEntries)
            {
                PasswordEntry = pwEntry;

                UpdateStatusDelegate updateStatusDel = new UpdateStatusDelegate(GetPasswordStatus);
                IAsyncResult asyncRes = updateStatusDel.BeginInvoke(null, null);
                updateStatusDel.EndInvoke(asyncRes);
                
                UpdateStatus();
            }
        }

        private void OnMenuHIBPClear(object sender, EventArgs e)
        {
            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            PwEntry[] selectedEntries = mainForm.GetSelectedEntries();

            foreach (PwEntry pwEntry in selectedEntries)
            {
                pwEntry.Strings.Remove(PluginOptions.ColumnName);
            }

            mainForm.UpdateUI(false, null, false, null, true, null, true);
        }
    }
}
