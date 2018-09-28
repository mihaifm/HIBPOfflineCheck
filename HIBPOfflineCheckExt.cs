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

namespace HIBPOfflineCheck
{
    public sealed class HIBPOfflineCheckExt : Plugin
    {
        private HIBPOfflineColumnProv m_prov = null;

        private static IPluginHost m_host = null;

        internal static IPluginHost Host
        {
            get { return m_host; }
        }

        public override bool Initialize(IPluginHost host)
        {
            Terminate();

            if (host == null) return false;

            m_host = host;

            m_prov = new HIBPOfflineColumnProv();
            m_prov.Host = host;

            m_host.ColumnProviderPool.Add(m_prov);

            return true;
        }

        

        public override void Terminate()
        {
            if (m_host == null) return;

            m_host.ColumnProviderPool.Remove(m_prov);
            m_prov = null;

            m_host = null;
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
        private const string HIBPFileName = @"pwned-passwords-ordered*.txt";
        private const string HIBPColumnName = "Have I been pwned?";
        private string Status { get; set; }
        private PwEntry PasswordEntry { get; set; }

        public IPluginHost Host { get; set; }

        public HIBPOfflineColumnProv()
        {
            HIBPOfflineCheckExt.Host.MainWindow.EntryContextMenu.Opening += ContextMenuStrip_Opening;
        }

        public override string[] ColumnNames
        {
            get { return new string[] { HIBPColumnName }; }
        }

        public override bool SupportsCellAction(string strColumnName)
        {
            return (strColumnName == HIBPColumnName);
        }

        private void GetPasswordStatus()
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();

            var pwd_sha_bytes = sha1.ComputeHash(PasswordEntry.Strings.Get(PwDefs.PasswordField).ReadUtf8());
            var pwd_sha_str = "";
            foreach (byte b in pwd_sha_bytes)
            {
                pwd_sha_str += b.ToString("x2");
            }
            pwd_sha_str = pwd_sha_str.ToUpperInvariant();

            string appdir = UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), false, true);
            var files = Directory.GetFiles(appdir, HIBPFileName);
            if (files.Length == 0)
            {
                Status = "HIBP file not found";
                return;
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

            try
            {
                FileStream fs = File.OpenRead(latestFile);
                StreamReader sr = new StreamReader(fs);

                string line;
                Status = "Secure";
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
                    if (middle > 0) line = sr.ReadLine() ?? "";

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
                        Status = "Pwned";
                        Status += " (password count: " + tokens[1].Trim() + ")";

                        break;
                    }
                }

                sr.Close();
                fs.Close();
            }
            catch (Exception)
            {
                Status = "Failed to read HIBP file";
            }
        }

        public override void PerformCellAction(string strColumnName, PwEntry pe)
        {
            if (strColumnName == null || pe == null) { Debug.Assert(false); return; }
            if (strColumnName != HIBPColumnName) { return; }

            if (pe.Strings.Get(PwDefs.PasswordField) == null)
            {
                return;
            }

            PasswordEntry = pe;

            GetPasswordStatus();
            UpdateStatus();
        }

        private void PwdTouchedHandler(object sender, ObjectTouchedEventArgs e)
        {
            PwEntry pe = sender as PwEntry;
            if (e.Modified)
            {
                PerformCellAction(HIBPColumnName, pe);
            }
        }

        public override string GetCellData(string strColumnName, PwEntry pe)
        {
            pe.Touched -= this.PwdTouchedHandler;
            pe.Touched += this.PwdTouchedHandler;

            if (pe.Strings.Get(HIBPColumnName) == null)
            {
                return "";
            }

            return pe.Strings.Get(HIBPColumnName).ReadString();
        }

        private void UpdateStatus()
        {
            MainForm mf = HIBPOfflineCheckExt.Host.MainWindow;
            ListView lv = (mf.Controls.Find("m_lvEntries", true)[0] as ListView);

            UIScrollInfo scroll = UIUtil.GetScrollInfo(lv, true);

            PasswordEntry.Strings.Set(HIBPColumnName, new ProtectedString(true, Status));

            mf.UpdateUI(false, null, false, null, true, null, true);

            UIUtil.Scroll(lv, scroll, true);
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

                    ToolStripMenuItem hibpMenuItem = new ToolStripMenuItem();
                    hibpMenuItem.Name = "m_ctxEntryHIBP";
                    hibpMenuItem.Text = HIBPColumnName;
                    hibpMenuItem.Click += this.OnMenuHIBP;
                    ctxEntryMassModify.DropDownItems.Add(hibpMenuItem);
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
    }
}
