using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SSLI;

namespace StationSyncroLI
{
    public partial class Form_Settings : Form
    {
        public Form_Settings()
        {
            InitializeComponent();
            this.timer1.Enabled = true;
        }

        private void buttonSetTime_Click_1(object sender, EventArgs e)
        {
            DateTime dtNew = new DateTime(this.dateTimePickerDate.Value.Year, this.dateTimePickerDate.Value.Month, this.dateTimePickerDate.Value.Day);

            Utils.SetSystemTime(dtNew);

            System.Threading.Thread.Sleep(500);

            dtNew = new DateTime(this.dateTimePickerDate.Value.Year, this.dateTimePickerDate.Value.Month, this.dateTimePickerDate.Value.Day,
                            this.dateTimePickerTime.Value.Hour, this.dateTimePickerTime.Value.Minute, this.dateTimePickerTime.Value.Second);

            Utils.SetSystemTime(dtNew);

            this.Close();
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            this.labelTime.Text = DateTime.Now.ToString("HH:mm:ss dd.MM.yyyy");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Utils.TouchScreenCalibrate();

            //Registry.RegFlushKey(Registry.HKLM);
        }

    }
}