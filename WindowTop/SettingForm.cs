using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowTop
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();
        }

        public List<Keys> KeyList = new List<Keys>();
        
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            Console.Clear();

            Console.WriteLine($"KeyData:{e.KeyData}");
            Console.WriteLine($"KeyCode:{e.KeyCode}");
            Console.WriteLine($"Alt:{e.Alt}");
            Console.WriteLine($"Control:{e.Control}");
            Console.WriteLine($"Shift:{e.Shift}");
            Console.WriteLine($"Modifiers:{e.Modifiers}");
            //Console.WriteLine($"commbo:{Keys.Y | Keys.U | Keys.A | Keys.N}");

            textBox1.Text = "";
            KeyList.Clear();
            if (e.Control)
            {
                KeyList.Add(Keys.Control);
            }
            if (e.Alt)
            {
                KeyList.Add(Keys.Alt);
            }
            if (e.Shift)
            {
                KeyList.Add(Keys.Shift);
            }

            if (e.Control || e.Alt ||  e.Shift)
            {
                if (e.KeyCode == Keys.ControlKey ||
                   e.KeyCode == Keys.ShiftKey ||
                   e.KeyCode == Keys.Menu)
                    return;
                KeyList.Add(e.KeyCode);
                UpdateTextBoxDispaly();
            }

        }


        void UpdateTextBoxDispaly()
        {
            for (int i = 0; i < KeyList.Count; i++)
            {
                textBox1.Text += KeyList[i];
                if(i != KeyList.Count - 1)
                {
                    textBox1.Text += " + ";
                }
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {

        }
    }
}
