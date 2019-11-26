using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Threading;

namespace InnovativeICEApp
{
	public partial class Form1 : Form
	{
		const string INNOVATIVE_SERVER_SERVICE_TYPE = "Innovative";
		const string FURY_SERVER_SERVICE_TYPE = "fury";
		const int PUBLISHER_PORT = 10099;
		Dictionary<string, string> m_innovative_remoteConnections = new Dictionary<string, string>();        // id, endoint
		Dictionary<string, string> m_fury_remoteConnections = new Dictionary<string, string>();        // id, endoint
		InnovativeInterface m_innovative_interface;
		Innovative_HAL m_innovative_hal;
		FuryInterface m_fury_interface;
		X80QC m_x80_hal;
		bool m_furyConnected = true;
		bool m_innovativeConnected = true;
		List<string> arguments = new List<string>();
		byte[] volts_ba = new byte[4];

		/// <summary>
		/// 
		/// </summary>
		public Form1(string[] args)
		{
			InitializeComponent();
			initiate_polling_elements();
			load_arguments(args);
			run_auto_commands();
			//Environment.Exit(0); //uncomment this for GUI use
		}

		private void load_arguments(string[] args)
		{
			foreach (string arg in args)
			{
				arguments.Add(arg);
				//Console.WriteLine(arg);
			}

			///example of arguments for writing mailbox
			//arguments.Add("WRITEMB");
			//arguments.Add("PACKAGE_EXCHANGE");
			//arguments.Add("NOOB"); <-- text to write


			///example of arguments for reading mailbox
			//arguments.Add("READMB");
			//arguments.Add("FURY");
		}

		private void initiate_polling_elements()
		{
			checkBox1.Enabled = false;
			checkBox2.Enabled = false;
			BackgroundWorker ADC_polling_worker = new BackgroundWorker();
			BackgroundWorker PSU_polling_worker = new BackgroundWorker();
			ADC_polling_worker.DoWork += ADC_Polling_worker_DoWork;
			PSU_polling_worker.DoWork += PSU_polling_worker_DoWork;

			ADC_polling_worker.RunWorkerAsync();
			PSU_polling_worker.RunWorkerAsync();
		}

		private void PSU_polling_worker_DoWork(object sender, DoWorkEventArgs e)
		{
			while (true)
			{
				try
				{
					this.Invoke((MethodInvoker)delegate
					{
						if (checkBox2.Checked == true)
						{
							var floats = do_psu_reads_float();

							PSU_ps0.Text = floats[0].ToString();
							PSU_PS1V.Text = floats[1].ToString();
							PSU_PS0I.Text = floats[2].ToString();
							PSU_PS1I.Text = floats[3].ToString();
							PSU_PS0C.Text = floats[4].ToString();
							PSU_PS1C.Text = floats[5].ToString();
						}
					});
				}
				catch { }
			System.Threading.Thread.Sleep(1000 * Int16.Parse(numericUpDown2.Value.ToString()));
			}
		}

		private void ADC_Polling_worker_DoWork(object sender, DoWorkEventArgs e)
		{
			while (true)
			{
				try
				{
					this.Invoke((MethodInvoker)delegate
					{
						if (checkBox1.Checked == true)
						{
							var floats = do_adc_reads_float();
							ADC_ps0.Text = floats[0].ToString();
							ADC_ps1.Text = floats[1].ToString();
							ADC_bib3v3.Text = floats[2].ToString();
							ADC_bib5v0.Text = floats[3].ToString();
							ADC_2v5ref.Text = floats[4].ToString();
							ADC_CNTL12I.Text = floats[5].ToString();
						}
					});
				}
				catch { }
				System.Threading.Thread.Sleep(1000 * Int16.Parse(numericUpDown1.Value.ToString()));
			}
		}

		private void run_auto_commands()
		{
			int DUT = 0;
			string voltage0 = "";
			string voltage1 = "";
			string command = "";

			try
			{
				command = arguments[0];
			}
			catch
			{
				command = "";
			} //set command blank for no arguments (GUI use)

			if (command != "")
			{
				call_InnovativeICEConnection();

				innovativeConnectBox.Checked = true;
			} //if command not blank, connect automatically

			// at this point the server is connected. now hit the PSU button with the right voltage
			if (command == "SETPSU")
				do_psu_set(arguments[1], arguments[2]);
			if (command == "SETDUT")
				do_DUT_set(arguments[1]);
			if (command == "ADCPOLL")
				do_adc_reads();
			if (command == "PSUPOLL")
				do_psu_reads();
			if (command == "READMB")
				do_mailbox_read(arguments[1]);
			if (command == "WRITEMB")
				do_mailbox_write(arguments[1], arguments[2]);


			/*
			var psu_poll = new byte[12];
			for (int i = 0; i < psu_poll.Length; i++)
			{
				psu_poll[i] = 0x00;
			}

			psu_poll = m_innovative_hal.PSUPOLL();
			float[] values = convert_to_float(psu_poll);

			Console.WriteLine("This is result of psu poll: "  + values[0] + " " + values[1] + " " + values[2]);
			*/
		}

		private void do_mailbox_read(string mailboxName)
		{
			Console.WriteLine("Reading mailbox: " + mailboxName);
			string output = string.Empty;
			output = m_innovative_hal.READMAILBOX(mailboxName);

			Console.WriteLine("Result: " + output);
		}

		private void do_mailbox_write(string mailboxName, string mailboxContents)
		{
			m_innovative_hal.WRITEMAILBOX(mailboxName, mailboxContents);
		}

		private string do_mailbox_read_gui(string mailboxName)
		{
			Console.WriteLine("Reading mailbox: " + mailboxName);
			string output = string.Empty;
			output = m_innovative_hal.READMAILBOX(mailboxName);
			//output = output.Replace(":", String.Empty);
			return output;
		}

		private void do_psu_reads()
		{
			string output = "";
			byte[] psu_reads = new byte[24];
			psu_reads = m_innovative_hal.PSUPOLL();


			var psu_reads_float = convert_to_float(psu_reads);

			for (int i = 0; i < psu_reads_float.Length; i++)
				output += psu_reads_float[i].ToString() + " ";

			Console.WriteLine(output);
		}

		private float[] do_psu_reads_float()
		{
			string output = "";
			byte[] psu_reads = new byte[24];
			psu_reads = m_innovative_hal.PSUPOLL();


			var psu_reads_float = convert_to_float(psu_reads);

			return psu_reads_float;
		}

		private void do_DUT_set(string DUT_string)
		{
			bool success = Int32.TryParse(DUT_string, out int DUT);
			if ((DUT < 21) && success)
			{
				if (m_innovativeConnected)
				{
					m_innovative_hal.Address(DUT);
				}
			}
		}

		private void do_psu_set(string voltage0, string voltage1)
		{
			//do first PSU
			voltage.Text = voltage0;
			phase.Text = "0";

			//if (voltage0 != "0")
			volts_ba = convert_to_byte(voltage0);
			psuButton_manual();

			//do second PSU

			voltage.Text = voltage1;
			phase.Text = "1";
			System.Threading.Thread.Sleep(500);
			//if (voltage1 != "0")
			volts_ba = convert_to_byte(voltage1);
			psuButton_manual();
		}

		private void do_adc_reads()
		{
			float constant = .0004394531F;
			short[] adc_reads = new short[6];
			adc_reads = m_innovative_hal.ADCPOLL();

			float[] adc_reads_float = new float[6];

			adc_reads_float[0] = (adc_reads[0] * 4 * constant);//phase 0
			adc_reads_float[1] = (adc_reads[1] * 4 * constant);//phase 1
			adc_reads_float[2] = (adc_reads[2] * 4 * constant);//bib 3v3
			adc_reads_float[3] = (adc_reads[3] * 4 * constant);//bib 5v0
			adc_reads_float[4] = (adc_reads[4] * 2.3F * constant);//uc2v5ref
			adc_reads_float[5] = (adc_reads[5] * 9.66F * constant);//amps

			for (int i = 0; i < adc_reads_float.Length; i++)
				Console.WriteLine(adc_reads_float[i] + " ");
		}

		private float[] do_adc_reads_float()
		{
			float constant = .0004394531F;
			short[] adc_reads = new short[6];
			adc_reads = m_innovative_hal.ADCPOLL();

			float[] adc_reads_float = new float[6];

			adc_reads_float[0] = (adc_reads[0] * 4 * constant * 1.025F);//phase 0
			adc_reads_float[1] = (adc_reads[1] * 4 * constant * 1.047F);//phase 1
			adc_reads_float[2] = (adc_reads[2] * 4 * constant * 1.0F);//bib 3v3
			adc_reads_float[3] = (adc_reads[3] * 4 * constant * 1.12F);//bib 5v0
			adc_reads_float[4] = (adc_reads[4] * 2.3F * constant * 1.05F);//uc2v5ref
			adc_reads_float[5] = (adc_reads[5] * 9.66F * constant *1.0F);//amps

			return adc_reads_float;
		}

		private byte[] convert_to_byte(string volts)
		{
			byte[] byte_array;
			float volts_float = float.Parse(volts);

			byte_array = BitConverter.GetBytes(volts_float);

			return byte_array;
		}

		private float[] convert_to_float(byte[] ba)
		{
			float[] values = new float[6];

			values[0] = BitConverter.ToSingle(ba, 0);
			values[1] = BitConverter.ToSingle(ba, 4);
			values[2] = BitConverter.ToSingle(ba, 8);
			values[3] = BitConverter.ToSingle(ba, 12);
			values[4] = BitConverter.ToSingle(ba, 16);
			values[5] = BitConverter.ToSingle(ba, 20);


			return values;
		}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FuryServerConnection_DropDown(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor; // Change the cursor state

            FuryServerConnection.Items.Clear();
            if (File.Exists("ServicePublisher.dll")) // Check for the .dll file
            {
                try
                {
                    // look for remote hosts
                    Util.Query(FuryServerConnection, m_fury_remoteConnections, FURY_SERVER_SERVICE_TYPE, "", PUBLISHER_PORT);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while querying for ice connections: " + ex.Message);
                }
            }

            Cursor.Current = Cursors.Default;
        }

        private void InnovativeICEConnection_DropDown(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor; // Change the cursor state
            InnovativeICEConnection.Items.Clear();
            if (File.Exists("ServicePublisher.dll")) // Check for the .dll file
            {
                try
                {
                     //look for remote hosts
                    Util.Query(InnovativeICEConnection, m_innovative_remoteConnections, INNOVATIVE_SERVER_SERVICE_TYPE, "", PUBLISHER_PORT);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while querying for ice connections: " + ex.Message);
                }
            }

            Cursor.Current = Cursors.Default;
        }

		private void call_InnovativeICEConnection()
		{
			InnovativeICEConnection.Items.Clear();
			if (File.Exists("ServicePublisher.dll")) // Check for the .dll file
			{
				try
				{
					//look for remote hosts
					Util.Query(InnovativeICEConnection, m_innovative_remoteConnections, INNOVATIVE_SERVER_SERVICE_TYPE, "", PUBLISHER_PORT);
				}
				catch (Exception ex)
				{
					MessageBox.Show("An error occurred while querying for ice connections: " + ex.Message);
				}
			}

			InnovativeICEConnection.Text = "BEAGLEBONE:ICEBURNIN";
			innovativeConnectBox.Checked = true;
			call_innovative_clickbox();
			

		} //implemented for command-line use

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void innovativeConnectBox_Click(object sender, EventArgs e)
        {
            //m_formMacomRegs.Enabled = false;
            string connection = null;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (innovativeConnectBox.Checked)
                {
                    connection = InnovativeICEConnection.Text;
					
                    if (connection.Length == 0)
                    {
						MessageBox.Show("Cannot connect without connection selection.");

						//throw new Exception("Cannot connect without Ember connection type selection.");
                    }

                   if (m_innovative_remoteConnections.ContainsKey(connection))
                    {
                        m_innovative_interface = new InnovativeIceInterface(m_innovative_remoteConnections[connection]);
                        connection = connection + ";" + m_innovative_remoteConnections[connection];
                    }

                    if (m_innovative_interface == null)
                    {
                        throw new Exception("ERROR: Unknown connection type.");
                    }
                    m_innovative_hal = new Innovative_HAL(m_innovative_interface);
                    m_innovativeConnected = true;

                    EnableInnovativeWidgets(true);
                }
                else
                {
                    system_InnovativeExceptionCleanUp();
                }
            }
            catch (Exception ex)
            {
                system_InnovativeExceptionCleanUp();
                if (ex.Message.StartsWith("Access is denied"))
                {
                    MessageBox.Show(string.Format("Exception raised - Can't open {0} (Already in use by another program?).", connection), "");
                }
                else
                {
                    MessageBox.Show(string.Format("Exception raised - ({0}).", ex.Message.Trim()), "");
                }
            }

            Cursor.Current = Cursors.Default;
        }


		private void call_innovative_clickbox()
		{
			string connection = null;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				if (innovativeConnectBox.Checked)
				{
					connection = InnovativeICEConnection.Text;
					//Console.WriteLine(connection);
					if (connection.Length == 0)
					{
						throw new Exception("Cannot connect without Ember connection type selection.");
					}

					if (m_innovative_remoteConnections.ContainsKey(connection))
					{
						m_innovative_interface = new InnovativeIceInterface(m_innovative_remoteConnections[connection]);
						connection = connection + ";" + m_innovative_remoteConnections[connection];
					}

					if (m_innovative_interface == null)
					{
						throw new Exception("ERROR: Unknown connection type.");
					}
					m_innovative_hal = new Innovative_HAL(m_innovative_interface);
					m_innovativeConnected = true;

					EnableInnovativeWidgets(true);
				}
				else
				{
					system_InnovativeExceptionCleanUp();
				}
			}
			catch (Exception ex)
			{
				system_InnovativeExceptionCleanUp();
				if (ex.Message.StartsWith("Access is denied"))
				{
					MessageBox.Show(string.Format("Exception raised - Can't open {0} (Already in use by another program?).", connection), "");
				}
				else
				{
					MessageBox.Show(string.Format("Exception raised - ({0}).", ex.Message.Trim()), "");
				}
			}
		}//manual call of clickbox funct for command line use
			/// <summary>
			/// 
			/// </summary>
			/// <param name="enable"></param>
			private void EnableInnovativeWidgets(bool enable)
        {
            if(enable == false)
            {
                furyConnectBox.Checked = false;
            }

            FuryServerConnection.Enabled = enable;
            furyConnectBox.Enabled = enable;

        }

        /// <summary>
        /// 
        /// </summary>
        private void system_InnovativeExceptionCleanUp() // Clean up the app when an exception is thrown
        {
            if (m_innovative_interface != null) // For interface
            {
                m_innovative_interface = null;
            }

            // Non specific clean-up
            m_innovative_hal = null;

            innovativeConnectBox.Checked = false;
            m_innovativeConnected = false;

            EnableInnovativeWidgets(false);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void furyConnectBox_Click(object sender, EventArgs e)
        {
            //m_formMacomRegs.Enabled = false;
            string connection = null;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (furyConnectBox.Checked)
                {
                    connection = FuryServerConnection.Text;

                    if (connection.Length == 0)
                    {
                        throw new Exception("Cannot connect without Ember connection type selection.");
                    }

                    if (m_fury_remoteConnections.ContainsKey(connection))
                    {
                        m_fury_interface = new FuryIceInterface(m_fury_remoteConnections[connection]);
                        connection = connection + ";" + m_fury_remoteConnections[connection];
                    }

                    if (m_fury_interface == null)
                    {
                        throw new Exception("ERROR: Unknown connection type.");
                    }
                    m_x80_hal = new X80QC(m_fury_interface);
                    m_furyConnected = true;

                    EnableFuryWidgets(true);

                    updateFuryInfo();

                }
                else
                {
                    system_FuryExceptionCleanUp();
                }
            }
            catch (Exception ex)
            {
                system_FuryExceptionCleanUp();
                if (ex.Message.StartsWith("Access is denied"))
                {
                    MessageBox.Show(string.Format("Exception raised - Can't open {0} (Already in use by another program?).", connection), "");
                }
                else
                {
                    MessageBox.Show(string.Format("Exception raised - ({0}).", ex.Message.Trim()), "");
                }
            }

            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enable"></param>
        private void EnableFuryWidgets(bool enable)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        private void system_FuryExceptionCleanUp() // Clean up the app when an exception is thrown
        {
            if (m_fury_interface != null) // For interface
            {
                m_fury_interface = null;
            }

            // Non specific clean-up
            m_x80_hal = null;

            furyConnectBox.Checked = false;
            m_furyConnected = false;

            EnableFuryWidgets(false);
        }

        /// <summary>
        /// 
        /// </summary>
        private void updateFuryInfo()
        {
			
            if (m_furyConnected)
            {
				m_x80_hal.FirmwareEn = true;

				string fw = System.Text.Encoding.ASCII.GetString(m_x80_hal.FirmwareID);
                firmewareID.Text = fw;
                mcuTemp.Text =  m_x80_hal.MCU_TEMP.ToString();
                IDD.Text = m_x80_hal.DUT_CURRENT.ToString();
                thermTemp.Text = m_x80_hal.DUT_TEMP.ToString();
                dutPresent.Checked = Convert.ToBoolean(m_x80_hal.DUTPRESENT);
                firmwareEN.Checked = m_x80_hal.FirmwareEn;
				
			}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void psuButton_Click(object sender, EventArgs e)
        {
			volts_ba = convert_to_byte(voltage.Value.ToString());
			Console.WriteLine(voltage.Value.ToString());


			if (m_innovativeConnected)
            {
                int state = 0;
                if (psuOFF.Checked == true)
                    state = 0;
                else state = 1;

                m_innovative_hal.PSU(state, Convert.ToInt32(phase.Value), volts_ba);
            }
        }

		private void psuButton_manual()
		{
			if (m_innovativeConnected)
			{
				int state = 0;
				if (psuOFF.Checked == true)
					state = 0;
				else state = 1;
				//put in here: if voltage is 0, then turn off state
				m_innovative_hal.PSU(state, Convert.ToInt32(phase.Value), volts_ba);
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void address_ValueChanged(object sender, EventArgs e)
        {
            if (m_innovativeConnected)
            {
                m_innovative_hal.Address((int)address.Value);
            }
        }

		private void InnovativeICEConnection_SelectedIndexChanged(object sender, EventArgs e)
		{
			
		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{

		}

		private void label10_Click(object sender, EventArgs e)
		{

		}

		private void innovativeConnectBox_CheckedChanged(object sender, EventArgs e)
		{
			checkBox1.Enabled = true;
			checkBox2.Enabled = true;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			do_mailbox_write("BI", textBox_setbi.Text);
		}//set bi

		private void button2_Click(object sender, EventArgs e)
		{
			do_mailbox_write("UC", textBox_setfury.Text);
		}//set fury

		private void button3_Click(object sender, EventArgs e)
		{
			do_mailbox_write("PACKAGE_EXCHANGE", textBox_setstring.Text);
		} //set string

		private void button6_Click(object sender, EventArgs e)
		{
			textBox_readbi.Text = do_mailbox_read_gui("BI");
		} //read bi

		private void button5_Click(object sender, EventArgs e)
		{
			textBox_readfury.Text = do_mailbox_read_gui("UC");
		} //read fury

		private void button4_Click(object sender, EventArgs e)
		{
			textBox_readstring.Text = do_mailbox_read_gui("PACKAGE_EXCHANGE");
		} //read string

        private void FirmwareEn_Click(object sender, EventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;

            m_x80_hal.FirmwareEn = checkbox.Checked;
        }

		private void firmwareEN_CheckedChanged(object sender, EventArgs e)
		{

		}

		private void button7_Click(object sender, EventArgs e)
		{
			string logfile = @"LogFile.txt";
			string logbody = null;

			TextBox[] textBoxes = new TextBox[] { textBox1, textBox2, textBox3, textBox4, textBox5, textBox6, textBox7, textBox8, textBox9, textBox10,
			textBox11, textBox12, textBox13, textBox14, textBox15, textBox16, textBox17, textBox18, textBox19, textBox20,
			textBox21, textBox22, textBox23, textBox24, textBox25, textBox26, textBox27, textBox28, textBox29, textBox30,
			textBox31, textBox32, textBox33, textBox34, textBox35, textBox36, textBox37, textBox38, textBox39, textBox40,
			textBox41, textBox42, textBox43, textBox44, textBox45, textBox46, textBox47, textBox48, textBox49, textBox50,
			textBox51, textBox52, textBox53, textBox54, textBox55, textBox56, textBox57, textBox58, textBox59, textBox60};

			if (!File.Exists(logfile))
			{
				File.Create(logfile);
			}

			try
			{
				this.Invoke((MethodInvoker)delegate
				{
					for (int x = 0; x < numericUpDown4.Value; x++)
					{
						for (int i = 0; i < 20; i++)
						{
							if (m_innovativeConnected)
							{
								m_innovative_hal.Address(i + 1);//set gpio mux
								address.Value = i + 1;
								address.Refresh();
								/*
								FuryServerConnection_DropDown(this, new EventArgs()); //click the dropdown to populate list
								for (int j = 0; j < FuryServerConnection.Items.Count; j++)
								{
									string selected = InnovativeICEConnection.Text;
									string resultString = Regex.Match(selected, @"\d+").Value;
									if (FuryServerConnection.Items[j].ToString().Contains(resultString))
										FuryServerConnection.SelectedIndex = j;
								}//select the right server based on the innovative box
								furyConnectBox.Checked = true;
								furyConnectBox_Click(this, new EventArgs()); //connect fury 
								*/
								m_x80_hal.FirmwareEn = true;

								string fw = System.Text.Encoding.ASCII.GetString(m_x80_hal.FirmwareID);
								firmewareID.Text = fw;
								firmewareID.Refresh();
								mcuTemp.Text = m_x80_hal.MCU_TEMP.ToString();
								mcuTemp.Refresh();
								IDD.Text = m_x80_hal.DUT_CURRENT.ToString();
								IDD.Refresh();
								thermTemp.Text = m_x80_hal.DUT_TEMP.ToString();
								thermTemp.Refresh();
								dutPresent.Checked = Convert.ToBoolean(m_x80_hal.DUTPRESENT);
								dutPresent.Refresh();
								firmwareEN.Checked = m_x80_hal.FirmwareEn;
								firmwareEN.Refresh();
								furyConnectBox_Click(this, new EventArgs()); //disconnect fury


								textBoxes[(3 * i)].Text = m_x80_hal.MCU_TEMP.ToString();
								textBoxes[(3 * i) + 1].Text = m_x80_hal.DUT_CURRENT.ToString();
								textBoxes[(3 * i) + 2].Text = m_x80_hal.DUT_TEMP.ToString();
								if (Convert.ToBoolean(m_x80_hal.DUTPRESENT))
								{
									textBoxes[(3 * i)].BackColor = Color.Gold;
									textBoxes[(3 * i) + 1].BackColor = Color.Gold;
									textBoxes[(3 * i) + 2].BackColor = Color.Gold;
								}
								else
								{
									textBoxes[(3 * i)].BackColor = Color.LightGray;
									textBoxes[(3 * i) + 1].BackColor = Color.LightGray;
									textBoxes[(3 * i) + 2].BackColor = Color.LightGray;
								}

								logbody += m_x80_hal.MCU_TEMP.ToString() + "," + m_x80_hal.DUT_CURRENT.ToString() + "," + m_x80_hal.DUT_TEMP.ToString() + ",";

							}
							else
								MessageBox.Show("Innovative server not connected");
						}
						if (logbody != null)
						{
							using (System.IO.StreamWriter filewriter = new System.IO.StreamWriter(logfile, true))
							{
								filewriter.WriteLine(DateTime.Now.ToString("MMMM dd yyyy hh:mm tt,") + logbody);
							}
							logbody = null;
						}
						if (x != numericUpDown4.Value - 1)
						{
							Thread.Sleep(Decimal.ToInt32(numericUpDown3.Value) * 1000);
						}
					}

				});
			}
			catch { }

		}
		


		private void textBox6_TextChanged(object sender, EventArgs e)
		{

		}

		private void textBox47_TextChanged(object sender, EventArgs e)
		{

		}

		private void textBox50_TextChanged(object sender, EventArgs e)
		{

		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void label28_Click(object sender, EventArgs e)
		{

		}

		private void label30_Click(object sender, EventArgs e)
		{

		}

		private void label38_Click(object sender, EventArgs e)
		{

		}

		private void checkBox3_CheckedChanged(object sender, EventArgs e)
		{

		}
		/*
protected override void SetVisibleCore(bool value)
{
if (!this.IsHandleCreated)
{
value = false;
CreateHandle();
}
base.SetVisibleCore(false);
}
*///to make form1 load invisible
	}
}
