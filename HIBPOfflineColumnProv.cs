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
using System.Net;
using KeePassLib.Collections;

namespace HIBPOfflineCheck
{
    public sealed class HIBPOfflineColumnProv : ColumnProvider
    {
        private string Status { get; set; }
        private PwEntry PasswordEntry { get; set; }

        public IPluginHost Host { private get; set; }
        public Options PluginOptions { private get; set; }

        private bool insecureWarning;
        private bool formEdited;
        private bool receivedStatus;

        public override string[] ColumnNames
        {
            get { return new string[] { PluginOptions.ColumnName }; }
        }

        public override bool SupportsCellAction(string strColumnName)
        {
            return (strColumnName == PluginOptions.ColumnName);
        }

        private void GetOnlineStatus()
        {
            var pwdSha = GetPasswordSHA();
            var truncatedSha = pwdSha.Substring(0, 5);

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var url = "https://api.pwnedpasswords.com/range/" + truncatedSha;

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.UserAgent = "KeePass-HIBP-plug/1.0";

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Status = "HIBP API error";
                        return;
                    }

                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string responseFromServer = reader.ReadToEnd();
                        var lines = responseFromServer.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        Status = PluginOptions.SecureText;

                        foreach (var line in lines)
                        {
                            string fullSha = truncatedSha + line;
                            var compare = string.Compare(pwdSha, fullSha.Substring(0, pwdSha.Length), StringComparison.Ordinal);

                            if (compare == 0)
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

                        reader.Close();
                        stream.Close();
                    }
                }
            }
            catch
            {
                Status = "HIBP API error";
            }
        }

        private string GetPasswordSHA()
        {
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

                return sb.ToString();
            }
        }

        private void GetPasswordStatus()
        {
            if (PluginOptions.CheckMode == Options.CheckModeType.Offline)
            {
                GetOfflineStatus();
            }
            else if (PluginOptions.CheckMode == Options.CheckModeType.Online)
            {
                GetOnlineStatus();
            }

            receivedStatus = true;
        }

        private void GetOfflineStatus()
        {
            string pwdShaStr = GetPasswordSHA();

            var latestFile = PluginOptions.HIBPFileName;
            if (!File.Exists(latestFile))
            {
                Status = "HIBP file not found";
                return;
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

            TouchEntry(PasswordEntry); 
        }

        private void PwdTouchedHandler(object sender, ObjectTouchedEventArgs e)
        {
            PwEntry pe = sender as PwEntry;
            if (e.Modified)
            {
                if (receivedStatus == false)
                {
                    PasswordEntry = pe;
                    GetPasswordStatus();
                    formEdited = true;
                }

                UpdateStatus();
            }
        }

        private void TouchEntry(PwEntry pe)
        {
            string currentStatus = null;

            var protectedStatus = pe.Strings.Get(PluginOptions.ColumnName);

            if (protectedStatus != null)
            {
                currentStatus = pe.Strings.Get(PluginOptions.ColumnName).ReadString();
            }

            if (currentStatus == null || currentStatus != Status)
            {
                pe.Touched -= PwdTouchedHandler;
                pe.Touched += PwdTouchedHandler;

                pe.Touch(true);
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

            if (insecureWarning && formEdited && PluginOptions.WarningDialog)
            {
                MessageBox.Show(PluginOptions.WarningDialogText,
                    "HIBP Offline Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            insecureWarning = false;
            formEdited = false;
            receivedStatus = false;
        }

        public void PasswordCheckWorker()
        {
            GetPasswordStatus();

            if (PluginOptions.CheckMode == Options.CheckModeType.Online)
            {
                System.Threading.Thread.Sleep(1600);
            }
        }

        public async void CheckAll()
        {
            var progressDisplay = new ProgressDisplay();
            progressDisplay.Show();

            var allEntries = new PwObjectList<PwEntry>();
            Host.Database.RootGroup.SearchEntries(SearchParameters.None, allEntries);

            for (uint i = 0; i < allEntries.UCount; i++)
            {
                PasswordEntry = allEntries.GetAt(i);

                await System.Threading.Tasks.Task.Run(() => PasswordCheckWorker());
                TouchEntry(PasswordEntry);
                progressDisplay.progressBar.Value = ((int) i + 1) * 100 / ((int) allEntries.UCount);

                if (progressDisplay.UserTerminated)
                {
                    progressDisplay.Close();
                    break;
                }
            }

            progressDisplay.Close();
        }

        public void ClearAll()
        {
            DialogResult dialog = MessageBox.Show("This will remove the HIBP status for all entries in the database. Continue?",
                String.Empty, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (dialog == DialogResult.Cancel)
                return;

            MainForm mainForm = Host.MainWindow;

            PwObjectList<PwEntry> allEntries = new PwObjectList<PwEntry>();
            Host.Database.RootGroup.SearchEntries(SearchParameters.None, allEntries);

            for (uint i = 0; i < allEntries.UCount; i++)
            {
                var pwEntry = allEntries.GetAt(i);
                pwEntry.Strings.Remove(PluginOptions.ColumnName);
            }

            mainForm.UpdateUI(false, null, false, null, true, null, true);
        }

        public async void OnMenuHIBP(object sender, EventArgs e)
        {
            var progressDisplay = new ProgressDisplay();
            progressDisplay.Show();

            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            PwEntry[] selectedEntries = mainForm.GetSelectedEntries();

            for (int j = 0; j < selectedEntries.Length; j++)
            {
                PasswordEntry = selectedEntries[j];

                await System.Threading.Tasks.Task.Run(() => PasswordCheckWorker());
                TouchEntry(PasswordEntry);
                progressDisplay.progressBar.Value = (j + 1) * 100 / selectedEntries.Length;

                if (progressDisplay.UserTerminated)
                {
                    progressDisplay.Close();
                    break;
                }
            }

            progressDisplay.Close();
        }

        public void OnMenuHIBPClear(object sender, EventArgs e)
        {
            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            PwEntry[] selectedEntries = mainForm.GetSelectedEntries();

            foreach (PwEntry pwEntry in selectedEntries)
            {
                pwEntry.Strings.Remove(PluginOptions.ColumnName);
            }

            mainForm.UpdateUI(false, null, false, null, true, null, true);
        }

        public void EntrySaved(object sender, EventArgs e)
        {
            PwEntryForm form = sender as PwEntryForm;

            form.EntryRef.Touched -= PwdTouchedHandler;
            form.EntryRef.Touched += PwdTouchedHandler;

            //only touch newly created entries, updated entries are touched by KeePass
            if (form.EntryRef.UsageCount <= 1)
            {
                form.EntryRef.Touch(true);
            }
        }
    }
}