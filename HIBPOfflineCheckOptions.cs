using KeePass.UI;
using KeePass.App;
using KeePassLib;
using System;
using System.Windows.Forms;
using static HIBPOfflineCheck.Options;

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

        private void HIBPOfflineCheckOptions_Load(object sender, EventArgs e)
        {
            Icon = AppIcons.Default;

            pb_BannerImage.Image = BannerFactory.CreateBanner(pb_BannerImage.Width, pb_BannerImage.Height, 
                BannerStyle.Default, Properties.Resources.B48x48_KOrganizer, 
                "HIBP Offline Check Options", "Manage plugin settings.");

            options = ext.LoadOptions();

            radioButtonOffline.Checked = (options.CheckMode == CheckModeType.Offline);
            radioButtonOnline.Checked = (options.CheckMode == CheckModeType.Online);
            textBoxFileName.Text = options.HIBPFileName;
            textBoxColumnName.Text = options.ColumnName;
            textBoxSecureText.Text = options.SecureText;
            textBoxInsecureText.Text = options.InsecureText;
            checkBoxBreachCountDetails.Checked = options.BreachCountDetails;
            checkBoxWarningDialog.Checked = options.WarningDialog;
            textBoxWarningDialog.Text = options.WarningDialogText;
            textBoxWarningDialog.Enabled = checkBoxWarningDialog.Checked;

            textBoxFileName.Select();
            textBoxFileName.Select(0, 0);

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            options.CheckMode = radioButtonOffline.Checked ? CheckModeType.Offline : CheckModeType.Online;
            options.HIBPFileName = textBoxFileName.Text;
            options.ColumnName = textBoxColumnName.Text;
            options.SecureText = textBoxSecureText.Text;
            options.InsecureText = textBoxInsecureText.Text;
            options.BreachCountDetails = checkBoxBreachCountDetails.Checked;
            options.WarningDialog = checkBoxWarningDialog.Checked;
            options.WarningDialogText = textBoxWarningDialog.Text;

            var standardFields = PwDefs.GetStandardFields();

            foreach (string key in standardFields)
            {
                if (key == options.ColumnName)
                {
                    MessageBox.Show("Column name conflicts with KeePass columns", 
                        " Invalid column name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            ext.SaveOptions(options);
            Close();
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
    }
}
