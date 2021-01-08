using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using MySql.Data;
using MySql.Data.MySqlClient;


namespace ElevatorQuoting
{
    public partial class MainForm : Form
    {
        List<string> ProvinceCode = new List<string>();
        List<decimal> materialDensitiesMetric = new List<decimal>();
        List<decimal> materialDensitiesImperial = new List<decimal>();
        List<decimal> cylinderEffectiveAreas = new List<decimal>();
        Dictionary<string, int> metricCapacityValues = new Dictionary<string, int>();
        Dictionary<string, int> imperialCapacityValues = new Dictionary<string, int>();

        Boolean unitsAreMetric = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            MessageBox.Show(dtpDate.Value.ToShortDateString());

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            LogicLoad();
            comboxUnits.SelectedIndex = 0;

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                CreateQuoteLocal();
            }
            catch
            {

            }
            SaveQuoteLocal();

        }
        
        void LogicLoad()
        {


            MySqlConnection conn;
            string myConnectionString;

            myConnectionString = "server=127.0.0.1;uid=root;pwd=stellaris;database=programlogic;";

            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();

                string sql = "SELECT * FROM province_year";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    comboxProvince.Items.Add(dr[0].ToString());
                    ProvinceCode.Add(dr[1].ToString());

                }

                dr.Close();
                cmd.Dispose();


                string sqlForCapacityImport = "SELECT * FROM class_loading";

                MySqlCommand cmdForImportingCapacities = new MySqlCommand(sqlForCapacityImport, conn);
                MySqlDataReader readerForImportingCapacities = cmdForImportingCapacities.ExecuteReader();

                while (readerForImportingCapacities.Read())
                {
                    metricCapacityValues.Add(readerForImportingCapacities[0].ToString(), Convert.ToInt32(readerForImportingCapacities[1]));
                    imperialCapacityValues.Add(readerForImportingCapacities[0].ToString(), Convert.ToInt32(readerForImportingCapacities[2]));
                }

               
                readerForImportingCapacities.Close();
                cmdForImportingCapacities.Dispose();

                ////////
                string sqlForMaterialsImport = "SELECT * FROM materials";

                MySqlCommand cmdForImportingMaterials = new MySqlCommand(sqlForMaterialsImport, conn);
                MySqlDataReader readerForImportingMaterials = cmdForImportingMaterials.ExecuteReader();

                while (readerForImportingMaterials.Read())
                {
                    comboxMaterials.Items.Add(readerForImportingMaterials[0].ToString());
                    materialDensitiesMetric.Add(Convert.ToDecimal(readerForImportingMaterials[1]));
                    materialDensitiesImperial.Add(Convert.ToDecimal(readerForImportingMaterials[2]));
                }

                comboxMaterials.SelectedIndex = 0;

                readerForImportingMaterials.Close();
                cmdForImportingMaterials.Dispose();
                //////

                ////////
                string sqlForCylindersImport = "SELECT * FROM cylinder_catalogue";

                MySqlCommand cmdForImportingCylinders = new MySqlCommand(sqlForCylindersImport, conn);
                MySqlDataReader readerForImportingCylinders = cmdForImportingCylinders.ExecuteReader();

                while (readerForImportingCylinders.Read())
                {
                    comboxCylinders.Items.Add(readerForImportingCylinders[0].ToString());
                    cylinderEffectiveAreas.Add(Convert.ToDecimal(readerForImportingCylinders[1]));
                }

                comboxMaterials.SelectedIndex = 0;

                readerForImportingMaterials.Close();
                cmdForImportingMaterials.Dispose();
                //////

                conn.Close();


            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }

            //Below is Access Connection
            //OleDbConnection conn = new OleDbConnection();
            //string connection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Stellaris\\ElevatorQuoting\\Databases\\ProgramLogic.accdb";
            //string sql = "SELECT * FROM Province_Year";
            //conn.ConnectionString = connection;
            //conn.Open();
            //OleDbCommand cmd = new OleDbCommand(sql, conn);
            //OleDbDataReader dr = cmd.ExecuteReader();

            //while(dr.Read())
            //{
            //    comboxProvince.Items.Add(dr[0].ToString());
            //    ProvinceCode.Add(dr[1].ToString());

            //}

            //dr.Close();
            //cmd.Dispose();
            //conn.Close();

        }



        void CreateQuoteLocal()
        {


            MySqlConnection conn;
            string myConnectionString;

            myConnectionString = "server=127.0.0.1;uid=root;pwd=stellaris;database=quotinglog;";

            conn = new MySql.Data.MySqlClient.MySqlConnection();
            conn.ConnectionString = myConnectionString;
            string sql = "INSERT INTO main(QuoteName) VALUES(@QuoteName)";
            conn.Open();

            MySqlCommand cmd = new MySqlCommand(sql, conn);


            cmd.Parameters.Add("@QuoteName", MySqlDbType.VarChar).Value = txtboxQuoteName.Text;


            cmd.ExecuteNonQuery();

            cmd.Dispose();
            conn.Close();


            //OleDbConnection conn = new OleDbConnection();
            //string connection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Stellaris\\ElevatorQuoting\\Databases\\QuotingLog.accdb";
            //string sql = "INSERT INTO Main(QuoteName) VALUES(@QuoteName)";
            //conn.ConnectionString = connection;
            //conn.Open();
            //OleDbCommand cmd = new OleDbCommand(sql, conn);


            //cmd.Parameters.Add("@QuoteName", OleDbType.VarChar).Value = txtboxQuoteName.Text;


            //cmd.ExecuteNonQuery();

            //cmd.Dispose();
            //conn.Close();

        }

        void SaveQuoteLocal()
        {

            MySqlConnection conn;
            string myConnectionString;

            myConnectionString = "server=127.0.0.1;uid=root;pwd=stellaris;database=quotinglog;";

            conn = new MySql.Data.MySqlClient.MySqlConnection();
            conn.ConnectionString = myConnectionString;

            string sql = "UPDATE main SET LoadType = @LoadType, QuoteDate = @QuoteDate Where QuoteName = @QuoteName";

            conn.Open();

            MySqlCommand cmd = new MySqlCommand(sql, conn);

            //update
            cmd.Parameters.Add("@LoadType", MySqlDbType.VarChar).Value = comboxLoadType.Text;
            cmd.Parameters.Add("@QuoteDate", MySqlDbType.Date).Value = Convert.ToDateTime(dtpDate.Value.ToShortDateString());

            //where
            cmd.Parameters.Add("@QuoteName", MySqlDbType.VarChar).Value = txtboxQuoteName.Text;

            cmd.ExecuteNonQuery();

            cmd.Dispose();
            conn.Close();



            //OleDbConnection conn = new OleDbConnection();
            //string connection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Stellaris\\ElevatorQuoting\\Databases\\QuotingLog.accdb";
            //string sql = "UPDATE Main SET LoadType = @LoadType, QuoteDate = @QuoteDate Where QuoteName = @QuoteName";
            //conn.ConnectionString = connection;
            //conn.Open();
            //OleDbCommand cmd = new OleDbCommand(sql, conn);

            ////update
            //cmd.Parameters.Add("@LoadType", OleDbType.VarChar).Value = comboxLoadType.Text;
            //cmd.Parameters.Add("@QuoteDate", OleDbType.DBDate).Value = Convert.ToDateTime(dtpDate.Value.ToShortDateString());

            ////where
            //cmd.Parameters.Add("@QuoteName", OleDbType.VarChar).Value = txtboxQuoteName.Text;

            //cmd.ExecuteNonQuery();

            //cmd.Dispose();
            //conn.Close();

            MessageBox.Show("Quote Saved");
        }

        void setUnits(string newUnits)
        {
            string newUnitLabel = null;

            if (newUnits == "Metric")
            {
                newUnitLabel = "m";
                unitsAreMetric = true;
            }
            else
            {
                newUnitLabel = "ft";
                unitsAreMetric = false;
            }

            labelUnit1.Text = newUnitLabel;
            labelUnit2.Text = newUnitLabel;
            labelUnit3.Text = newUnitLabel;
            labelUnit4.Text = newUnitLabel;
            labelUnit5.Text = newUnitLabel;
            labelUnit6.Text = newUnitLabel;
            labelSpeedUnit.Text = newUnitLabel  + "/s";
        }

        void calculateCapacity()
        {

            if (isThisStringANumber(txtboxPlatformWidth.Text) && isThisStringANumber(txtboxPlatformLength.Text))
            {
                
                decimal platformWidth = decimal.Parse(txtboxPlatformWidth.Text);
                decimal platformLength = decimal.Parse(txtboxPlatformLength.Text);
                decimal platformArea = platformLength * platformWidth;
                
                //Class A Capacity//
                decimal platformClassACapacity = (unitsAreMetric ? metricCapacityValues["A"] : imperialCapacityValues["A"]) * platformArea;
                textboxCapacityClassA.Text = string.Format("{0}", platformClassACapacity);
                ////////////////////

                //Class B Capacity//
                decimal platformClassBCapacity = (unitsAreMetric ? metricCapacityValues["B"] : imperialCapacityValues["B"]) * platformArea;
                textboxCapacityClassB.Text = string.Format("{0}", platformClassBCapacity);
                ////////////////////
                
                //Class C Capacity//
                decimal platformClassCCapacity = (unitsAreMetric ? metricCapacityValues["C1"] : imperialCapacityValues["C1"]) * platformArea;
                textboxCapacityClassC.Text = string.Format("{0}", platformClassCCapacity);
                ////////////////////
            }
            else
            {
                textboxCapacityClassA.Text = "Invalid";
                textboxCapacityClassB.Text = "Invalid";
                textboxCapacityClassC.Text = "Invalid";
            }
        }

        void calculatePlatformMass()
        {
            if (isThisStringANumber(txtboxPlatformThickness.Text) && isThisStringANumber(txtboxPlatformWidth.Text) && isThisStringANumber(txtboxPlatformLength.Text))
            {
                decimal platformWidth = decimal.Parse(txtboxPlatformWidth.Text);
                decimal platformLength = decimal.Parse(txtboxPlatformLength.Text);
                decimal platformThickness = decimal.Parse(txtboxPlatformThickness.Text);
                decimal platformVolume = platformWidth * platformLength * platformThickness;



                decimal materialDensity = 1;

                decimal conversionFactor = 1;

                if (unitsAreMetric)
                {
                    materialDensity = materialDensitiesMetric[comboxMaterials.SelectedIndex];
                }
                else
                {
                    materialDensity = materialDensitiesImperial[comboxMaterials.SelectedIndex];
                    conversionFactor = 1728;
                }

                decimal platformMass = materialDensity * platformVolume * conversionFactor;

                txtboxPlatformMass.Text = string.Format("{0}", platformMass);
            }
            else
            {
                txtboxPlatformMass.Text = "Invalid";
            }
        }

        Boolean isThisStringANumber(string numberToCheck)
        {
            if (decimal.TryParse(numberToCheck, out decimal d)){
                return true;
            }
            else
            {
                return false;
            }
        }

        private void comboxProvince_SelectedIndexChanged(object sender, EventArgs e)
        {

            txtboxCodeYear.Text = ProvinceCode[comboxProvince.SelectedIndex];

        }

        private void txtboxTravelDis_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboxUnits_SelectedIndexChanged(object sender, EventArgs e)
        {
            setUnits(comboxUnits.Items[comboxUnits.SelectedIndex].ToString());
            calculateCapacity();
            calculatePlatformMass();
        }

        private void txtboxPlatformWidth_TextChanged(object sender, EventArgs e)
        {
            calculateCapacity();
            calculatePlatformMass();
        }

        private void txtboxPlatformLength_TextChanged(object sender, EventArgs e)
        {
            calculateCapacity();
            calculatePlatformMass();
        }

        private void txtboxPlatformThickness_TextChanged(object sender, EventArgs e)
        {
            calculatePlatformMass();
        }

        private void comboxMaterials_SelectedIndexChanged(object sender, EventArgs e)
        {
            calculatePlatformMass();
        }
    }
}
