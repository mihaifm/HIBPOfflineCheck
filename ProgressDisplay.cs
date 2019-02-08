using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HIBPOfflineCheck
{
    public partial class ProgressDisplay : Form
    {
        public bool UserTerminated { get; set; }

        public ProgressDisplay()
        {
            InitializeComponent();
            UserTerminated = false;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            UserTerminated = true;
            this.Close();
        }

        private void ProgressDisplay_FormClosing(object sender, FormClosingEventArgs e)
        {
            UserTerminated = true;
        }
    }
}
