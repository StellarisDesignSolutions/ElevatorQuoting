using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Renci.SshNet;
using Renci.SshNet.Common;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace ElevatorQuoting
{
    public partial class LoadQuote : Form
    {
        //EVENTS
        public delegate void OnLoadingQuoteLocal(object sender, EventArgs e);
        public static event OnLoadingQuoteLocal OnLoadingQuote;

        //Dictionary<int, string> QuoteInformation = new Dictionary<int, string>();
        List<ListViewItem> QuoteList = new List<ListViewItem>();

        public LoadQuote()
        {
            InitializeComponent();
        }

        private void LoadQuote_Load(object sender, EventArgs e)
        {
            //sshConnection(GetQuotes);
            GetQuotes();
            //Tooltip(comboxQuotes);
        }
        /*
        public void Tooltip(ListControl lc)
        {
            foreach (ListViewItem item in lc.)
            {
                item.ToolTipText = QuoteInformation[Convert.ToInt16(item.Text)];
            }
        }
        */
        void sshConnection(Func<Boolean> function)
        {
            PasswordConnectionInfo connectionInfo = new PasswordConnectionInfo("stellarismysql.ddns.net", 7846, "gregyoung", "stellaris"); //replace "192.168.2.52" with "stellarismysql.ddns.net", 7846 for connections from offsite
            connectionInfo.Timeout = TimeSpan.FromSeconds(30);

            using (var client = new SshClient(connectionInfo))
            {
                try
                {
                    Console.WriteLine("Trying SSH connection...");
                    client.Connect();
                    if (client.IsConnected)
                    {
                        Console.WriteLine("SSH connection is active: {0}", client.ConnectionInfo.ToString());
                    }
                    else
                    {
                        Console.WriteLine("SSH connection has failed: {0}", client.ConnectionInfo.ToString());
                    }

                    Console.WriteLine("\r\nTrying port forwarding...");
                    var portFwld = new ForwardedPortLocal("127.0.0.1", 1000, "localhost", 3306);
                    client.AddForwardedPort(portFwld);
                    portFwld.Start();
                    if (portFwld.IsStarted)
                    {
                        Console.WriteLine("Port forwarded: {0}", portFwld.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Port forwarding has failed.");
                    }

                    function();

                    client.Disconnect();

                }
                catch (SshException err)
                {
                    Console.WriteLine("SSH client connection error: {0}", err.Message);
                }
                catch (System.Net.Sockets.SocketException err)
                {
                    Console.WriteLine("Socket connection error: {0}", err.Message);
                }

            }
        }

        Boolean GetQuotes()
        {
            MySqlConnection conn;
            string myConnectionString;

            //myConnectionString = "server=127.0.0.1;port=1000;uid=gregyoung;pwd=[Stellaris03];database=quotinglog;";
            myConnectionString = "server=localhost;port=3306;uid=root;pwd=stellaris;database=quotinglog;";

            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString);

                conn.Open();

                string sqlForQuotesImport = "SELECT * FROM main";

                MySqlCommand cmdForQuotes = new MySqlCommand(sqlForQuotesImport, conn);
                MySqlDataReader readerForImportingQuotes = cmdForQuotes.ExecuteReader();

                while (readerForImportingQuotes.Read())
                {
                    /*
                    comboxQuotes.Items.Add(readerForImportingQuotes[0].ToString());
                    QuoteInformation.Add(Convert.ToInt16(readerForImportingQuotes[0]), readerForImportingQuotes[1].ToString());
                    */
                    ListViewItem temp = new ListViewItem(readerForImportingQuotes[0].ToString());
                    temp.SubItems.Add(readerForImportingQuotes[1].ToString());
                    temp.SubItems.Add(readerForImportingQuotes[2].ToString());
                    temp.SubItems.Add(readerForImportingQuotes[3].ToString());
                    //temp.ToolTipText = readerForImportingQuotes[1].ToString();
                    QuoteList.Add(temp);
                }

                listViewQuotes.Columns.Add("Quote Number", 150, HorizontalAlignment.Left);
                listViewQuotes.Columns.Add("Description", 150, HorizontalAlignment.Left);
                listViewQuotes.Columns.Add("Date", 150, HorizontalAlignment.Left);
                listViewQuotes.Columns.Add("Customer", 150, HorizontalAlignment.Left);

                foreach (ListViewItem item in QuoteList)
                {
                    listViewQuotes.Items.Add(item);
                    //item.ToolTipText = "Test";
                }

                readerForImportingQuotes.Close();
                cmdForQuotes.Dispose();

                ////////


                conn.Close();

                return true;

            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        Boolean LoadQuotes()
        {
            MySqlConnection conn;
            string myConnectionString;

            //myConnectionString = "server=127.0.0.1;port=1000;uid=gregyoung;pwd=[Stellaris03];database=quotinglog;";
            myConnectionString = "server=localhost;port=3306;uid=root;pwd=stellaris;database=quotinglog;";
            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString);

                conn.Open();

                string sqlForQuotesImport = "SELECT * FROM main WHERE QuoteName = " + Quote.QuoteNumber;

                MySqlCommand cmdForQuotes = new MySqlCommand(sqlForQuotesImport, conn);
                MySqlDataReader readerForImportingQuotes = cmdForQuotes.ExecuteReader();
                readerForImportingQuotes.Read();

                Quote.ProjectDescription = readerForImportingQuotes[1].ToString();
                Quote.ProjectCustomer = readerForImportingQuotes[3].ToString();
                Quote.ProjectContact = readerForImportingQuotes[4].ToString();
                Quote.ProjectProvince = readerForImportingQuotes[5].ToString();

                UserInputs.LoadType = readerForImportingQuotes[6].ToString();
                UserInputs.PitDepth = Convert.ToDecimal(readerForImportingQuotes[7]);
                UserInputs.TravelDistance = Convert.ToDecimal(readerForImportingQuotes[8]);
                UserInputs.OverheadClearance = Convert.ToDecimal(readerForImportingQuotes[9]);
                UserInputs.Floors = Convert.ToInt16(readerForImportingQuotes[10]);
                UserInputs.TravelSpeed = Convert.ToDecimal(readerForImportingQuotes[11]);
                UserInputs.PlatformWidth = Convert.ToDecimal(readerForImportingQuotes[12]);
                UserInputs.PlatformLength = Convert.ToDecimal(readerForImportingQuotes[13]);
                UserInputs.InlineThrough = readerForImportingQuotes[14].ToString();
                UserInputs.Capacity = Convert.ToDecimal(readerForImportingQuotes[15]);
                UserInputs.CylinderSelection = Convert.ToInt16(readerForImportingQuotes[16]);
                UserInputs.NumberOfCylinders = Convert.ToInt16(readerForImportingQuotes[17]);
                UserInputs.MetricUnits = Convert.ToBoolean(readerForImportingQuotes[18]);

                readerForImportingQuotes.Close();
                cmdForQuotes.Dispose();

                ////////

                conn.Close();

                return true;

            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            //Quote.Reset();
            Quote.QuoteNumber = Convert.ToInt16(listViewQuotes.SelectedItems[0].Text);
            //sshConnection(LoadQuotes);
            LoadQuotes();
            OnLoadingQuote(sender, e);
            this.Hide();
        }
    }
}
