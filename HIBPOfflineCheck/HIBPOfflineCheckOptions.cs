using KeePass.UI;
using KeePass.App;
using KeePassLib;
using System;
using System.Windows.Forms;

namespace HIBPOfflineCheck
{
    public partial class HIBPOfflineCheckOptions : Form
    {
        private HIBPOfflineCheckExt ext;
        private Options options;

        public HIBPOfflineCheckOptions(HIBPOfflineCheckExt ext)
        {
            this.ext = ext;
            InitializeComponent();
        }

        private bool CommitOptions()
        {
            options.CheckMode = radioButtonOffline.Checked ?
                Options.CheckModeType.Offline : radioButtonOnline.Checked ?
                Options.CheckModeType.Online : Options.CheckModeType.BloomFilter;

            options.HIBPFileName = textBoxFileName.Text;
            options.ColumnName = textBoxColumnName.Text;
            options.SecureText = textBoxSecureText.Text;
            options.InsecureText = textBoxInsecureText.Text;
            options.BreachCountDetails = checkBoxBreachCountDetails.Checked;
            options.WarningDialog = checkBoxWarningDialog.Checked;
            options.WarningDialogText = textBoxWarningDialog.Text;

            bool bloomFilterChanged = (options.BloomFilter != textBoxBloomFilter.Text);
            options.BloomFilter = textBoxBloomFilter.Text;

            var standardFields = PwDefs.GetStandardFields();

            foreach (string key in standardFields)
            {
                if (key == options.ColumnName)
                {
                    MessageBox.Show("Column name conflicts with KeePass columns",
                        " Invalid column name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            if (bloomFilterChanged)
            {
                ext.Prov.BloomFilter = null;
            }

            ext.SaveOptions(options);

            return true;
        }

        private void HIBPOfflineCheckOptions_Load(object sender, EventArgs e)
        {
            Icon = AppIcons.Default;

            pb_BannerImage.Image = BannerFactory.CreateBanner(pb_BannerImage.Width, pb_BannerImage.Height, 
                BannerStyle.Default, Properties.Resources.B48x48_KOrganizer, 
                "HIBP Offline Check Options", "Manage plugin settings.");

            options = ext.LoadOptions();

            radioButtonOffline.Checked = (options.CheckMode == Options.CheckModeType.Offline);
            radioButtonOnline.Checked = (options.CheckMode == Options.CheckModeType.Online);
            radioButtonBloom.Checked = (options.CheckMode == Options.CheckModeType.BloomFilter);
            textBoxFileName.Text = options.HIBPFileName;
            textBoxColumnName.Text = options.ColumnName;
            textBoxSecureText.Text = options.SecureText;
            textBoxInsecureText.Text = options.InsecureText;
            checkBoxBreachCountDetails.Checked = options.BreachCountDetails;
            checkBoxWarningDialog.Checked = options.WarningDialog;
            textBoxWarningDialog.Text = options.WarningDialogText;
            textBoxWarningDialog.Enabled = checkBoxWarningDialog.Checked;
            textBoxBloomFilter.Text = options.BloomFilter;
            textBoxBloomFilter.Enabled = radioButtonBloom.Checked;
            buttonCreateBloom.Enabled = radioButtonBloom.Checked;
            buttonBrowseBloom.Enabled = radioButtonBloom.Checked;

            textBoxFileName.Select();
            textBoxFileName.Select(0, 0);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (CommitOptions())
            {
                Close();
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxFileName.Text = dialog.FileName;
            }
        }

        private void checkBoxWarningDialog_CheckedChanged(object sender, EventArgs e)
        {
            textBoxWarningDialog.Enabled = checkBoxWarningDialog.Checked;
        }

        private void radioButtonOffline_CheckedChanged(object sender, EventArgs e)
        {
            textBoxFileName.Enabled = radioButtonOffline.Checked;
        }

        private void buttonCheckAll_Click(object sender, EventArgs e)
        {
            if (CommitOptions())
            {
                ext.Prov.CheckAll();
            }
        }

        private void buttonClearAll_Click(object sender, EventArgs e)
        {
            if (CommitOptions())
            {
                ext.Prov.ClearAll();
            }
        }

        private void buttonCreateBloom_Click(object sender, EventArgs e)
        {
            if (CommitOptions())
            {
                var createBloomForm = new CreateBloomFilter(ext);
                createBloomForm.Show();
            }
        }

        private void buttonBrowseBloom_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBoxBloomFilter.Text = dialog.FileName;
            }
        }

        private void radioButtonBloom_CheckedChanged(object sender, EventArgs e)
        {
            textBoxBloomFilter.Enabled = radioButtonBloom.Checked;
            buttonCreateBloom.Enabled = radioButtonBloom.Checked;
            buttonBrowseBloom.Enabled = radioButtonBloom.Checked;
        }
    }
}
