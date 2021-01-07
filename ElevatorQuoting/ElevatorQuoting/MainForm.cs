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
                string sql = "SELECT * FROM province_year";
                conn.Open();

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    comboxProvince.Items.Add(dr[0].ToString());
                    ProvinceCode.Add(dr[1].ToString());

                }

                dr.Close();
                cmd.Dispose();
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
            }
            else
            {
                newUnitLabel = "ft";
            }

            labelUnit1.Text = newUnitLabel;
            labelUnit2.Text = newUnitLabel;
            labelUnit3.Text = newUnitLabel;
            labelUnit4.Text = newUnitLabel;
            labelUnit5.Text = newUnitLabel;
            labelSpeedUnit.Text = newUnitLabel  + "/s";
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
        }
    }
}
