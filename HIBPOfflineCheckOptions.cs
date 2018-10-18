using KeePass.App;
using KeePass.Plugins;
using KeePassLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
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

        private void HIBPOfflineCheckOptions_Load(object sender, EventArgs e)
        {
            this.Icon = AppIcons.Default;
            
            options = ext.LoadOptions();

            textBoxFileName.Text = options.HIBPFileName;
            textBoxColumnName.Text = options.ColumnName;
            textBoxSecureText.Text = options.SecureText;
            textBoxInsecureText.Text = options.InsecureText;
            checkBoxBreachCountDetails.Checked = options.BreachCountDetails;
            checkBoxWarningDialog.Checked = options.WarningDialog;

            textBoxFileName.Select();
            textBoxFileName.Select(0, 0);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            options.HIBPFileName = textBoxFileName.Text;
            options.ColumnName = textBoxColumnName.Text;
            options.SecureText = textBoxSecureText.Text;
            options.InsecureText = textBoxInsecureText.Text;
            options.BreachCountDetails = checkBoxBreachCountDetails.Checked;
            options.WarningDialog = checkBoxWarningDialog.Checked;

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
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
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
    }
}
