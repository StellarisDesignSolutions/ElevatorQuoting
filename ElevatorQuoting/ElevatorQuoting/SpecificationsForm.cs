using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ElevatorQuoting
{
    public partial class SpecificationsForm : Form
    {
        public SpecificationsForm()
        {
            InitializeComponent();
        }

        private void SpecificationsForm_Load(object sender, EventArgs e)
        {
            updateValues();
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            updateValues();
        }

        private void updateValues()
        {
            txtboxCodeYear.Text = Lift.ClassYear;
            txtboxClass.Text = Lift.LoadingClass;
            txtboxPlatformMass.Text = string.Format("{0,4:.00}", Lift.PlatformMass);
            txtboxMinCapacity.Text = string.Format("{0,4:.00}", Lift.MinCapacity);
            txtboxRequiredCapacity.Text = string.Format("{0,4:.00}", Lift.RequiredCapacity);
            txtboxEmptyPlatformStatic.Text = string.Format("{0,4:.00}", Lift.EmptyStaticPressure);
            txtboxEmptyPlatformDynamic.Text = string.Format("{0,4:.00}", Lift.EmptyDynamicPressure);
            txtboxFullLoadStatic.Text = string.Format("{0,4:.00}", Lift.FullStaticPressure);
            txtboxFullLoadDynamic.Text = string.Format("{0,4:.00}", Lift.FullDynamicPressure);

        }

        private void buttonDXF_Click(object sender, EventArgs e)
        {
            MainForm.TestCreate(MainForm.unitsAreMetric, MainForm.dxfStartX, MainForm.dxfStartY);
        }
    }
}
