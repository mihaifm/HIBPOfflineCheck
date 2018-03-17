using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using KeePass;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;

using KeePassLib;
using System.Security.Cryptography;
using System.IO;
using KeePassLib.Security;
using System.Diagnostics;
using System.Collections;

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
        private const string HIBPFileName = @"pwned-passwords-ordered*";
        private const string HIBPColumnName = "Have I been pwned?";
        private string Status { get; set; }

        public IPluginHost Host { get; set; }

        public override string[] ColumnNames
        {
            get { return new string[] { HIBPColumnName }; }
        }

        public override bool SupportsCellAction(string strColumnName)
        {
            return (strColumnName == HIBPColumnName);
        }

        public override void PerformCellAction(string strColumnName, PwEntry pe)
        {
            if (strColumnName == null || pe == null) { Debug.Assert(false); return; }
            if (strColumnName != HIBPColumnName) { return; }

            if (pe.Strings.Get(PwDefs.PasswordField) == null)
            {
                return;
            }

            SHA1 sha1 = new SHA1CryptoServiceProvider();

            var pwd_sha_bytes = sha1.ComputeHash(pe.Strings.Get(PwDefs.PasswordField).ReadUtf8());
            var pwd_sha_str = "";
            foreach (byte b in pwd_sha_bytes)
            {
                pwd_sha_str += b.ToString("x2");
            }
            pwd_sha_str = pwd_sha_str.ToUpperInvariant();

            var files = Directory.GetFiles(".", HIBPFileName);
            if (files.Length == 0)
            {
                Status = "HIBP file not found";
                UpdateStatus(Status, pe);
                return;
            }
            
            try
            {
                FileStream fs = File.OpenRead(files[0]);
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

                    // We may have read only a partial line so read again to make sure we get a full line
                    if ((middle > 0) && (!sr.EndOfStream)) line = sr.ReadLine() ?? "";

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

                UpdateStatus(Status, pe);

                sr.Close();
                fs.Close();
            }
            catch (Exception)
            {
                Status = "Failed to read HIBP file";
                UpdateStatus(Status, pe);
                return;
            }
        }

        private void PwdTouchedHandler(object sender, ObjectTouchedEventArgs e)
        {
            PwEntry pe = sender as PwEntry;
            if (e.Modified)
            {
                pe.Strings.Set(HIBPColumnName, new ProtectedString(true, ""));
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

        private void UpdateStatus(string status, PwEntry pe)
        {
            MainForm mf = HIBPOfflineCheckExt.Host.MainWindow;
            ListView lv = (mf.Controls.Find("m_lvEntries", true)[0] as ListView);

            UIScrollInfo scroll = UIUtil.GetScrollInfo(lv, true);

            pe.Strings.Set(HIBPColumnName, new ProtectedString(true, status));

            mf.UpdateUI(false, null, false, null, true, null, true);

            UIUtil.Scroll(lv, scroll, true);
        }
    }
}
