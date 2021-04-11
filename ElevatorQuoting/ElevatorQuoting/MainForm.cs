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
using netDxf;
using netDxf.Entities;
using netDxf.IO;
// SSH
using Renci.SshNet;
using Renci.SshNet.Common;


/// <summary>
/// Formatting decimals for nice output:
/// string.Format("{0,4:.00}", platformMass)
/// </summary>

namespace ElevatorQuoting
{
    public partial class MainForm : Form
    {
        //public Lift newLift = new Lift();

        List<string> ProvinceCode = new List<string>();
        List<decimal> materialDensitiesMetric = new List<decimal>();
        List<decimal> materialDensitiesImperial = new List<decimal>();
        List<decimal> cylinderEffectiveAreasMetric = new List<decimal>();
        List<decimal> cylinderEffectiveAreasImperial = new List<decimal>();
        Dictionary<string, int> metricCapacityValues = new Dictionary<string, int>();
        Dictionary<string, int> imperialCapacityValues = new Dictionary<string, int>();

        List<Customer> customers = new List<Customer>();

        int shifted = 0;

        int dxfStartX = 200; //inch
        int dxfStartY = 200; //inch

        const decimal poundsPerKilogram = 2.20462M;
        const decimal acceleration = 9.80665M;

        const decimal maxOperatingPressure = 1200; //This value will be moved to a standards database//
        const decimal pitDepthThreshold = 0.666M;
        const decimal massPerSqMDeepPit = 317.5378M;
        const decimal massPerSqFtDeepPit = 65;
        const decimal massPerSqMShallowPit = 1;
        const decimal massPerSqFtShallowPit = 1;

        Boolean pitDepthThresholdMet;
        Boolean unitsAreMetric = false;

        public MainForm()
        {
            InitializeComponent();
        }

        // loading subs

        private void MainForm_Load(object sender, EventArgs e)
        {
            sshConnection();
            //LogicLoad();  //migrated to sshConnection function
            //comboxUnits.SelectedIndex = 0;
            //comboxCylinders.SelectedIndex = 0;
            //comboxNumberOfCylinders.SelectedIndex = 0;
            //comboxProvince.SelectedIndex = 8;

        }
        void sshConnection()
        {
            PasswordConnectionInfo connectionInfo = new PasswordConnectionInfo("192.168.2.52", "gregyoung", "stellaris"); //replace "192.168.2.52" with "stellarismysql.ddns.net", 7846 for connections from offsite
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

                    LogicLoad();

                    client.Disconnect();

                }
                catch (SshException e)
                {
                    Console.WriteLine("SSH client connection error: {0}", e.Message);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    Console.WriteLine("Socket connection error: {0}", e.Message);
                }

            }

            //Console.WriteLine("\r\nTrying database connection...");
            //DBConnect dbConnect = new DBConnect("localhost", "test_database", "root", "passwrod123", "4479");

            //var ct = dbConnect.Count("packages");
            //Console.WriteLine(ct.ToString());
        }
        void LogicLoad()
        {


            MySqlConnection conn;
            string myConnectionString;

            myConnectionString = "server=127.0.0.1;port=1000;uid=gregyoung;pwd=[Stellaris03];database=programlogic;";

            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString);
                //conn.ConnectionString = myConnectionString;
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
                    cylinderEffectiveAreasMetric.Add(Convert.ToDecimal(readerForImportingCylinders[1]));
                    cylinderEffectiveAreasImperial.Add(Convert.ToDecimal(readerForImportingCylinders[2]));
                }

                comboxMaterials.SelectedIndex = 0;

                readerForImportingCylinders.Close();
                cmdForImportingCylinders.Dispose();
                //////

                //////


                MySqlConnection conn2 = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString);
                conn2.Open();

                string sqlForCustomers = "SELECT * FROM customer_information";

                MySqlCommand cmdForCustomers = new MySqlCommand(sqlForCustomers, conn);
                MySqlDataReader readerForCustomers = cmdForCustomers.ExecuteReader();

                

                while (readerForCustomers.Read())
                {
                    MySqlCommand cmdForContacts = new MySqlCommand("SELECT * FROM customer_contacts WHERE customer_id='" + readerForCustomers[0].ToString() + "'", conn2);
                    MySqlDataReader readerForContacts = cmdForContacts.ExecuteReader();
                    List<Contact> tempContacts = new List<Contact>();
                    while (readerForContacts.Read())
                    {
                        tempContacts.Add(new Contact(readerForContacts[1].ToString(), readerForContacts[2].ToString(), readerForContacts[3].ToString()));
                    }
                    cmdForContacts.Dispose();
                    readerForContacts.Close();
                    comboxCustomer.Items.Add(readerForCustomers[1].ToString());
                    customers.Add(new Customer(readerForCustomers[0].ToString(), readerForCustomers[1].ToString(), tempContacts));
                }


                readerForCustomers.Close();
                cmdForCustomers.Dispose();

                
                conn2.Close();

                //////

                /*
                MySqlCommand cmdForQuoteNumber = new MySqlCommand("SELECT AUTO_INCREMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'quotinglog' AND TABLE_NAME = 'main'", conn);
                MySqlDataReader readerForQuoteNumber = cmdForQuoteNumber.ExecuteReader();
                readerForQuoteNumber.Read();
                txtboxQuoteName.Text = Convert.ToString(readerForQuoteNumber.GetInt32(0));

                readerForQuoteNumber.Close();
                cmdForQuoteNumber.Dispose();
                */
                conn.Close();


            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        void convertTextbox(TextBox txtbox, decimal conversionFactor)
        {
            if (isThisStringANumber(txtbox.Text))
            {
                txtbox.Text = Convert.ToString(Math.Round(Convert.ToDecimal(txtbox.Text) * conversionFactor, 4));
            }
        }

        void convertAllInputs(Boolean imperial)
        {
            decimal conversionFactor;
            decimal conversionFactorInches;
            decimal conversionFactorMass;

            if (imperial)
            {
                conversionFactor = 0.3048M;
                conversionFactorInches = conversionFactor / 12M;
                conversionFactorMass = 1M / poundsPerKilogram;

            } else
            {
                conversionFactor = 3.28084M;
                conversionFactorInches = conversionFactor * 12M;
                conversionFactorMass = poundsPerKilogram;
            }

            convertTextbox(txtboxPitDepth, conversionFactor);
            convertTextbox(txtboxPlatformLength, conversionFactor);
            convertTextbox(txtboxPlatformWidth, conversionFactor);
            convertTextbox(txtboxOverheadCl, conversionFactor);
            convertTextbox(txtboxTravelDis, conversionFactor);
            convertTextbox(txtboxTravelSpeed, conversionFactor);
            convertTextbox(txtboxCapacity, conversionFactorMass);

        }

        void setUnits(string newUnits)
        {
            string newUnitLabelLength;
            string newUnitLabelMass;
            string newUnitLabelPressure;

            if (newUnits == "Metric")
            {
                if (!unitsAreMetric)
                {
                    unitsAreMetric = true;
                    convertAllInputs(unitsAreMetric);
                }
                newUnitLabelLength = "m";
                newUnitLabelMass = "kg";
                newUnitLabelPressure = "Mpa";
            }
            else
            {
                if (unitsAreMetric)
                {
                    unitsAreMetric = false;
                    convertAllInputs(unitsAreMetric);
                }
                newUnitLabelLength = "ft";
                newUnitLabelMass = "lbs";
                newUnitLabelPressure = "psi";
            }

            foreach (Label label in panelConditions.Controls.OfType<Label>().Where(label => label.Name.StartsWith("labelUnit")))
            {
                label.Text = newUnitLabelLength;
                if (label.Name.Contains("Speed"))
                {
                    label.Text += "/s";
                } else if (label.Name.Contains("Mass"))
                {
                    label.Text = newUnitLabelMass;
                }
            }

            foreach (Label label in panelOutput.Controls.OfType<Label>().Where(label => label.Name.StartsWith("labelMass")))
            {
                label.Text = newUnitLabelMass;
            }

            foreach (Label label in panelOutput.Controls.OfType<Label>().Where(label => label.Name.StartsWith("labelPressure")))
            {
                label.Text = newUnitLabelPressure;
            }

            /*
            labelUnit1.Text = newUnitLabelLength;
            labelUnit2.Text = newUnitLabelLength;
            labelUnit3.Text = newUnitLabelLength;
            labelUnit4.Text = newUnitLabelLength;
            labelUnit5.Text = newUnitLabelLength;
            labelSpeedUnit.Text = newUnitLabelLength + "/s";
            */
        }
        

        //Next Buttons



        //Call Calculate
        private void comboxUnits_SelectedIndexChanged(object sender, EventArgs e)
        {
            setUnits(comboxUnits.Items[comboxUnits.SelectedIndex].ToString());
            updateAllCalculations();
        }

        private void buttonCalculate_Click(object sender, EventArgs e)
        {
            updateAllCalculations();
        }


        //calculation
        void updateAllCalculations()
        {
            calculateCapacity();
            calculatePlatformMass();
            calculatePressures();
            //calculatePressures(txtboxFullLoadStatic, txtboxFullLoadDynamic, txtboxCapacity);
            //calculatePressures(txtboxFullLoadStaticA, txtboxFullLoadDynamicA, txtboxCapacityClassA);
            //calculatePressures(txtboxFullLoadStaticB, txtboxFullLoadDynamicB, txtboxCapacityClassB);
            //calculatePressures(txtboxFullLoadStaticC, txtboxFullLoadDynamicC, txtboxCapacityClassC);
        }

        void calculateCapacity()
        {

            if (isThisStringANumber(txtboxPlatformWidth.Text) && isThisStringANumber(txtboxPlatformLength.Text))
            {

                decimal platformWidth = decimal.Parse(txtboxPlatformWidth.Text);
                decimal platformLength = decimal.Parse(txtboxPlatformLength.Text);
                decimal platformArea = platformLength * platformWidth;
                decimal platformClassCapacity = -1;

                switch (comboxLoadType.SelectedIndex)
                {
                    case 0:
                        platformClassCapacity = (unitsAreMetric ? metricCapacityValues["A"] : imperialCapacityValues["A"]) * platformArea;
                        Lift.LoadingClass = "A";
                        break;
                    case 1:
                        platformClassCapacity = (unitsAreMetric ? metricCapacityValues["B"] : imperialCapacityValues["B"]) * platformArea;
                        Lift.LoadingClass = "B";
                        break;
                    case 2:
                        platformClassCapacity = (unitsAreMetric ? metricCapacityValues["C1"] : imperialCapacityValues["C1"]) * platformArea;
                        Lift.LoadingClass = "C1";
                        break;
                    case 3:
                        platformClassCapacity = (unitsAreMetric ? metricCapacityValues["C2"] : imperialCapacityValues["C2"]) * platformArea;
                        Lift.LoadingClass = "C2";
                        break;
                    case 4:
                        platformClassCapacity = (unitsAreMetric ? metricCapacityValues["C3"] : imperialCapacityValues["C3"]) * platformArea;
                        Lift.LoadingClass = "C3";
                        break;
                    default:
                        
                        break;
                }

                Lift.MinCapacity = platformClassCapacity;


                if (isThisStringANumber(txtboxCapacity.Text) && Lift.MinCapacity != -1)
                {
                    if (Convert.ToDecimal(txtboxCapacity.Text) >= Lift.MinCapacity)
                    {
                        Lift.RequiredCapacity = Convert.ToDecimal(txtboxCapacity.Text);
                    }
                    else
                    {
                        Lift.RequiredCapacity = -1;
                    }
                }

            }
            else
            {
                Lift.MinCapacity = -1;
            }
        }
        void calculatePlatformMass()
        {
            if (isThisStringANumber(txtboxPlatformWidth.Text) && isThisStringANumber(txtboxPlatformLength.Text))
            {
                decimal platformWidth = decimal.Parse(txtboxPlatformWidth.Text);
                decimal platformLength = decimal.Parse(txtboxPlatformLength.Text);
                decimal platformArea = platformWidth * platformLength;



                decimal platformMassPerArea;

                if (pitDepthThresholdMet)
                {

                    if (unitsAreMetric)
                    {
                        platformMassPerArea = massPerSqMDeepPit;
                    }
                    else
                    {
                        platformMassPerArea = massPerSqFtDeepPit;
                    }
                } else
                {
                    if (unitsAreMetric)
                    {
                        platformMassPerArea = massPerSqMShallowPit;
                    }
                    else
                    {
                        platformMassPerArea = massPerSqFtShallowPit;
                    }
                }

                decimal platformMass = platformArea * platformMassPerArea;

                Lift.PlatformMass = platformMass;

            }
            else
            {
                Lift.PlatformMass = -1;
            }
        }
        //void calculatePressures(TextBox textBoxToPopulateStatic, TextBox textBoxToPopulateDynamic, TextBox capacityTextBox)
        void calculatePressures()
        {
            if (Lift.RequiredCapacity != -1 && Lift.PlatformMass != -1 && isThisStringANumber(comboxNumberOfCylinders.Text) && comboxCylinders.SelectedIndex != -1)
            {
                decimal platformMass = Lift.PlatformMass;

                decimal totalMass = platformMass + Lift.RequiredCapacity;

                decimal conversionFactor = 1;

                decimal totalArea;

                if (unitsAreMetric)
                {
                    totalArea = decimal.Parse(comboxNumberOfCylinders.Text) * cylinderEffectiveAreasMetric[comboxCylinders.SelectedIndex];
                    conversionFactor = acceleration;
                }
                else
                {
                    totalArea = decimal.Parse(comboxNumberOfCylinders.Text) * cylinderEffectiveAreasImperial[comboxCylinders.SelectedIndex];
                    conversionFactor = 1;
                }

                decimal emptyPlatformPressureStatic = platformMass * conversionFactor / totalArea;

                decimal emptyPlatformPressureDynamic = emptyPlatformPressureStatic * 1.1M;

                decimal maxOperatingPressureStatic = totalMass * conversionFactor / totalArea;

                decimal maxOperatingPressureDynamic = maxOperatingPressureStatic * 1.1M;

                /*
                txtboxFullLoadStatic.BackColor = txtboxMinCapacity.BackColor;
                txtboxFullLoadStatic.ForeColor = Color.Black;
                if (!isPressureOk(maxOperatingPressureStatic))
                {
                    txtboxFullLoadStatic.ForeColor = Color.Red;
                }
                */
                Lift.FullStaticPressure = maxOperatingPressureStatic;

                /*
                txtboxFullLoadDynamic.BackColor = txtboxMinCapacity.BackColor;
                txtboxFullLoadDynamic.ForeColor = Color.Black;
                if (!isPressureOk(maxOperatingPressureDynamic))
                {
                    txtboxFullLoadDynamic.ForeColor = Color.Red;
                }
                */
                Lift.FullDynamicPressure = maxOperatingPressureDynamic;

                /*
                txtboxEmptyPlatformStatic.BackColor = txtboxMinCapacity.BackColor;
                txtboxEmptyPlatformStatic.ForeColor = Color.Black;
                if (!isPressureOk(emptyPlatformPressureStatic))
                {
                    txtboxEmptyPlatformStatic.ForeColor = Color.Red;
                }
                */
                Lift.EmptyStaticPressure = emptyPlatformPressureStatic;

                /*
                txtboxEmptyPlatformDynamic.BackColor = txtboxMinCapacity.BackColor;
                txtboxEmptyPlatformDynamic.ForeColor = Color.Black;
                if (!isPressureOk(emptyPlatformPressureDynamic))
                {
                    txtboxEmptyPlatformDynamic.ForeColor = Color.Red;
                }
                */
                Lift.EmptyDynamicPressure = emptyPlatformPressureDynamic;
            }
            else
            {
                Lift.FullStaticPressure = -1;
                Lift.FullDynamicPressure = -1;
                Lift.EmptyStaticPressure = -1;
                Lift.EmptyDynamicPressure = -1;
            }
        }


        //OK Button
        private void buttonOK_Click(object sender, EventArgs e)
        {
            MessageBox.Show(dtpDate.Value.ToShortDateString());

        }


        //Saving
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasswordConnectionInfo connectionInfo = new PasswordConnectionInfo("192.168.2.52", "gregyoung", "stellaris"); //replace "192.168.2.52" with "stellarismysql.ddns.net", 7846 for connections from offsite
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

                    if (!isThisStringANumber(txtboxQuoteName.Text))
                    {
                        CreateQuoteLocal();
                    }
                    SaveQuoteLocal();

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
        void CreateQuoteLocal()
        {

            MySqlConnection conn;
            string myConnectionString;

            myConnectionString = "server=127.0.0.1;port=1000;uid=gregyoung;pwd=[Stellaris03];database=quotinglog;";

            conn = new MySql.Data.MySqlClient.MySqlConnection();
            conn.ConnectionString = myConnectionString;
            string sql = "INSERT INTO main (ProjectDescription) VALUES(@ProjectDescription)";
            
            conn.Open();

            MySqlCommand cmd = new MySqlCommand(sql, conn);


            cmd.Parameters.Add("@ProjectDescription", MySqlDbType.VarChar).Value = txtboxProjectDescription.Text;
            /*
            cmd.Parameters.Add("@Customer", MySqlDbType.VarChar).Value = comboxCustomer.Text;
            cmd.Parameters.Add("@Contact", MySqlDbType.VarChar).Value = comboxContactName.Text;
            cmd.Parameters.Add("@LoadType", MySqlDbType.VarChar).Value = comboxLoadType.Text;
            cmd.Parameters.Add("@PitDepth", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxPitDepth.Text) ? Convert.ToDecimal(txtboxPitDepth.Text) : 0;
            cmd.Parameters.Add("@TravelDistance", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxTravelDis.Text) ? Convert.ToDecimal(txtboxTravelDis.Text) : 0;
            cmd.Parameters.Add("@OverheadClearance", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxOverheadCl.Text) ? Convert.ToDecimal(txtboxOverheadCl.Text) : 0;
            cmd.Parameters.Add("@Floors", MySqlDbType.Int16).Value = isThisStringANumber(comboxFloors.Text) ? Convert.ToInt16(comboxFloors.Text) : 0;
            cmd.Parameters.Add("@TravelSpeed", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxTravelSpeed.Text) ? Convert.ToDecimal(txtboxTravelSpeed.Text) : 0;
            cmd.Parameters.Add("@PlatformWidth", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxPlatformWidth.Text) ? Convert.ToDecimal(txtboxPlatformWidth.Text) : 0;
            cmd.Parameters.Add("@PlatformLength", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxPlatformLength.Text) ? Convert.ToDecimal(txtboxPlatformLength.Text) : 0;
            cmd.Parameters.Add("@InlineThrough", MySqlDbType.VarChar).Value = comboxInlineThrough.Text;
            cmd.Parameters.Add("@Capacity", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxCapacity.Text) ? Convert.ToDecimal(txtboxCapacity.Text) : 0;
            cmd.Parameters.Add("@Date", MySqlDbType.Date).Value = Convert.ToDateTime(dtpDate.Value.ToShortDateString());
            */
            cmd.ExecuteNonQuery();

            cmd.Dispose();
            conn.Close();

        }
        void SaveQuoteLocal()
        {

            MySqlConnection conn;
            string myConnectionString;

            myConnectionString = "server=127.0.0.1;port=1000;uid=gregyoung;pwd=[Stellaris03];database=quotinglog;";

            conn = new MySql.Data.MySqlClient.MySqlConnection();
            conn.ConnectionString = myConnectionString;
            conn.Open();

            if (!isThisStringANumber(txtboxQuoteName.Text))
            {
                MySqlCommand cmdForQuoteNumber = new MySqlCommand("SELECT QuoteName FROM main ORDER BY QuoteName DESC LIMIT 1", conn);
                MySqlDataReader readerForQuoteNumber = cmdForQuoteNumber.ExecuteReader();
                readerForQuoteNumber.Read();
                txtboxQuoteName.Text = Convert.ToString(readerForQuoteNumber.GetInt16(0));

                readerForQuoteNumber.Close();
                cmdForQuoteNumber.Dispose();
            }

            //string sql = "UPDATE main SET LoadType = @LoadType, QuoteDate = @QuoteDate Where QuoteName = @QuoteName";
            string sql = "UPDATE main SET ProjectDescription = @ProjectDescription, Date = @Date, Customer = @Customer, Contact = @Contact, LoadType = @LoadType, PitDepth = @PitDepth, TravelDistance = @TravelDistance, OverheadClearance = @OverheadClearance, Floors = @Floors, TravelSpeed = @TravelSpeed, PlatformWidth = @PlatformWidth, PlatformLength = @PlatformLength, InlineThrough = @InlineThrough, Capacity = @Capacity WHERE QuoteName = @QuoteName";
            

            MySqlCommand cmd = new MySqlCommand(sql, conn);

            //update
            cmd.Parameters.Add("@ProjectDescription", MySqlDbType.VarChar).Value = txtboxProjectDescription.Text;
            cmd.Parameters.Add("@Customer", MySqlDbType.VarChar).Value = comboxCustomer.Text;
            cmd.Parameters.Add("@Contact", MySqlDbType.VarChar).Value = comboxContactName.Text;
            cmd.Parameters.Add("@LoadType", MySqlDbType.VarChar).Value = comboxLoadType.Text;
            cmd.Parameters.Add("@PitDepth", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxPitDepth.Text) ? Convert.ToDecimal(txtboxPitDepth.Text) : 0;
            cmd.Parameters.Add("@TravelDistance", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxTravelDis.Text) ? Convert.ToDecimal(txtboxTravelDis.Text) : 0;
            cmd.Parameters.Add("@OverheadClearance", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxOverheadCl.Text) ? Convert.ToDecimal(txtboxOverheadCl.Text) : 0;
            cmd.Parameters.Add("@Floors", MySqlDbType.Int16).Value = isThisStringANumber(comboxFloors.Text) ? Convert.ToInt16(comboxFloors.Text) : 0;
            cmd.Parameters.Add("@TravelSpeed", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxTravelSpeed.Text) ? Convert.ToDecimal(txtboxTravelSpeed.Text) : 0;
            cmd.Parameters.Add("@PlatformWidth", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxPlatformWidth.Text) ? Convert.ToDecimal(txtboxPlatformWidth.Text) : 0;
            cmd.Parameters.Add("@PlatformLength", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxPlatformLength.Text) ? Convert.ToDecimal(txtboxPlatformLength.Text) : 0;
            cmd.Parameters.Add("@InlineThrough", MySqlDbType.VarChar).Value = comboxInlineThrough.Text;
            cmd.Parameters.Add("@Capacity", MySqlDbType.Decimal).Value = isThisStringANumber(txtboxCapacity.Text) ? Convert.ToDecimal(txtboxCapacity.Text) : 0;
            cmd.Parameters.Add("@Date", MySqlDbType.Date).Value = Convert.ToDateTime(dtpDate.Value.ToShortDateString());

            //where
            cmd.Parameters.Add("@QuoteName", MySqlDbType.Int16).Value = Convert.ToInt16(txtboxQuoteName.Text);

            cmd.ExecuteNonQuery();

            cmd.Dispose();
            conn.Close();


            MessageBox.Show("Quote Saved");
        }


        //Boolean Checks

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
        Boolean isPressureOk(decimal pressureToCheck)
        {
            decimal maximumPressure = unitsAreMetric ? maxOperatingPressure * 6.895M : maxOperatingPressure;

            if (pressureToCheck <= maximumPressure)
            {
                return true;
            }
            else
            {
                return false;
            }
        }



        private void drawObject(List<Line> objectList, DxfDocument doc)
        {
            for (int i = 0; i < objectList.Count; i++)
            {
                doc.AddEntity(objectList[i]);
            }
        }

        private void drawDimension(List<LinearDimension> objectList, DxfDocument doc)
        {
            for (int i = 0; i < objectList.Count; i++)
            {
                doc.AddEntity(objectList[i]);
            }
        }

       private void createDXF()
        {
            double conversionFactor = 1;
            double PlatformThickness = .5;

            if (!unitsAreMetric)
            {
                conversionFactor = 12;
                PlatformThickness = 12;
            }

            double PlatformLength = (Convert.ToDouble(txtboxPlatformLength.Text) * conversionFactor);
            double PlatformWidth = (Convert.ToDouble(txtboxPlatformWidth.Text) * conversionFactor);


            double PitDepth = (Convert.ToDouble(txtboxPitDepth.Text) * conversionFactor);

            double TravelDistance = (Convert.ToDouble(txtboxTravelDis.Text) * conversionFactor);
            double OverheadCl = (Convert.ToDouble(txtboxOverheadCl.Text) * conversionFactor);

            double TopCl = 24;

            double DimensionX = dxfStartX / 2;
            double dimPad = 5;

            double hatchThickness = 20;

            double dxfPlanStartX = dxfStartX + PlatformLength * 2;


            int numOfFloors;

            // your DXF file name
            string file = "_sample.dxf";

            // create a new document, by default it will create an AutoCad2000 DXF version
            DxfDocument doc = new DxfDocument();
            // an entity

            //Platform
            List<Line> platform = new List<Line>();

            platform.Add(new Line(new Vector2(dxfStartX, dxfStartY), new Vector2(dxfStartX, dxfStartY + PitDepth)));
            platform.Add(new Line(new Vector2(dxfStartX, dxfStartY + PitDepth), new Vector2(dxfStartX - PlatformThickness * 2, dxfStartY + PitDepth)));
            platform.Add(new Line(new Vector2(dxfStartX - PlatformThickness * 2, dxfStartY + PitDepth + PlatformThickness), new Vector2(dxfStartX + PlatformThickness, dxfStartY + PitDepth + PlatformThickness)));
            platform.Add(new Line(new Vector2(dxfStartX, dxfStartY), new Vector2(dxfStartX + PlatformLength + PlatformThickness * 2, dxfStartY)));
            platform.Add(new Line(new Vector2(dxfStartX + PlatformLength + PlatformThickness * 2, dxfStartY), new Vector2(dxfStartX + PlatformLength + PlatformThickness * 2, dxfStartY + PitDepth)));
            platform.Add(new Line(new Vector2(dxfStartX + PlatformLength + PlatformThickness * 2, dxfStartY + PitDepth), new Vector2(dxfStartX + PlatformLength + PlatformThickness * 4, dxfStartY + PitDepth)));
            platform.Add(new Line(new Vector2(dxfStartX + PlatformLength + PlatformThickness, dxfStartY + PitDepth + PlatformThickness), new Vector2(dxfStartX + PlatformLength + PlatformThickness * 4, dxfStartY + PitDepth + PlatformThickness)));
            platform.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY + PlatformThickness), new Vector2(dxfStartX + PlatformLength + PlatformThickness, dxfStartY + PlatformThickness)));


            //top of lift
            List<Line> topOfLift = new List<Line>();

            topOfLift.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY + PlatformThickness), new Vector2(dxfStartX + PlatformThickness, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl)));
            topOfLift.Add(new Line(new Vector2(dxfStartX + PlatformThickness + PlatformLength, dxfStartY + PlatformThickness), new Vector2(dxfStartX + PlatformThickness + PlatformLength, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl)));
            topOfLift.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl), new Vector2(dxfStartX + PlatformThickness + PlatformLength, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl)));

            //top area
            List<Line> topArea = new List<Line>();

            topArea.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl - TopCl), new Vector2(dxfStartX, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl - TopCl)));
            topArea.Add(new Line(new Vector2(dxfStartX, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl - TopCl), new Vector2(dxfStartX, dxfStartY + (PlatformThickness * 2) + PitDepth + TravelDistance + OverheadCl)));
            topArea.Add(new Line(new Vector2(dxfStartX, dxfStartY + (PlatformThickness * 2) + PitDepth + TravelDistance + OverheadCl), new Vector2(dxfStartX + PlatformLength + (PlatformThickness * 2), dxfStartY + (PlatformThickness * 2) + PitDepth + TravelDistance + OverheadCl)));
            topArea.Add(new Line(new Vector2(dxfStartX + PlatformLength + (PlatformThickness * 2), dxfStartY + (PlatformThickness * 2) + PitDepth + TravelDistance + OverheadCl), new Vector2(dxfStartX + PlatformLength + (PlatformThickness * 2), dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl - TopCl)));
            topArea.Add(new Line(new Vector2(dxfStartX + PlatformLength + (PlatformThickness * 2), dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl - TopCl), new Vector2(dxfStartX + PlatformLength + PlatformThickness, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl - TopCl)));


            //Floors
            List<Line> floors = new List<Line>();

            List<LinearDimension> intTravel = new List<LinearDimension>();

            //Subsequent floors

            numOfFloors = Convert.ToInt32(comboxFloors.Text);

            for (int i = 1; i < numOfFloors; i++)
            {

                floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY + PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX - (PlatformThickness * 2), dxfStartY + PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors))));
                floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY - PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX, dxfStartY - PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors))));
                floors.Add(new Line(new Vector2(dxfStartX, dxfStartY - PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX, dxfStartY + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors))));
                floors.Add(new Line(new Vector2(dxfStartX, dxfStartY + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX - (PlatformThickness * 2), dxfStartY + TravelDistance - (TravelDistance * i / numOfFloors) + PitDepth)));
                floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness + PlatformLength, dxfStartY + PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX + (PlatformThickness * 4) + PlatformLength, dxfStartY + PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors))));
                floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness + PlatformLength, dxfStartY - PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY - PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors))));
                floors.Add(new Line(new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY - PlatformThickness + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY + PitDepth + TravelDistance - (TravelDistance * i / numOfFloors))));
                floors.Add(new Line(new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY + TravelDistance + PitDepth - (TravelDistance * i / numOfFloors)), new Vector2(dxfStartX + (PlatformThickness * 4) + PlatformLength, dxfStartY + TravelDistance + PitDepth - (TravelDistance * i / numOfFloors))));

                LinearDimension floordim = new LinearDimension(new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PitDepth + PlatformThickness + TravelDistance - (TravelDistance * (i + 1) / numOfFloors)), new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PitDepth + PlatformThickness + TravelDistance - (TravelDistance * i / numOfFloors)), DimensionX - (PlatformThickness * 5.5), 90, netDxf.Tables.DimensionStyle.Iso25);
                floordim.UserText = (TravelDistance / numOfFloors).ToString() + " INT TRAVEL";

                intTravel.Add(floordim);

            }

            if (numOfFloors > 1)
            {

                LinearDimension floordim = new LinearDimension(new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PitDepth + PlatformThickness + TravelDistance), new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PitDepth + PlatformThickness + TravelDistance - (TravelDistance * 1 / numOfFloors)), DimensionX - (PlatformThickness * 5.5), 90, netDxf.Tables.DimensionStyle.Iso25);
                floordim.UserText = (TravelDistance / numOfFloors).ToString() + " INT TRAVEL";

                intTravel.Add(floordim);

            }


            //Top Floor
            floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY + PlatformThickness + PitDepth + TravelDistance), new Vector2(dxfStartX - (PlatformThickness * 2), dxfStartY + PlatformThickness + PitDepth + TravelDistance)));
            floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness, dxfStartY - PlatformThickness + PitDepth + TravelDistance), new Vector2(dxfStartX, dxfStartY - PlatformThickness + PitDepth + TravelDistance)));
            floors.Add(new Line(new Vector2(dxfStartX, dxfStartY - PlatformThickness + PitDepth + TravelDistance), new Vector2(dxfStartX, dxfStartY + PitDepth + TravelDistance)));
            floors.Add(new Line(new Vector2(dxfStartX, dxfStartY + PitDepth + TravelDistance), new Vector2(dxfStartX - (PlatformThickness * 2), dxfStartY + TravelDistance + PitDepth)));
            floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness + PlatformLength, dxfStartY + PlatformThickness + PitDepth + TravelDistance), new Vector2(dxfStartX + (PlatformThickness * 4) + PlatformLength, dxfStartY + PlatformThickness + PitDepth + TravelDistance)));
            floors.Add(new Line(new Vector2(dxfStartX + PlatformThickness + PlatformLength, dxfStartY - PlatformThickness + PitDepth + TravelDistance), new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY - PlatformThickness + PitDepth + TravelDistance)));
            floors.Add(new Line(new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY - PlatformThickness + PitDepth + TravelDistance), new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY + PitDepth + TravelDistance)));
            floors.Add(new Line(new Vector2(dxfStartX + (PlatformThickness * 2) + PlatformLength, dxfStartY + TravelDistance + PitDepth), new Vector2(dxfStartX + (PlatformThickness * 4) + PlatformLength, dxfStartY + TravelDistance + PitDepth)));

            //Dimensions

            //PitDepth

            LinearDimension dim1 = new LinearDimension(new Vector2(dxfStartX + PlatformThickness - dimPad, dxfStartY + PlatformThickness), new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PitDepth + PlatformThickness), DimensionX, 90, netDxf.Tables.DimensionStyle.Iso25);
            dim1.UserText = PitDepth.ToString() + " PIT DEPTH";


            //Travel

            LinearDimension dim2 = new LinearDimension(new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PitDepth + PlatformThickness), new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PitDepth + PlatformThickness + TravelDistance), DimensionX - (PlatformThickness * 1.5), 90, netDxf.Tables.DimensionStyle.Iso25);
            dim2.UserText = TravelDistance.ToString() + " TRAVEL";


            //Overhead Cl
            LinearDimension dim3 = new LinearDimension(new Vector2(dxfStartX - (PlatformThickness * 2) - dimPad, dxfStartY + PlatformThickness + PitDepth + TravelDistance), new Vector2(dxfStartX + PlatformThickness - dimPad, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl), DimensionX, 90, netDxf.Tables.DimensionStyle.Iso25);
            dim3.UserText = OverheadCl.ToString() + " OVERHEAD CLEARANCE";

            //Top Cl
            LinearDimension dim4 = new LinearDimension(new Vector2(dxfStartX - dimPad, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl - TopCl), new Vector2(dxfStartX + PlatformThickness - dimPad, dxfStartY + PlatformThickness + PitDepth + TravelDistance + OverheadCl), DimensionX - PlatformThickness * 3, 90, netDxf.Tables.DimensionStyle.Iso25);



            //Plan view
            List<Line> planView = new List<Line>();

            //outside
            planView.Add(new Line(new Vector2(dxfPlanStartX, dxfStartY), new Vector2(dxfPlanStartX + PlatformLength + hatchThickness * 2, dxfStartY)));
            planView.Add(new Line(new Vector2(dxfPlanStartX, dxfStartY), new Vector2(dxfPlanStartX, dxfStartY + PlatformWidth + hatchThickness * 2)));
            planView.Add(new Line(new Vector2(dxfPlanStartX, dxfStartY + PlatformWidth + hatchThickness * 2), new Vector2(dxfPlanStartX + PlatformLength + hatchThickness * 2, dxfStartY + PlatformWidth + hatchThickness * 2)));
            planView.Add(new Line(new Vector2(dxfPlanStartX + PlatformLength + hatchThickness * 2, dxfStartY), new Vector2(dxfPlanStartX + PlatformLength + hatchThickness * 2, dxfStartY + PlatformWidth + hatchThickness * 2)));

            //inside
            planView.Add(new Line(new Vector2(dxfPlanStartX + hatchThickness, dxfStartY + hatchThickness), new Vector2(dxfPlanStartX + PlatformLength + hatchThickness, dxfStartY + hatchThickness)));
            planView.Add(new Line(new Vector2(dxfPlanStartX + hatchThickness, dxfStartY + hatchThickness), new Vector2(dxfPlanStartX + hatchThickness, dxfStartY + PlatformWidth + hatchThickness)));
            planView.Add(new Line(new Vector2(dxfPlanStartX + hatchThickness, dxfStartY + PlatformWidth + hatchThickness), new Vector2(dxfPlanStartX + PlatformLength + hatchThickness, dxfStartY + PlatformWidth + hatchThickness)));
            planView.Add(new Line(new Vector2(dxfPlanStartX + PlatformLength + hatchThickness, dxfStartY + hatchThickness), new Vector2(dxfPlanStartX + PlatformLength + hatchThickness, dxfStartY + PlatformWidth + hatchThickness)));



            drawObject(platform, doc);
            drawObject(topOfLift, doc);
            drawObject(topArea, doc);
            drawObject(floors, doc);
            drawObject(planView, doc);
            drawDimension(intTravel, doc);

            // add your entities here

            doc.AddEntity(dim1);
            doc.AddEntity(dim2);
            doc.AddEntity(dim3);
            doc.AddEntity(dim4);


            // save to file
            doc.Save(file);

            // this check is optional but recommended before loading a DXF file
            //DxfVersion dxfVersion = DxfDocument.CheckDxfFileVersion(file);
            // netDxf is only compatible with AutoCad2000 and higher DXF versions
            //if (dxfVersion < DxfVersion.AutoCad2000) return;
            // load file
            DxfDocument loaded = DxfDocument.Load(file);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Application.Restart();
            Lift.Reset();
            comboxContactName.Items.Clear();
            txtboxQuoteName.Text = "";
            dtpDate.Value = DateTime.Today;

            foreach (Control control in panelDetails.Controls)
            {
                if (control.Name.StartsWith("txtbox") || control.Name.StartsWith("combox"))
                {
                    control.Text = "";
                }
                
            }
            foreach (Control control in panelLoading.Controls)
            {
                if (control.Name.StartsWith("txtbox") || control.Name.StartsWith("combox"))
                {
                    control.Text = "";
                } 

            }
            foreach (Control control in panelConditions.Controls)
            {
                if (control.Name.StartsWith("txtbox") || control.Name.StartsWith("combox"))
                {
                    control.Text = "";
                }

            }
            foreach (Control control in panelCylinders.Controls)
            {
                if (control.Name.StartsWith("txtbox") || control.Name.StartsWith("combox"))
                {
                    control.Text = "";
                }

            }
        }

        private void comboxCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            int customerIndex = comboxCustomer.SelectedIndex;

            comboxContactName.Items.Clear();

            comboxContactName.Text = "";
            txtboxContactEmail.Text = "";
            txtboxContactPhone.Text = "";

            foreach (Contact contact in customers[customerIndex].Contacts)
            {
                comboxContactName.Items.Add(contact.Name);
            }
        }

        private void comboxContactName_SelectedIndexChanged(object sender, EventArgs e)
        {
            int customerIndex = comboxCustomer.SelectedIndex;
            int contactIndex = comboxContactName.SelectedIndex;

            txtboxContactEmail.Text = customers[customerIndex].Contacts[contactIndex].Email;
            txtboxContactPhone.Text = customers[customerIndex].Contacts[contactIndex].Phone;
        }

        private void buttonPDNext_Click(object sender, EventArgs e)
        {
            /*
            if (comboxCustomer.SelectedIndex == -1)
            {
                MessageBox.Show(this, "No Customer Selected", "Complete Form", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (comboxProvince.SelectedIndex == -1)
            {
                MessageBox.Show(this, "No Province Selected", "Complete Form", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (comboxContactName.SelectedIndex == -1)
            {
                MessageBox.Show(this, "No Contact Selected", "Complete Form", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (txtboxProjectDescription.Text == "")
            {
                MessageBox.Show(this, "Project Description Missing", "Complete Form", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                //txtboxQuoteName.Text = customers[comboxCustomer.SelectedIndex].ID;
                tabControl.SelectedIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
            }
            */
            timerNext.Start();
        }

        private void timerNext_Tick(object sender, EventArgs e)
        {

            panelDetails.Left -= 10;
            panelLoading.Left -= 10;
            panelConditions.Left -= 10;
            panelCylinders.Left -= 10;
            shifted += 10;

            if (shifted >= 370)
            {
                timerNext.Stop();
                shifted = 0;
            }
        }

        private void buttonLoadBack_Click(object sender, EventArgs e)
        {
            //tabControl.SelectedIndex = (tabControl.SelectedIndex - 1) % tabControl.TabCount;
            timerBack.Start();
        }

        private void timerBack_Tick(object sender, EventArgs e)
        {
            panelDetails.Left += 10;
            panelLoading.Left += 10;
            panelConditions.Left += 10;
            panelCylinders.Left += 10;
            shifted += 10;

            if (shifted >= 370)
            {
                timerBack.Stop();
                shifted = 0;
            }
        }

        private void buttonLoadNext_Click(object sender, EventArgs e)
        {
            /*
            if (comboxLoadType.SelectedIndex == -1)
            {
                MessageBox.Show(this, "No Load Type Selected", "Complete Form", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            } else
            {
                tabControl.SelectedIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
            }
            */
            timerNext.Start();
        }

        private void buttonSCBack_Click(object sender, EventArgs e)
        {
            //tabControl.SelectedIndex = (tabControl.SelectedIndex - 1) % tabControl.TabCount;
            timerBack.Start();
        }

        private void buttonSCNext_Click(object sender, EventArgs e)
        {
            updateAllCalculations();
            //tabControl.SelectedIndex = (tabControl.SelectedIndex + 1) % tabControl.TabCount;
            timerNext.Start();
        }

        private void comboxLoadType_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboxLoadType.SelectedIndex)
            {
                case 0:

                    pictureBoxClass.Image = Properties.Resources.ClassA;

                    break;

                case 1:

                    pictureBoxClass.Image = Properties.Resources.ClassB;

                    break;

                case 2:

                    pictureBoxClass.Image = Properties.Resources.ClassC1;

                    break;

                case 3:

                    pictureBoxClass.Image = Properties.Resources.ClassC2;

                    break;

                case 4:

                    pictureBoxClass.Image = Properties.Resources.ClassC3;

                    break;
            }
            calculateCapacity();
        }

        private void comboxCylinders_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateAllCalculations();
        }

        private void comboxNumberOfCylinders_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateAllCalculations();
        }

        private void buttonCylBack_Click(object sender, EventArgs e)
        {
            timerBack.Start();
        }

        private void liftSpecificationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SpecificationsForm frm = new SpecificationsForm();
            frm.Show();
            //MessageBox.Show(newLift.ToString());
        }

        private void comboxProvince_SelectedIndexChanged(object sender, EventArgs e)
        {
            Lift.ClassYear = ProvinceCode[comboxProvince.SelectedIndex];
        }

        private void txtboxPitDepth_TextChanged(object sender, EventArgs e)
        {
            decimal pitDepth = 0;

            if (isThisStringANumber(txtboxPitDepth.Text))
            {
                if (unitsAreMetric)
                {
                    pitDepth = Convert.ToDecimal(txtboxPitDepth.Text) * 3.28084M;
                }
                else
                {
                    pitDepth = Convert.ToDecimal(txtboxPitDepth.Text);
                }
            }

            if (pitDepth >= pitDepthThreshold)
            {
                pitDepthThresholdMet = true;
            }
            else
            {
                pitDepthThresholdMet = false;
            }

            updateAllCalculations();
        }

        private void txtboxPlatformWidth_TextChanged(object sender, EventArgs e)
        {
            updateAllCalculations();
        }

        private void txtboxPlatformLength_TextChanged(object sender, EventArgs e)
        {
            updateAllCalculations();
        }

        private void txtboxCapacity_TextChanged(object sender, EventArgs e)
        {
            updateAllCalculations();
        }

        private void buttonDXF_Click(object sender, EventArgs e)
        {
            createDXF();
        }


        /*
        void updateLabels(string labelId, string labelText)
        {
            Controls.Find("labelUnit", true).FirstOrDefault();
        }
        */
    }

    public class Customer
    {

        // Auto-implemented readonly property:
        public string ID { get; }
        public string Name { get; }
        public List<Contact> Contacts { get; }

        // Constructor that takes no arguments:
        /*public Customer()
        {
            Name = "unknown";

        }
        */

        // Constructor that takes arguments:
        public Customer(string id, string name, List<Contact> contacts)
        {
            ID = id;
            Name = name;
            Contacts = contacts;
        }

        // Method that overrides the base class (System.Object) implementation.
        public override string ToString()
        {
            return Name;
        }
    }

    public class Contact
    {

        // Auto-implemented readonly property:

        public string Name { get; }
        public string Email { get; }
        public string Phone { get; }


        // Constructor that takes no arguments:
        /*
        public Contact()
        {
            Name = "unknown";
        }
        */

        // Constructor that takes arguments:
        public Contact(string name, string email, string phone)
        {
            Name = name;
            Email = email;
            Phone = phone;
        }

        // Method that overrides the base class (System.Object) implementation.
        public override string ToString()
        {
            return Name;
        }
    }
    public static class Lift
    {

        // Auto-implemented readonly property:
        public static string ClassYear { get; set; }
        public static string LoadingClass { get; set; }
        public static decimal PlatformMass { get; set; }
        public static decimal MinCapacity { get; set; }
        public static decimal RequiredCapacity { get; set; }
        public static decimal EmptyStaticPressure { get; set; }
        public static decimal EmptyDynamicPressure { get; set; }
        public static decimal FullStaticPressure { get; set; }
        public static decimal FullDynamicPressure { get; set; }

        // Constructor that takes no arguments:
        static Lift()
        {
            ClassYear = "";
            LoadingClass = "";
            PlatformMass = -1;
            MinCapacity = -1;
            RequiredCapacity = -1;
            EmptyStaticPressure = -1;
            EmptyDynamicPressure = -1;
            FullStaticPressure = -1;
            FullDynamicPressure = -1;
        }


        // Constructor that takes arguments:
        /*
        public Lift(string id, string name, List<Contact> contacts)
        {
            ID = id;
            Name = name;
            Contacts = contacts;
        }
        */
        // Method that overrides the base class (System.Object) implementation.

        public static void Reset()
        {
            ClassYear = "";
            LoadingClass = "";
            PlatformMass = -1;
            MinCapacity = -1;
            RequiredCapacity = -1;
            EmptyStaticPressure = -1;
            EmptyDynamicPressure = -1;
            FullStaticPressure = -1;
            FullDynamicPressure = -1;
        }
        public static string OutputString()
        {
            return string.Format("Here is some info about your lift:\nClass Year: {0}\nClass: {1}\nPlatform Mass: {2,4:.00}\nMinimum Capacity: {3,4:.00}\nRequired Capacity: {4,4:.00}\nEmpty Platform Static Pressure: {5,4:.00}\nEmpty Platform Dynamic Pressure: {6,4:.00}\nFull Load Static Pressure: {7,4:.00}\nFull Load Dynamic Pressure: {8,4:.00}", ClassYear, LoadingClass, PlatformMass, MinCapacity, RequiredCapacity, EmptyStaticPressure, EmptyDynamicPressure, FullStaticPressure, FullDynamicPressure);
        }
    }
    /*
    public class Lift
    {

        // Auto-implemented readonly property:
        public string ClassYear { get; set; }
        public string LoadingClass { get; set; }
        public decimal PlatformMass { get; set; }
        public decimal MinCapacity { get; set; }
        public decimal RequiredCapacity { get; set; }
        public decimal EmptyStaticPressure { get; set; }
        public decimal EmptyDynamicPressure { get; set; }
        public decimal FullStaticPressure { get; set; }
        public decimal FullDynamicPressure { get; set; }

        // Constructor that takes no arguments:
        public Lift()
        {
            ClassYear = "";
            LoadingClass = "";
            PlatformMass = -1;
            MinCapacity = -1;
            RequiredCapacity = -1;
            EmptyStaticPressure = -1;
            EmptyDynamicPressure = -1;
            FullStaticPressure = -1;
            FullDynamicPressure = -1;
        }
        

        // Constructor that takes arguments:
        
        public Lift(string id, string name, List<Contact> contacts)
        {
            ID = id;
            Name = name;
            Contacts = contacts;
        }
        
    // Method that overrides the base class (System.Object) implementation.
    public override string ToString()
        {
            return string.Format("Here is some info about your lift:\nClass Year: {0}\nClass: {1}\nPlatform Mass: {2,4:.00}\nMinimum Capacity: {3,4:.00}\nRequired Capacity: {4,4:.00}\nEmpty Platform Static Pressure: {5,4:.00}\nEmpty Platform Dynamic Pressure: {6,4:.00}\nFull Load Static Pressure: {7,4:.00}\nFull Load Dynamic Pressure: {8,4:.00}", ClassYear, LoadingClass, PlatformMass, MinCapacity, RequiredCapacity, EmptyStaticPressure, EmptyDynamicPressure, FullStaticPressure, FullDynamicPressure);
        }
    }
    */
}
