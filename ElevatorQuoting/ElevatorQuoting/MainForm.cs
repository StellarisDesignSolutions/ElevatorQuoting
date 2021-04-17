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

        public static ConversionFactor KilogramsToPounds = new ConversionFactor(2.20462M, "pounds", "lbs", false);
        public static ConversionFactor PoundsToKilograms = new ConversionFactor(0.453593M, "kilograms", "kg", true);

        public static ConversionFactor MetresToFeet = new ConversionFactor(3.28084M, "feet","ft", false);
        public static ConversionFactor FeetToMetres = new ConversionFactor(0.304799M, "metres", "m", true);

        public static ConversionFactor MpaToPsi = new ConversionFactor(145.03768M, "psi", "PSI", false);
        public static ConversionFactor PsiToMpa = new ConversionFactor(0.00689476M, "mpa", "MPa", true);

        public static ConversionFactor FeetToInches = new ConversionFactor(12M, "inches", "in", false);
        public static ConversionFactor InchesToFeet = new ConversionFactor(0.83333333M, "feet", "ft", false);


        const decimal AccelerationDueToGravity = 9.80665M;

        //Variables
        List<string> ProvinceCode = new List<string>();
        List<decimal> cylinderEffectiveAreasMetric = new List<decimal>();
        List<decimal> cylinderEffectiveAreasImperial = new List<decimal>();
        Dictionary<string, int> metricCapacityValues = new Dictionary<string, int>();
        Dictionary<string, int> imperialCapacityValues = new Dictionary<string, int>();

        List<Customer> customers = new List<Customer>();

        int shifted = 0;

        public static int dxfStartX = 200; //inch
        public static int dxfStartY = 200; //inch

        const decimal poundsPerKilogram = 2.20462M;
        

        const decimal maxOperatingPressure = 1200; //This value will be moved to a standards database//
        const decimal pitDepthThreshold = 0.666M;
        const decimal pitDepthThresholdMetric = pitDepthThreshold / 3.28084M;
        const decimal massPerSqMDeepPit = 317.5378M;
        const decimal massPerSqFtDeepPit = 65;
        const decimal massPerSqMShallowPit = 1;
        const decimal massPerSqFtShallowPit = 1;

        Boolean pitDepthThresholdMet;
        public static Boolean unitsAreMetric = false;

        public MainForm()
        {
            InitializeComponent();

            //Events//
            LoadQuote.OnLoadingQuote += LoadQuote_OnLoadingQuote;
            //////////
            
            comboxUnits.SelectedIndex = 0;
        }

        void LoadQuote_OnLoadingQuote(object sender, EventArgs e)
        {
            NewQuote(false);
            //this.txtboxProjectDescription.Text = "QUOTE LOADED";
            txtboxQuoteName.Text = Quote.QuoteNumber.ToString();
            txtboxProjectDescription.Text = Quote.ProjectDescription;
            comboxCustomer.Text = Quote.ProjectCustomer;
            comboxContactName.Text = Quote.ProjectContact;

            comboxLoadType.Text = UserInputs.LoadType;
            txtboxPitDepth.Text = UserInputs.PitDepth.ToString();
            txtboxTravelDis.Text = UserInputs.TravelDistance.ToString();
            comboxFloors.Text = UserInputs.Floors.ToString();
            txtboxOverheadCl.Text = UserInputs.OverheadClearance.ToString();
            txtboxPlatformWidth.Text = UserInputs.PlatformWidth.ToString();
            txtboxPlatformLength.Text = UserInputs.PlatformLength.ToString();
            txtboxTravelSpeed.Text = UserInputs.TravelSpeed.ToString();
            txtboxCapacity.Text = UserInputs.Capacity.ToString();
            comboxInlineThrough.Text = UserInputs.InlineThrough;

        }

        // loading subs

        private void MainForm_Load(object sender, EventArgs e)
        {
            sshConnection(LogicLoad);
            //LogicLoad();  //migrated to sshConnection function
            //comboxUnits.SelectedIndex = 0;
            //comboxCylinders.SelectedIndex = 0;
            //comboxNumberOfCylinders.SelectedIndex = 0;
            //comboxProvince.SelectedIndex = 8;

        }
        void sshConnection(Func<Boolean> function)
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

                    function();

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
        }
        Boolean LogicLoad()
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
                string sqlForCylindersImport = "SELECT * FROM cylinder_catalogue";

                MySqlCommand cmdForImportingCylinders = new MySqlCommand(sqlForCylindersImport, conn);
                MySqlDataReader readerForImportingCylinders = cmdForImportingCylinders.ExecuteReader();

                while (readerForImportingCylinders.Read())
                {
                    comboxCylinders.Items.Add(readerForImportingCylinders[0].ToString());
                    cylinderEffectiveAreasMetric.Add(Convert.ToDecimal(readerForImportingCylinders[1]));
                    cylinderEffectiveAreasImperial.Add(Convert.ToDecimal(readerForImportingCylinders[2]));
                }

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

                conn.Close();

                return true;

            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
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
            decimal conversionFactorMass;

            if (imperial)
            {
                conversionFactor = 0.3048M;
                conversionFactorMass = 1M / poundsPerKilogram;

            } else
            {
                conversionFactor = 3.28084M;
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
                    //UserInputs.ConvertUnits();
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
                    //UserInputs.ConvertUnits();
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
        }
        

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
        }

        void calculateCapacity()
        {

            if (UserInputs.PlatformWidth > 0 && UserInputs.PlatformLength > 0)
            {

                decimal platformArea = UserInputs.PlatformLength * UserInputs.PlatformWidth;
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
                        Lift.LoadingClass = "";
                        break;
                }

                Lift.MinCapacity = platformClassCapacity;


                if (UserInputs.Capacity > 0 && Lift.MinCapacity > 0)
                {
                    if (UserInputs.Capacity >= Lift.MinCapacity)
                    {
                        Lift.RequiredCapacity = UserInputs.Capacity;
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
            if (UserInputs.PlatformWidth > 0 && UserInputs.PlatformLength > 0)
            {

                decimal platformArea = UserInputs.PlatformWidth * UserInputs.PlatformLength;

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
                }
                else
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
            if (Lift.RequiredCapacity != -1 && Lift.PlatformMass != -1 && UserInputs.NumberOfCylinders > 0 && UserInputs.CylinderSelection != -1)
            {
                decimal platformMass = Lift.PlatformMass;

                decimal totalMass = platformMass + Lift.RequiredCapacity;

                decimal conversionFactor;

                decimal totalArea;

                if (unitsAreMetric)
                {
                    totalArea = Convert.ToDecimal(UserInputs.NumberOfCylinders) * cylinderEffectiveAreasMetric[UserInputs.CylinderSelection];
                    conversionFactor = AccelerationDueToGravity;
                }
                else
                {
                    totalArea = Convert.ToDecimal(UserInputs.NumberOfCylinders) * cylinderEffectiveAreasImperial[UserInputs.CylinderSelection];
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

        Boolean SaveFunction()
        {
            if (!isThisStringANumber(txtboxQuoteName.Text))
            {
                CreateQuoteLocal();
            }
            SaveQuoteLocal();
            return true;
        }

        //Saving
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sshConnection(SaveFunction);
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
                Quote.QuoteNumber = readerForQuoteNumber.GetInt16(0);
                txtboxQuoteName.Text = Convert.ToString(Quote.QuoteNumber);
                readerForQuoteNumber.Close();
                cmdForQuoteNumber.Dispose();
            }

            string sql = "UPDATE main SET ProjectDescription = @ProjectDescription, Date = @Date, Customer = @Customer, Contact = @Contact, LoadType = @LoadType, PitDepth = @PitDepth, TravelDistance = @TravelDistance, OverheadClearance = @OverheadClearance, Floors = @Floors, TravelSpeed = @TravelSpeed, PlatformWidth = @PlatformWidth, PlatformLength = @PlatformLength, InlineThrough = @InlineThrough, Capacity = @Capacity WHERE QuoteName = @QuoteName";
            

            MySqlCommand cmd = new MySqlCommand(sql, conn);

            //update
            cmd.Parameters.Add("@ProjectDescription", MySqlDbType.VarChar).Value = txtboxProjectDescription.Text;
            cmd.Parameters.Add("@Date", MySqlDbType.Date).Value = Convert.ToDateTime(dtpDate.Value.ToShortDateString());
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

        public static string CreateDxf(Boolean metricUnits, double dxfStartX, double dxfStartY)
        {
            double conversionFactor = 1;
            double PlatformThickness = .5;
            
            if (!metricUnits)
            {
                conversionFactor = 12;
                PlatformThickness = 12;
            }

            double PlatformLength = (Convert.ToDouble(UserInputs.PlatformLength) * conversionFactor);
            double PlatformWidth = (Convert.ToDouble(UserInputs.PlatformWidth) * conversionFactor);


            double PitDepth = (Convert.ToDouble(UserInputs.PitDepth) * conversionFactor);

            double TravelDistance = (Convert.ToDouble(UserInputs.TravelDistance) * conversionFactor);
            double OverheadCl = (Convert.ToDouble(UserInputs.OverheadClearance) * conversionFactor);

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

            numOfFloors = UserInputs.Floors;

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

            DateTime dateTime = DateTime.Now;

            return dateTime.ToString();

            void drawObject(List<Line> objectList, DxfDocument document)
            {
                for (int i = 0; i < objectList.Count; i++)
                {
                    document.AddEntity(objectList[i]);
                }
            }

            void drawDimension(List<LinearDimension> objectList, DxfDocument document)
            {
                for (int i = 0; i < objectList.Count; i++)
                {
                    document.AddEntity(objectList[i]);
                }
            }

        }
      
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewQuote(true);
        }

        void NewQuote(Boolean resetQuote)
        {
            //Application.Restart();
            
            
            comboxUnits.SelectedIndex = 0;
            comboxCustomer.SelectedIndex = -1;
            comboxContactName.SelectedIndex = -1;
            comboxContactName.Items.Clear();
            comboxLoadType.SelectedIndex = -1;
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

            if (resetQuote)
            {
                Quote.Reset();
                Lift.Reset();
                UserInputs.Reset();
            }
        }

        private void comboxCustomer_SelectedIndexChanged(object sender, EventArgs e)
        {
            int customerIndex = comboxCustomer.SelectedIndex;

            comboxContactName.Items.Clear();

            comboxContactName.Text = "";
            txtboxContactEmail.Text = "";
            txtboxContactPhone.Text = "";

            if (customerIndex != -1)
            {
                foreach (Contact contact in customers[customerIndex].Contacts)
                {
                    comboxContactName.Items.Add(contact.Name);
                }
            }
        }

        private void comboxContactName_SelectedIndexChanged(object sender, EventArgs e)
        {
            int customerIndex = comboxCustomer.SelectedIndex;
            int contactIndex = comboxContactName.SelectedIndex;

            txtboxContactEmail.Text = (customerIndex != -1 ? customers[customerIndex].Contacts[contactIndex].Email : "");
            txtboxContactPhone.Text = (customerIndex != -1 ? customers[customerIndex].Contacts[contactIndex].Phone : "");
        }

        private void buttonPDNext_Click(object sender, EventArgs e)
        {
            if (!timerNext.Enabled && !timerBack.Enabled)
            {
                timerNext.Start();
            }
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
            if (!timerNext.Enabled && !timerBack.Enabled)
            {
                timerBack.Start();
            }
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
            if (!timerNext.Enabled && !timerBack.Enabled)
            {
                timerNext.Start();
            }
        }

        private void buttonSCBack_Click(object sender, EventArgs e)
        {
            if (!timerNext.Enabled && !timerBack.Enabled)
            {
                timerBack.Start();
            }
        }

        private void buttonSCNext_Click(object sender, EventArgs e)
        {
            updateAllCalculations();
            if (!timerNext.Enabled && !timerBack.Enabled)
            {
                timerNext.Start();
            }
        }

        private void comboxLoadType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Lift.LoadingClass = "";
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
            UserInputs.CylinderSelection = comboxCylinders.SelectedIndex;
            updateAllCalculations();
        }

        private void comboxNumberOfCylinders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isThisStringANumber(comboxNumberOfCylinders.Text))
            {
                UserInputs.NumberOfCylinders = Convert.ToInt16(comboxNumberOfCylinders.Text);
            }
            updateAllCalculations();
        }

        private void buttonCylBack_Click(object sender, EventArgs e)
        {
            if (!timerNext.Enabled && !timerBack.Enabled)
            {
                timerBack.Start();
            }
        }

        private void liftSpecificationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SpecificationsForm SpecForm = new SpecificationsForm();
            SpecForm.Show();
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

            if (isThisStringANumber(txtboxPitDepth.Text))
            {
                UserInputs.PitDepth = Convert.ToDecimal(txtboxPitDepth.Text);
            }
            updateAllCalculations();
        }

        private void txtboxPlatformWidth_TextChanged(object sender, EventArgs e)
        {
            if (isThisStringANumber(txtboxPlatformWidth.Text))
            {
                UserInputs.PlatformWidth = Convert.ToDecimal(txtboxPlatformWidth.Text);
            }
            updateAllCalculations();
        }

        private void txtboxPlatformLength_TextChanged(object sender, EventArgs e)
        {
            if (isThisStringANumber(txtboxPlatformLength.Text))
            {
                UserInputs.PlatformLength = Convert.ToDecimal(txtboxPlatformLength.Text);
            }
            updateAllCalculations();
        }

        private void txtboxCapacity_TextChanged(object sender, EventArgs e)
        {
            if (isThisStringANumber(txtboxCapacity.Text))
            {
                UserInputs.Capacity = Convert.ToDecimal(txtboxCapacity.Text);
            }
            updateAllCalculations();
        }

        private void buttonDXF_Click(object sender, EventArgs e)
        {
            CreateDxf(unitsAreMetric, dxfStartX, dxfStartY);
        }

        private void txtboxTravelDis_TextChanged(object sender, EventArgs e)
        {
            if (isThisStringANumber(txtboxTravelDis.Text))
            {
                UserInputs.TravelDistance = Convert.ToDecimal(txtboxTravelDis.Text);
            }
                
        }

        private void comboxFloors_SelectedIndexChanged(object sender, EventArgs e)
        {
            UserInputs.Floors = Convert.ToInt16(comboxFloors.Text);
        }

        private void txtboxOverheadCl_TextChanged(object sender, EventArgs e)
        {
            if (isThisStringANumber(txtboxOverheadCl.Text))
            {
                UserInputs.OverheadClearance = Convert.ToDecimal(txtboxOverheadCl.Text);
            }
        }

        private void txtboxTravelSpeed_TextChanged(object sender, EventArgs e)
        {
            if (isThisStringANumber(txtboxTravelSpeed.Text))
            {
                UserInputs.TravelSpeed = Convert.ToDecimal(txtboxTravelSpeed.Text);
            }
        }

        private void comboxInlineThrough_SelectedIndexChanged(object sender, EventArgs e)
        {
            UserInputs.InlineThrough = comboxInlineThrough.Text;
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadQuote LoadingForm = new LoadQuote();
            LoadingForm.Show();
        }

        /*
        void updateLabels(string labelId, string labelText)
        {
            Controls.Find("labelUnit", true).FirstOrDefault();
        }
        */


        Tuple<decimal, string> ConvertUnits(decimal valueToConvert, ConversionFactor conversionFactor)
        {

            decimal convertedValue;

            convertedValue = valueToConvert * conversionFactor.Value;

            return Tuple.Create(convertedValue, conversionFactor.UnitAbbreviation);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(UserInputs.PitDepth.ConvertUnits("metres"));
        }
    }

    public class StandardUnit
    {
        public decimal Value { get; set; }
        public string Units { get; set; }
        public string UnitAbbreviation { get; set; }
        public Boolean MetricUnits { get; set; }

        public StandardUnit(decimal value, string units, string unitAbbreviation, Boolean metricUnits)
        {
            Value = value;
            Units = units;
            UnitAbbreviation = unitAbbreviation;
            MetricUnits = metricUnits;
        }

        public string ConvertUnits(string convertTo)
        {

            ConversionFactor ActiveConversionFactor;
            decimal OriginalValue = this.Value;
            string ConvertedFrom = this.UnitAbbreviation;

            switch (this.Units)
            {

                case "kilograms":
                    switch (convertTo)
                    {

                        case "kilograms":
                            return "Units are already Kilograms";
                        case "pounds":
                            ActiveConversionFactor = MainForm.KilogramsToPounds;
                            break;
                        case "mpa":
                            return "Cannot convert Kilograms to MPa";
                        case "psi":
                            return "Cannot convert Kilograms to PSI";
                        case "metres":
                            return "Cannot convert Kilograms to Metres";
                        case "feet":
                            return "Cannot convert Kilograms to Feet";
                        case "inches":
                            return "Cannot convert Kilograms to Inches";
                        default:
                            return "The requested conversion is not available";
                    }
                    break;
                case "pounds":
                    switch (convertTo)
                    {

                        case "kilograms":
                            ActiveConversionFactor = MainForm.PoundsToKilograms;
                            break;
                        case "pounds":
                            return "Units are already Pounds";
                        case "mpa":
                            return "Cannot convert to MPa";
                        case "psi":
                            return "Cannot convert to PSI";
                        case "metres":
                            return "Cannot convert to Metres";
                        case "feet":
                            return "Cannot convert to Feet";
                        case "inches":
                            return "Cannot convert to Inches";
                        default:
                            return "The requested conversion is not available";
                    }
                    break;
                case "mpa":
                    switch (convertTo)
                    {

                        case "kilograms":
                            return "Cannot convert to Kilograms";
                        case "pounds":
                            return "Cannot convert to Pounds";
                        case "mpa":
                            return "Units are already MPa";
                        case "psi":
                            ActiveConversionFactor = MainForm.MpaToPsi;
                            break;
                        case "metres":
                            return "Cannot convert to Metres";
                        case "feet":
                            return "Cannot convert to Feet";
                        case "inches":
                            return "Cannot convert to Inches";
                        default:
                            return "The requested conversion is not available";
                    }
                    break;
                case "psi":
                    switch (convertTo)
                    {

                        case "kilograms":
                            return "Cannot convert to Kilograms";
                        case "pounds":
                            return "Cannot convert to Pounds";
                        case "mpa":
                            ActiveConversionFactor = MainForm.PsiToMpa;
                            break;
                        case "psi":
                            return "Units are already PSI";
                        case "metres":
                            return "Cannot convert to Metres";
                        case "feet":
                            return "Cannot convert to Feet";
                        case "inches":
                            return "Cannot convert to Inches";
                        default:
                            return "The requested conversion is not available";
                    }
                    break;
                case "metres":
                    switch (convertTo)
                    {

                        case "kilograms":
                            return "Cannot convert to Kilograms";
                        case "pounds":
                            return "Cannot convert to Pounds";
                        case "mpa":
                            return "Cannot convert to MPa";
                        case "psi":
                            return "Cannot convert to PSI";
                        case "metres":
                            return "Units are already Metres";
                        case "feet":
                            ActiveConversionFactor = MainForm.MetresToFeet;
                            break;
                        case "inches":
                            return "Cannot convert to Inches";
                        default:
                            return "The requested conversion is not available";
                    }
                    break;
                case "feet":
                    switch (convertTo)
                    {

                        case "kilograms":
                            return "Cannot convert to Kilograms";
                        case "pounds":
                            return "Cannot convert to Pounds";
                        case "mpa":
                            return "Cannot convert to MPa";
                        case "psi":
                            return "Cannot convert to PSI";
                        case "metres":
                            ActiveConversionFactor = MainForm.FeetToMetres;
                            break;
                        case "feet":
                            return "Units are already Feet";
                        case "inches":
                            ActiveConversionFactor = MainForm.FeetToInches;
                            break;
                        default:
                            return "The requested conversion is not available";
                    }
                    break;
                case "inches":
                    switch (convertTo)
                    {

                        case "kilograms":
                            return "Cannot convert to Kilograms";
                        case "pounds":
                            return "Cannot convert to Pounds";
                        case "mpa":
                            return "Cannot convert to MPa";
                        case "psi":
                            return "Cannot convert to PSI";
                        case "metres":
                            return "Cannot convert to Metres";
                        case "feet":
                            ActiveConversionFactor = MainForm.InchesToFeet;
                            break;
                        case "inches":
                            return "Units are already Inches";
                        default:
                            return "The requested conversion is not available";
                    }
                    break;
                default:

                    return "The input units are invalid";
            }
            this.Value *= ActiveConversionFactor.Value;
            this.Units = ActiveConversionFactor.Units;
            this.UnitAbbreviation = ActiveConversionFactor.UnitAbbreviation;
            this.MetricUnits = ActiveConversionFactor.MetricUnits;

            return String.Format("Converted {0,4:.0000}{1} to {2,4:.0000}{3}", OriginalValue,ConvertedFrom,this.Value,this.UnitAbbreviation);

        }
        public override string ToString()
        {
            return String.Format("{0,4:.00}{1}", Value, UnitAbbreviation);
        }
    }

    public class ConversionFactor
    {
        public decimal Value { get; }
        public string Units { get; }
        public string UnitAbbreviation { get; }
        public Boolean MetricUnits { get; }

        public ConversionFactor(decimal value, string units, string unitAbbreviation, Boolean metricUnits)
        {
            Value = value;
            Units = units;
            UnitAbbreviation = unitAbbreviation;
            MetricUnits = metricUnits;
        }
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

    public static class Quote
    {
        public static short QuoteNumber { get; set; }
        public static short Revision { get; set; }
        //public static Customer ProjectCustomer { get; set; }
        public static string ProjectCustomer { get; set; }
        //public static Contact ProjectContact { get; set; }
        public static string ProjectContact { get; set; }
        public static string ProjectDescription { get; set; }

        public static void Reset()
        {
            QuoteNumber = -1;
            Revision = -1;
            ProjectCustomer = "";
            ProjectContact = "";
            ProjectDescription = "";
        }

    }

    public static class UserInputs
    {
        // Auto-implemented readonly property:
        public static string LoadType { get; set; }
        public static decimal PitDepth { get; set; }
        //public static StandardUnit PitDepth { get; set; }
        public static decimal OverheadClearance { get; set; }
        public static short Floors { get; set; }
        public static decimal TravelDistance { get; set; }
        public static decimal PlatformWidth { get; set; }
        public static decimal PlatformLength { get; set; }
        public static decimal Capacity { get; set; }
        public static decimal TravelSpeed { get; set; }
        public static string InlineThrough { get; set; }
        public static int CylinderSelection { get; set; }
        public static short NumberOfCylinders { get; set; }
        public static Boolean MetricUnits { get; set; }

        // Constructor that takes no arguments:
        static UserInputs()
        {
            LoadType = "";
            PitDepth = -1;
            //PitDepth = new StandardUnit(-1, "feet", "ft", false);
            OverheadClearance = -1;
            Floors = -1;
            TravelDistance = -1;
            PlatformWidth = -1;
            PlatformLength = -1;
            Capacity = -1;
            TravelSpeed = -1;
            InlineThrough = "";
            CylinderSelection = -1;
            NumberOfCylinders = -1;
            MetricUnits = false;
        }

        public static void Reset()
        {
            LoadType = "";
            PitDepth = -1;
            //PitDepth = new StandardUnit(-1, "feet", "ft", false);
            OverheadClearance = -1;
            Floors = -1;
            TravelDistance = -1;
            PlatformWidth = -1;
            PlatformLength = -1;
            Capacity = -1;
            TravelSpeed = -1;
            InlineThrough = "";
            CylinderSelection = -1;
            NumberOfCylinders = -1;
            MetricUnits = false;
        }

        /*
        public static void ConvertUnits()
        {
            if (MetricUnits)
            {
                PitDepth *= 3.28084M;
                OverheadClearance *= 3.28084M;
                TravelDistance *= 3.28084M;
                PlatformWidth *= 3.28084M;
                PlatformLength *= 3.28084M;
                Capacity *= 1 / 2.20462M;
                TravelSpeed *= 3.28084M;
                MetricUnits = false;
            }
            else
            {
                PitDepth /= 3.28084M;
                OverheadClearance *= 1 / 3.28084M;
                TravelDistance *= 1 / 3.28084M;
                PlatformWidth *= 1 / 3.28084M;
                PlatformLength *= 1 / 3.28084M;
                Capacity *= 2.20462M;
                TravelSpeed *= 1 / 3.28084M;
                MetricUnits = true;
            }
        }
        */

        public static string OutputString()
        {
            return "";
            //return string.Format("Here is some info about your lift:\nClass Year: {0}\nClass: {1}\nPlatform Mass: {2,4:.00}\nMinimum Capacity: {3,4:.00}\nRequired Capacity: {4,4:.00}\nEmpty Platform Static Pressure: {5,4:.00}\nEmpty Platform Dynamic Pressure: {6,4:.00}\nFull Load Static Pressure: {7,4:.00}\nFull Load Dynamic Pressure: {8,4:.00}", ClassYear, LoadingClass, PlatformMass, MinCapacity, RequiredCapacity, EmptyStaticPressure, EmptyDynamicPressure, FullStaticPressure, FullDynamicPressure);
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
}
