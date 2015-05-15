using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
        }

        private SasServer activeSession = null;
        private string spth = "";

        public SasServer cs
        {
            get { return activeSession; }
            set { activeSession = value; }
        }

        public string pth
        {
            get { return spth; }
            set { spth = value; }
        
        }

 
        private void button1_Click(object sender, EventArgs e)
        {
            if (activeSession.IsConnected)
            {
                string chmod = calc_o() + calc_g() + calc_u();
                string statment = "x chmod " + chmod + " " + spth + ";";
                //MessageBox.Show(statment);

               activeSession.Workspace.LanguageService.Submit(statment);

               //File.WriteAllText(@"d:\data\541395034\Desktop\Work\test.txt", activeSession.Workspace.LanguageService.FlushLog(100000));

               this.Close();
            }
            else { MessageBox.Show("Something wrong."); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private string calc_o()
        {
            int r,w,x;
            if (checkBox1.Checked) r = 4; else r = 0;
            if (checkBox4.Checked) w = 2; else w = 0;
            if (checkBox7.Checked) x = 1; else x = 0;
            return (r + w + x).ToString();
        }

        private string calc_u()
        {
            int r, w, x;
            if (checkBox3.Checked) r = 4; else r = 0;
            if (checkBox6.Checked) w = 2; else w = 0;
            if (checkBox9.Checked) x = 1; else x = 0;
            return (r + w + x).ToString();
        }

        private string calc_g()
        {
            int r, w, x;
            if (checkBox2.Checked) r = 4; else r = 0;
            if (checkBox5.Checked) w = 2; else w = 0;
            if (checkBox8.Checked) x = 1; else x = 0;
            return (r + w + x).ToString();
        }
    }
}
