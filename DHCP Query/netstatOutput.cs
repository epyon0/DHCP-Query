using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DHCP_Query
{
    public partial class netstatOutput : Form
    {
        public netstatOutput(string dest, string input)
        {
            InitializeComponent();
            this.Text = $"NETWORK INFO: {dest}";
            textBox1.Text = input;
        }

        private void netstatOutput_Load(object sender, EventArgs e)
        {

        }

        private void netstatOutput_Shown(object sender, EventArgs e)
        {
            this.BringToFront();
            this.Focus();
        }
    }
}
