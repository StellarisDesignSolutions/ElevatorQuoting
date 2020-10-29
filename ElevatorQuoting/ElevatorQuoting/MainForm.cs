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

            OleDbConnection conn = new OleDbConnection();
            string connection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Stellaris\\ElevatorQuoting\\Databases\\ProgramLogic.accdb";
            string sql = "SELECT * FROM Province_Year";
            conn.ConnectionString = connection;
            conn.Open();
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            OleDbDataReader dr = cmd.ExecuteReader();

            while(dr.Read())
            {
                comboxProvince.Items.Add(dr[0].ToString());
                ProvinceCode.Add(dr[1].ToString());

            }

            dr.Close();
            cmd.Dispose();
            conn.Close();

        }



        void CreateQuoteLocal()
        {

            OleDbConnection conn = new OleDbConnection();
            string connection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Stellaris\\ElevatorQuoting\\Databases\\QuotingLog.accdb";
            string sql = "INSERT INTO Main(QuoteName) VALUES(@QuoteName)";
            conn.ConnectionString = connection;
            conn.Open();
            OleDbCommand cmd = new OleDbCommand(sql, conn);


            cmd.Parameters.Add("@QuoteName", OleDbType.VarChar).Value = txtboxQuoteName.Text;


            cmd.ExecuteNonQuery();

            cmd.Dispose();
            conn.Close();

        }

        void SaveQuoteLocal()
        {

            OleDbConnection conn = new OleDbConnection();
            string connection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\Stellaris\\ElevatorQuoting\\Databases\\QuotingLog.accdb";
            string sql = "UPDATE Main SET LoadType = @LoadType, QuoteDate = @QuoteDate Where QuoteName = @QuoteName";
            conn.ConnectionString = connection;
            conn.Open();
            OleDbCommand cmd = new OleDbCommand(sql, conn);

            //update
            cmd.Parameters.Add("@LoadType", OleDbType.VarChar).Value = comboxLoadType.Text;
            cmd.Parameters.Add("@QuoteDate", OleDbType.DBDate).Value = Convert.ToDateTime(dtpDate.Value.ToShortDateString());

            //where
            cmd.Parameters.Add("@QuoteName", OleDbType.VarChar).Value = txtboxQuoteName.Text;

            cmd.ExecuteNonQuery();

            cmd.Dispose();
            conn.Close();

            MessageBox.Show("Quote Saved");
        }

        private void comboxProvince_SelectedIndexChanged(object sender, EventArgs e)
        {

            txtboxCodeYear.Text = ProvinceCode[comboxProvince.SelectedIndex];

        }
    }
}
