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
        private Options _options;

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

            _options = ext.LoadOptions();

            textBoxFileName.Text = _options.HIBPFileName;
            textBoxColumnName.Text = _options.ColumnName;
            textBoxSecureText.Text = _options.SecureText;
            textBoxInsecureText.Text = _options.InsecureText;
            checkBoxBreachCountDetails.Checked = _options.BreachCountDetails;
            checkBoxWarningDialog.Checked = _options.WarningDialog;
            textBoxWarningDialog.Text = _options.WarningDialogText;
            textBoxWarningDialog.Enabled = checkBoxWarningDialog.Checked;

            textBoxFileName.Select();
            textBoxFileName.Select(0, 0);

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            _options.HIBPFileName = textBoxFileName.Text;
            _options.ColumnName = textBoxColumnName.Text;
            _options.SecureText = textBoxSecureText.Text;
            _options.InsecureText = textBoxInsecureText.Text;
            _options.BreachCountDetails = checkBoxBreachCountDetails.Checked;
            _options.WarningDialog = checkBoxWarningDialog.Checked;
            _options.WarningDialogText = textBoxWarningDialog.Text;

            var standardFields = PwDefs.GetStandardFields();

            foreach (string key in standardFields)
            {
                if (key == _options.ColumnName)
                {
                    MessageBox.Show("Column name conflicts with KeePass columns", 
                        " Invalid column name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            ext.SaveOptions(_options);
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
    }
}
