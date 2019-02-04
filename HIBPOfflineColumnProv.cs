using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util.Spr;
using KeePassLib;
using KeePassLib.Security;

namespace HIBPOfflineCheck
{
    public sealed class HIBPOfflineColumnProv : ColumnProvider
    {
        private string Status { get; set; }
        private PwEntry PasswordEntry { get; set; }

        public IPluginHost Host { private get; set; }
        public Options PluginOptions { private get; set; }

        private bool insecureWarning;
        private bool passwordEdited;

        public HIBPOfflineColumnProv()
        {
            HIBPOfflineCheckExt.Host.MainWindow.EntryContextMenu.Opening += ContextMenuStrip_Opening;
        }

        public override string[] ColumnNames => new [] { PluginOptions.ColumnName };

        public override bool SupportsCellAction(string strColumnName)
        {
            return (strColumnName == PluginOptions.ColumnName);
        }

        private void GetPasswordStatus()
        {
            var latestFile = PluginOptions.HIBPFileName;
            if (!File.Exists(latestFile))
            {
                Status = "HIBP file not found";
                return;
            }

            string pwdShaStr;
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var context = new SprContext(PasswordEntry, Host.Database, SprCompileFlags.All);
                var password = SprEngine.Compile(PasswordEntry.Strings.GetSafe(PwDefs.PasswordField).ReadString(), context);

                var pwdShaBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder(2 * pwdShaBytes.Length);

                foreach (var b in pwdShaBytes)
                {
                    sb.AppendFormat("{0:X2}", b);
                }
                pwdShaStr = sb.ToString();
            }

            using (var fs = File.OpenRead(latestFile))
            using (var sr = new StreamReader(fs))
            {
                try
                {
                    Status = PluginOptions.SecureText;
                    var shaLen = pwdShaStr.Length;

                    var low = 0L;
                    var high = fs.Length;

                    while (low <= high)
                    {
                        var middle = (low + high + 1) / 2;
                        fs.Seek(middle, SeekOrigin.Begin);

                        // Resync with base stream after seek
                        sr.DiscardBufferedData();

                        var line = sr.ReadLine();

                        if (sr.EndOfStream) break;

                        // We may have read only a partial line so read again to make sure we get a full line
                        if (middle > 0) line = sr.ReadLine() ?? string.Empty;

                        if (line != null)
                        {
                            var compare = string.Compare(pwdShaStr, line.Substring(0, shaLen), StringComparison.Ordinal);

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
                                var tokens = line.Split(':');
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
            if (pe == null) return string.Empty;

            pe.Touched -= PwdTouchedHandler;
            pe.Touched += PwdTouchedHandler;

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

        private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            ToolStripItem[] items = mainForm.EntryContextMenu.Items.Find("m_ctxEntryMassModify", true);

            if (items.Length > 0)
            {
                ToolStripMenuItem ctxEntryMassModify = items[0] as ToolStripMenuItem;

                if (ctxEntryMassModify != null)
                {
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

                        hibpMenuItem.Click += OnMenuHIBP;
                        ctxEntryMassModify.DropDownItems.Add(hibpMenuItem);

                        ToolStripMenuItem hibpClearMenuItem = new ToolStripMenuItem()
                        {
                            Name = "m_ctxEntryHIBPClear",
                            Text = "Clear pwned status"
                        };

                        hibpClearMenuItem.Click += OnMenuHIBPClear;
                        ctxEntryMassModify.DropDownItems.Add(hibpClearMenuItem);

                    }
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

                UpdateStatusDelegate updateStatusDel = GetPasswordStatus;
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