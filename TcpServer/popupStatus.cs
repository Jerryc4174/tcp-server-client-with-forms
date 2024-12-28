using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TcpServer
{
    public partial class popupStatus : Form
    {
        public popupStatus()
        {
            InitializeComponent();
        }

        public void SetDtb1Text(string text)
        {
            dataTextBox1.Text = text;
            dataTextBox1.Refresh();
        }

        public void SetDtb2Text(string text)
        {
            dataTextBox2.Text = text;
            dataTextBox2.Refresh();
        }

        public void SetDtb3Text(string text)
        {
            dataTextBox3.Text = text;
            dataTextBox3.Refresh();
        }

        public void SetDtb4Text(string text)
        {
            dataTextBox4.Text = text;
            dataTextBox4.Refresh();
        }

        public void SetDtb5Text(string text)
        {
            dataTextBox5.Text = text;
            dataTextBox5.Refresh();
        }

        public void SetDtb6Text(string text)
        {
            dataTextBox6.Text = text;
            dataTextBox6.Refresh();
        }

        public void SetDtb7Text(string text)
        {
            dataTextBox7.Text = text;
            dataTextBox7.Refresh();
        }

        public void SetDtb8Text(string text)
        {
            dataTextBox8.Text = text;
            dataTextBox8.Refresh();
        }

        public void SetDtb9Text(string text)
        {
            dataTextBox9.Text = text;
            dataTextBox9.Refresh();
        }

        public void SetDtb10Text(string text)
        {
            dataTextBox10.Text = text;
            dataTextBox10.Refresh();
        }

        public void SetCB1(bool value)
        {
            checkBox1.Checked = value;
            checkBox1.Refresh();
        }

        public void SetCB2(bool value)
        {
            checkBox2.Checked = value;
            checkBox2.Refresh();
        }

        public void SetCB3(bool value)
        {
            checkBox3.Checked = value;
            checkBox3.Refresh();
        }

        public void SetCB4(bool value)
        {
            checkBox4.Checked = value;
            checkBox4.Refresh();
        }

        public void SetCB5(bool value)
        {
            checkBox5.Checked = value;
            checkBox5.Refresh();
        }

        public void SetCB6(bool value)
        {
            checkBox6.Checked = value;
            checkBox6.Refresh();
        }

        public void SetCB7(bool value)
        {
            checkBox7.Checked = value;
            checkBox7.Refresh();
        }

        public void SetCB8(bool value)
        {
            checkBox8.Checked = value;
            checkBox8.Refresh();
        }

        public void SetCB9(bool value)
        {
            checkBox9.Checked = value;
            checkBox9.Refresh();

        }

        public void SetCB10(bool value)
        {
            checkBox10.Checked = value;
            checkBox10.Refresh();
        }

        public void SetCB11(bool value)
        {
            checkBox11.Checked = value;
            checkBox11.Refresh();
        }

        public void SetCB12(bool value)
        {
            checkBox12.Checked = value;
            checkBox12.Refresh();
        }

        public void SetCB13(bool value)
        {
            checkBox13.Checked = value;
            checkBox13.Refresh();
        }

        public void SetCB14(bool value)
        {
            checkBox14.Checked = value;
            checkBox14.Refresh();
        }

        public void SetCB15(bool value)
        {
            checkBox15.Checked = value;
            checkBox15.Refresh();
        }

        public void SetCB16(bool value)
        {
            checkBox16.Checked = value;
            checkBox16.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
