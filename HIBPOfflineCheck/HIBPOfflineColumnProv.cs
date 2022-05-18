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
using KeePassLib.Delegates;
using KeePassLib.Serialization;

namespace HIBPOfflineCheck
{
    public sealed class HIBPOfflineColumnProv : ColumnProvider
    {
        private string Status { get; set; }
        private PwEntry PasswordEntry { get; set; }

        public IPluginHost Host { private get; set; }
        public Options PluginOptions { get; set; }

        private bool insecureWarning = false;
        private bool receivedStatus = false;
        private string currentStatus = null;
        private bool bulkCheck = false;

        public BloomFilter BloomFilter { get; set; }

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
            currentStatus = GetCurrentStatus(PasswordEntry);

            if (currentStatus == PluginOptions.ExcludedText)
            {
                Status = PluginOptions.ExcludedText;
                return;
            }

            if (PluginOptions.CheckMode == Options.CheckModeType.Offline)
            {
                GetOfflineStatus();
            }
            else if (PluginOptions.CheckMode == Options.CheckModeType.Online)
            {
                GetOnlineStatus();
            }
            else if (PluginOptions.CheckMode == Options.CheckModeType.BloomFilter)
            {
                GetBloomStatus();
            }

            receivedStatus = true;
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

        private void GetOnlineStatus()
        {
            var pwdSha = GetPasswordSHA();
            var truncatedSha = pwdSha.Substring(0, 5);

            var url = "https://api.pwnedpasswords.com/range/" + truncatedSha;

            IOConnectionInfo ioc = new IOConnectionInfo
            {
                Path = url
            };

            try
            {
                using (Stream stream = IOConnection.OpenRead(ioc))
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
            catch
            {
                Status = "HIBP API error";
            }
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

        private void GetBloomStatus()
        {
            string pwdShaStr = GetPasswordSHA();

            var bloomFilterFile = PluginOptions.BloomFilter;
            if (!File.Exists(bloomFilterFile))
            {
                Status = "Bloom Filter not found";
                return;
            }

            if (BloomFilter == null || BloomFilter.Capacity == 0)
            {
                BloomFilter = new BloomFilter(bloomFilterFile);
            }

            if (BloomFilter.Contains(pwdShaStr))
            {
                Status = PluginOptions.InsecureText;
                insecureWarning = true;
            }
            else
            {
                Status = PluginOptions.SecureText;
            }
        }

        public override void PerformCellAction(string strColumnName, PwEntry pe)
        {
            if (strColumnName == null || pe == null) { Debug.Assert(false); return; }
            if (strColumnName != PluginOptions.ColumnName) { return; }

            PasswordEntry = pe;
            bulkCheck = false;

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

                    if (insecureWarning && PluginOptions.WarningDialog)
                    {
                        MessageBox.Show(PluginOptions.WarningDialogText,
                            "HIBP Offline Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (currentStatus != Status)
                {
                    UpdateStatus();
                }

                ResetState();
            }
        }

        private string GetCurrentStatus(PwEntry pe)
        {
            string currentStatus = null;

            var protectedStatus = pe.Strings.Get(PluginOptions.ColumnName);

            if (protectedStatus != null)
            {
                currentStatus = pe.Strings.Get(PluginOptions.ColumnName).ReadString();
            }

            return currentStatus;
        }

        private void TouchEntry(PwEntry pe)
        {
            if (currentStatus == null ||
                currentStatus != Status && currentStatus != PluginOptions.ExcludedText)
            {
                pe.Touched -= PwdTouchedHandler;
                pe.Touched += PwdTouchedHandler;

                pe.Touch(true);
            }

            ResetState();
        }

        private void ResetState()
        {
            insecureWarning = false;
            receivedStatus = false;
            currentStatus = null;
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

            PasswordEntry.Strings.Set(PluginOptions.ColumnName, new ProtectedString(false, Status));

            if (bulkCheck == false)
            {
                UpdateUI();
            }

            ResetState();
        }

        private void UpdateUI()
        {
            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            ListView lv = (mainForm.Controls.Find("m_lvEntries", true)[0] as ListView);
            UIScrollInfo scroll = UIUtil.GetScrollInfo(lv, true);
            mainForm.UpdateUI(false, null, false, null, true, null, true);
            UIUtil.Scroll(lv, scroll, true);
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
            bulkCheck = true;

            if (Host.Database == null || Host.Database.RootGroup == null)
                return;

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

            UpdateUI();
            
            progressDisplay.Close();
        }

        public void ClearAll()
        {
            if (Host.Database == null || Host.Database.RootGroup == null)
                return;

            DialogResult dialog = MessageBox.Show("This will remove the HIBP status for all entries in the database. Continue?",
                string.Empty, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (dialog == DialogResult.Cancel)
                return;

            bulkCheck = true;

            PwObjectList<PwEntry> allEntries = new PwObjectList<PwEntry>();
            Host.Database.RootGroup.SearchEntries(SearchParameters.None, allEntries);

            for (uint i = 0; i < allEntries.UCount; i++)
            {
                PasswordEntry = allEntries.GetAt(i);

                PasswordEntry.Strings.Remove(PluginOptions.ColumnName);
                Status = null;
                receivedStatus = true;
                TouchEntry(PasswordEntry);
            }

            UpdateUI();
        }

        public void OnMenuFindPwned(object sender, EventArgs e)
        {
            if (Host.Database == null || Host.Database.RootGroup == null)
                return;

            PwGroup pgResults = new PwGroup(true, true, string.Empty, PwIcon.List)
            {
                IsVirtual = true
            };

            PwGroup recycleBin = Host.Database.RootGroup.FindGroup(Host.Database.RecycleBinUuid, true);

            Host.Database.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, delegate (PwEntry pe)
            {
                var status = GetCurrentStatus(pe);
                if (status != null && status.StartsWith(PluginOptions.InsecureText))
                {
                    if (PluginOptions.ExcludeExpired && pe.Expires && pe.ExpiryTime.CompareTo(DateTime.UtcNow) <= 0)
                        return true;

                    if (PluginOptions.ExcludeRecycleBin)
                    {
                        var ancestor = pe.ParentGroup;

                        while (ancestor != null)
                        {
                            if (ancestor == recycleBin)
                                return true;

                            ancestor = ancestor.ParentGroup;
                        }
                    }

                    pgResults.AddEntry(pe, false, false);
                }

                return true;
            });

            var sp = new SearchParameters
            {
                RespectEntrySearchingDisabled = true
            };

            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            mainForm.UpdateUI(false, null, false, null, true, pgResults, false);
        }

        public async void OnMenuHIBP(object sender, EventArgs e)
        {
            bulkCheck = true;

            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            PwEntry[] selectedEntries = mainForm.GetSelectedEntries();

            if (selectedEntries == null)
                return;

            var progressDisplay = new ProgressDisplay();
            progressDisplay.Show();

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

            UpdateUI();

            progressDisplay.Close();
        }

        public void OnMenuHIBPClear(object sender, EventArgs e)
        {
            bulkCheck = true;

            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            PwEntry[] selectedEntries = mainForm.GetSelectedEntries();

            if (selectedEntries == null)
                return;

            foreach (PwEntry pwEntry in selectedEntries)
            {
                PasswordEntry = pwEntry;

                PasswordEntry.Strings.Remove(PluginOptions.ColumnName);
                Status = null;
                receivedStatus = true;
                TouchEntry(PasswordEntry);
            }

            UpdateUI();
        }

        public void OnMenuHIBPExclude(object sender, EventArgs e)
        {
            bulkCheck = true;

            MainForm mainForm = HIBPOfflineCheckExt.Host.MainWindow;
            PwEntry[] selectedEntries = mainForm.GetSelectedEntries();

            if (selectedEntries == null)
                return;

            foreach (PwEntry pwEntry in selectedEntries)
            {
                PasswordEntry = pwEntry;

                Status = PluginOptions.ExcludedText;
                receivedStatus = true;
                TouchEntry(PasswordEntry);
            }

            UpdateUI();
        }
        
        public void EntrySaved(object sender, EventArgs e)
        {
            PwEntryForm form = sender as PwEntryForm;

            if (PluginOptions.AutoCheck == false)
            {
                return;
            }

            form.EntryRef.Touched -= PwdTouchedHandler;
            form.EntryRef.Touched += PwdTouchedHandler;

            bulkCheck = false;

            form.EntryRef.Touch(true);
        }

        public void ClearEventHandlers(PwEntryForm form)
        {
            form.EntryRef.Touched -= PwdTouchedHandler;

            ResetState();
        }
    }
}
