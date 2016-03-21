using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cello
{
    public partial class DifferentialForm : Form
    {
        public DifferentialForm()
        {
            InitializeComponent();
        }

        private void DifferentialForm_Load(object sender, EventArgs e)
        {

        }

        public bool WriteLine(string s)
        {
            if (this.Created)
            {
                BeginInvoke((Action<string>)((str) =>
                    {
                        try
                        {
                            OutputTextBox.AppendText(str + "\r\n");
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString());
                        }
                    }), s);
                return true;
            }
            else
                return false;
        }

    }
}
