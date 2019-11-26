
/**
 * Copyright (c) 2015 Ultra Communications Inc.  All rights reserved.
 * 
 * $Id: X80QC.cs 2128 2018-01-23 23:46:02Z vahid $
 **/

using System;
using System.Text;

namespace InnovativeICEApp
{
	/// <summary>
	/// This class provides high-level access to X80QC running Ultracomm firmware.
	/// </summary>
    public class X80QC
    {
        public const ushort MAX_OTDR_SAMPLE = 31;
        FuryInterface m_dev_if;
        CoreType m_core_type = CoreType.NONE;
        public bool radio_mode = false;
        public string radio_slave_serialNum = "0000";

        /// <summary>
        /// 
        /// </summary>
        public CoreType CORE_TYPE
        {
            get { return m_core_type; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Info INFO
        {
            get
            {
                string fw = System.Text.Encoding.ASCII.GetString(Query(Cmd.FIRMWARE_ID, 16));

                switch (fw.Substring(0, 8))
                {
                    case "185-0009":
                    case "185-0002":
                        m_core_type = CoreType.X80Q;
                        break;
                    case "185-0012":
                        m_core_type = CoreType.X80J;
                        break;
                    case "185-0004":
                        string rev = fw.Substring(9, 2);
                        if (int.Parse(rev) < 2)
                        {
                            m_core_type = CoreType.X80QFD;
                        }
                        else
                        {
                            m_core_type = GetStatus().QFD ? CoreType.X80QFD : CoreType.X80Q;
                        }
                        break;
                    case "185-0010":
                        m_core_type = CoreType.X80QARM;
                        break;
                    default:
                        m_core_type = CoreType.NONE;
                        break;
                }
                return new Info(Query(Cmd.INFO, Info.Length));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte TRANSCEIVER_SEL
        {
            get
            {
                byte selected_transceiver = 0;
                try
                {
                    selected_transceiver = (byte)Query(Cmd.TRANSCEIVER_SEL, 1)[0];
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return selected_transceiver;
            }
            set { Write(Cmd.TRANSCEIVER_SEL, new byte[] { (byte)value }); }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte TRANSCEIVERS_NUM
        {
            get
            {
                byte transceivers_num = 0;
                try
                {
                    transceivers_num = (byte)Query(Cmd.TRANSCEIVERS_NUM, 1)[0];
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return transceivers_num;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RADIO_ENABLE
        {
            get
            {
                return radio_mode;
            }

            set
            {
                radio_mode = value;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public string FURY_RADIO_SERIAL_SEL
        {
            get
            {
                return radio_slave_serialNum;
            }

            set
            {
                radio_slave_serialNum = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RADIO_SLAVE_SELECT
        {
            get
            {
                string radio_slave_select;
                try
                {
                    radio_slave_select = System.Text.Encoding.ASCII.GetString(Query(Cmd.RADIO_SERVER, new byte[] { (byte)RADIOCmds.RADIO_SLAVE_SELECT }, 4));
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return radio_slave_select;
            }
            set
            {
                try
                {
                    byte[] slave_list = System.Text.Encoding.ASCII.GetBytes(value);
                    byte [] cmds = new byte[slave_list.Length + 1];
                    cmds[0] = (byte)RADIOCmds.RADIO_SLAVE_SELECT;

                    Array.Copy(slave_list, 0, cmds, 1, slave_list.Length);
                    RADIO_ENABLE = false;
                    Query(Cmd.RADIO_SERVER, cmds, 4);
                    RADIO_ENABLE = true;
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string[] RADIO_SLAVE_LIST
        {
            get
            {
                string[] radio_slave_list;
                byte[] resp;
                try
                {
                    resp = Query(Cmd.RADIO_SERVER, new byte[] { (byte)RADIOCmds.RADIO_SLAVE_LIST }, -1);

                    int length = resp.Length / 4;
                    radio_slave_list = new string[length];
                    int j = 0;
                    byte[] str = new byte[4];
                    for(int i = 0; i<length; i++)
                    {
                        Array.Copy(resp, 4*i, str, 0, 4 );
                        radio_slave_list[j++] = System.Text.Encoding.ASCII.GetString(str);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return radio_slave_list;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public UInt16 RADIO_SLAVE_LIST_NUM
        {
            get
            {
                UInt16 radio_slave_num = 0;
                try
                {
                    radio_slave_num = new ByteAssembler(Query(Cmd.RADIO_SERVER, new byte[] { (byte)RADIOCmds.RADIO_SLAVE_LIST_NUM }, 2), ByteAssembler.Order.LE, 0).u16;
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return radio_slave_num;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte RADIO_SLAVE_PA_PWRLVL
        {
            get
            {
                byte powerLevel = 0x00;
                try
                {
                    powerLevel =  Query(Cmd.RADIO_PWR_LVL, new byte[] { }, 1)[0];
                }catch(Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }

                return powerLevel;
            }
            set
            {
                try
                {
                    Write(Cmd.RADIO_PWR_LVL, new byte[] {value});
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public byte RADIO_SLAVE_PA_TCLVL
        {
            get
            {
                byte tcLevel = 0x00;
                try
                {
                    tcLevel = Query(Cmd.RADIO_TC_LVL, new byte[] { }, 1)[0];
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }

                return tcLevel;
            }
            set
            {
                try
                {
                    Write(Cmd.RADIO_TC_LVL, new byte[] { value });
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte MalbecChipAddr
        {
            get
            {
                byte chip_address = 0xe0;
                try
                {
                    if (INFO.m_cmd_ver > 0)
                    {
                        chip_address = (byte)Query(Cmd.CHIP_ADDR, 1)[0];
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return chip_address;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte MuscatChipAddr
        {
            get
            {
                byte chip_address = 0xc4;
                try
                {
                    chip_address = (byte)Query(Cmd.CHIP_ADDR, 1)[0];
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return chip_address;
            }
        }
       
        /// <summary>
        /// 
        /// </summary>
        public Mode MODE
        {
            get {
                Mode mode;
                try
                {
                    mode = (Mode)Query(Cmd.MODE, 1)[0];
                }
                catch(Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                }
                return mode;
            }
            set { Write(Cmd.MODE, new byte[] { (byte)value }); }	// send command+arg
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="mode"></param>
        public void SetTxMode(byte channel, Mode mode)
        {
            Write(Cmd.TXMODE, new byte[] { channel, (byte)mode });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public Mode GetTxMode(byte channel)
        {
            Mode mode;
            try
            {
                mode = (Mode)Query(Cmd.TXMODE, new byte[] { channel }, 1)[0];
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("{0}", ex.Message));
            }

            return mode;
        }


        /// <summary>
        /// 
        /// </summary>
        public Single MCU_TEMP
        {
            get { return new ByteAssembler(Query(Cmd.TEMP, new byte[] { (byte)TempRead.MCU }, 4), (INFO.m_cfg_ver > 127) ? ByteAssembler.Order.LE : ByteAssembler.Order.BE, 0).fval; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Single DUT_CURRENT
        {
            get { return new ByteAssembler(Query(Cmd.BURNIN, new byte[] { (byte)BURNINCMD.BURNIN_DUT_IDD }, 4), (INFO.m_cfg_ver > 127) ? ByteAssembler.Order.LE : ByteAssembler.Order.BE, 0).fval; }
        }

        /// <summary>
        /// 
        /// </summary>
        public Single DUT_TEMP
        {
            get { return new ByteAssembler(Query(Cmd.BURNIN, new byte[] { (byte)BURNINCMD.BURNIN_VOLT_REG_TEMP }, 4), (INFO.m_cfg_ver > 127) ? ByteAssembler.Order.LE : ByteAssembler.Order.BE, 0).fval; }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte DUTPRESENT
        {
            get { return Query(Cmd.BURNIN, new byte[] { (byte)BURNINCMD.BURNIN_DUT_PRES }, 1)[0]; }
        }
        /// <summary>
        /// 
        /// </summary>
        public byte[] SERIAL_NO
        {
            get { return Query(Cmd.SERIAL_NO, 16); }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte TX_DISABLE
        {
            get { return Query(Cmd.TX_DISABLE, 1)[0]; }
            set { Write(Cmd.TX_DISABLE, new byte[] { value }); }	// send command+args
        }

        /// <summary>
        /// 
        /// </summary>
        public byte RX_DISABLE
        {
            get { return Query(Cmd.RX_DISABLE, 1)[0]; }
            set { Write(Cmd.RX_DISABLE, new byte[] { value }); }	// send command+args
        }

        /// <summary>
        /// 
        /// </summary>
        public byte SQUELCH
        {
            get { return Query(Cmd.SQUELCH, 1)[0]; }
            set { Write(Cmd.SQUELCH, new byte[] { value }); }	// send command+args
        }

        /// <summary>
        /// 
        /// </summary>
        public byte RXSIGNALDETECT
        {
            get { return Query(Cmd.RXSIGNALDETECT, 1)[0]; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TXDIS
        {
            get { return Query(Cmd.TXDIS, 1)[0] != 0; }
            set { Write(Cmd.TXDIS, new byte[] { (byte)(value ? 1 : 0) }); }	// send command+args
        }

        /// <summary>
        /// 
        /// </summary>
        public byte[] ProductCode
        {
            get { return Query(Cmd.PRODUCT_CODE, 16); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enable"></param>
        public bool FirmwareEn
        {
            get { return Query(Cmd.FIRMWARE_EN, 1)[0] != 0; }
            set { Write(Cmd.FIRMWARE_EN, new byte[] { (byte)(value ? 1 : 0), 0 }); }
        }

        /// <summary>
        /// 
        /// </summary>
        public byte[] FirmwareID
        {
            get {return Query(Cmd.FIRMWARE_ID, 16);}
        }

        /// <summary>
        /// 
        /// </summary>
        public byte NUMRATES
        {
            get { return (byte)Query(Cmd.NUM_RATE, 1)[0]; }
        }

        public string[] RATESLIST
        {
            get { 
                byte [] rates_byte_arr =  Query(Cmd.RATE_LIST, NUMRATES * 16);

                string[] rate_str_arr = new string[NUMRATES];

                for(int i = 0; i < NUMRATES; i++)
                {
                    byte[] tmpArr = new byte[16];
                    Array.Copy(rates_byte_arr, i * 16, tmpArr, 0, 16);
                    rate_str_arr[i] = System.Text.Encoding.ASCII.GetString(tmpArr);
                }

                return rate_str_arr;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public byte GetRXRate(byte ch)
        {
            return (byte)Query(Cmd.RXRATE_SEL, new byte[] { ch }, 1)[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="rateIndex"></param>
        public void SetRXRate(byte ch, byte rateIndex)
        {
            Write(Cmd.RXRATE_SEL, new byte[] { ch, rateIndex });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public byte GetTXRate(byte ch)
        {
            return (byte)Query(Cmd.TXRATE_SEL, new byte[] { ch }, 1)[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="rateIndex"></param>
        public void SetTXRate(byte ch, byte rateIndex)
        {
            Write(Cmd.TXRATE_SEL, new byte[] { ch, rateIndex });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dev_if"></param>
        public X80QC(FuryInterface dev_if)
        {

            m_dev_if = dev_if;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CheckAttachment()
        {
            bool result;

            try
            {
                Info info = INFO;
                result = true;
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

                
        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {
            Write(Cmd.RESET, new byte[] { 0xa3 });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chip_addr"></param>
        /// <param name="addr"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool SPI_Write(byte chip_addr, byte addr, byte val)
        {
            byte[] cmd_args = new byte[] { chip_addr, addr, val };
            return (SYSIO.SYSIO_STATUS)Query(Cmd.SPI_WRITE, cmd_args, 1)[0] == SYSIO.SYSIO_STATUS.SYSIO_STATUS_OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chip_addr"></param>
        /// <param name="addr"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool SPI_Read(byte chip_addr, byte addr, out byte val)
        {
            byte[] cmd_args = new byte[] { chip_addr, addr };

            byte[] buf = Query(Cmd.SPI_READ, cmd_args, 2);
            val = buf[1];

            return (SYSIO.SYSIO_STATUS)buf[0] == SYSIO.SYSIO_STATUS.SYSIO_STATUS_OK;	// return true if successful
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mux"></param>
        /// <param name="val"></param>
        public void ADC(ADC_MUX mux, out UInt16 val)
        {
            val = new ByteAssembler(Query(Cmd.SCI_ADC_READ, new byte[] { (byte)mux }, 2), (INFO.m_cfg_ver > 127) ? ByteAssembler.Order.LE : ByteAssembler.Order.BE, 0).u16;
        }

        public void VBIT(ADC_MUX mux, out float fval)
        {
            fval = new ByteAssembler(Query(Cmd.VBIT_MEAS, new byte[] { (byte)mux, 27, 16 }, 4), (INFO.m_cfg_ver > 127) ? ByteAssembler.Order.LE : ByteAssembler.Order.BE, 0).fval;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public Single BIT(byte[] args)
        {

            return new ByteAssembler(Query(Cmd.BIT_MEASURE, args, 4), (INFO.m_cfg_ver > 127) ? ByteAssembler.Order.LE : ByteAssembler.Order.BE, 0).fval;
        }

        /// <summary>
        /// @brief command to the MCU to clear out the BIT history
        /// </summary>
        /// <param name="args"></param>
        public void Clear_BITHistory()
        {
             Write(Cmd.BIT_MEASURE, new byte[] { (byte)BIT_Measurement.READ_CLRHIST, 0 });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Status GetStatus()
        {

            return new Status(Query(Cmd.STATUS, 2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetString(byte[] bytes)
        {
            string str = System.Text.Encoding.ASCII.GetString(bytes);
            int nul_idx = str.IndexOf('\0');							// find nul terminator, if any
            return nul_idx >= 0 ? str.Substring(0, nul_idx) : str;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="cmd_args"></param>
        /// <param name="response_length"></param>
        /// <returns></returns>
        private byte[] Query(Cmd cmd, byte[] cmd_args, int response_length)
        {
            byte[] resp;
            lock (this)
            {
                if (RADIO_ENABLE == true)
                {
                    byte[] slave_list = System.Text.Encoding.ASCII.GetBytes(FURY_RADIO_SERIAL_SEL);
                    byte[] temp_args = cmd_args;
                    cmd_args = new byte[cmd_args.Length + FURY_RADIO_SERIAL_SEL.Length];
                    Array.Copy(slave_list, cmd_args, slave_list.Length);
                    Array.Copy(temp_args, 0, cmd_args, FURY_RADIO_SERIAL_SEL.Length, temp_args.Length);
                    RADIO_SLAVE_SELECT = FURY_RADIO_SERIAL_SEL;
                }

                m_dev_if.Query(cmd, cmd_args, response_length, out resp);
            }
            return resp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="response_length"></param>
        /// <returns></returns>
        private byte[] Query(Cmd cmd, int response_length)
        {
            byte[] resp = null;

            lock (this)
            {
                try
                {
                    byte[] cmd_args = new byte[0];
                    if (RADIO_ENABLE == true)
                    {
                        byte[] slave_list = System.Text.Encoding.ASCII.GetBytes(FURY_RADIO_SERIAL_SEL);
                        byte[] temp_args = cmd_args;
                        cmd_args = new byte[cmd_args.Length + FURY_RADIO_SERIAL_SEL.Length];
                        Array.Copy(slave_list, cmd_args, slave_list.Length);
                        Array.Copy(temp_args, 0, cmd_args, FURY_RADIO_SERIAL_SEL.Length, temp_args.Length);
                        RADIO_SLAVE_SELECT = FURY_RADIO_SERIAL_SEL;
                    }

                    m_dev_if.Query(cmd, cmd_args, response_length, out resp);
                }catch(Exception ex)
                {
                    throw new Exception(String.Format("{0}", ex.Message));
                   // System.Windows.Forms.MessageBox.Show(string.Format("Serial Query Exception.  {0}", ex.Message));
                }
            }
            return resp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="data"></param>
        private void Write(Cmd cmd, byte[] cmd_args)
        {
            lock (this)
            {
                if (RADIO_ENABLE == true)
                {
                    byte[] slave_list = System.Text.Encoding.ASCII.GetBytes(FURY_RADIO_SERIAL_SEL);
                    byte[] temp_args = cmd_args;
                    cmd_args = new byte[cmd_args.Length + FURY_RADIO_SERIAL_SEL.Length];
                    Array.Copy(slave_list, cmd_args, slave_list.Length);
                    Array.Copy(temp_args, 0, cmd_args, FURY_RADIO_SERIAL_SEL.Length, temp_args.Length);
                    RADIO_SLAVE_SELECT = FURY_RADIO_SERIAL_SEL;
                }

                m_dev_if.Write(cmd, cmd_args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class Info
        {
            public byte m_cmd_ver;
            public byte m_cfg_ver;
            public static int Length { get { return 2; } }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="buf"></param>
            public Info(byte[] buf)
            {

                m_cmd_ver = buf[0];
                m_cfg_ver = buf[1];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class Status
        {
            byte[] m_status;

            public bool RESET { get { return (m_status[1] & 0x80) != 0; } }
            public bool QFD { get { return (m_status[1] & 0x40) != 0; } }   // valid if rev 02 firmware (or later)
            public bool REG_TEST_FAIL { get { return (m_status[1] & 0x0f) != 0; } }
            public bool TRANSCEIVER_PASS { get { return (m_status[1] & 0x4) == 0;  } }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="status"></param>
            public Status(byte[] status)
            {

                m_status = status;
            }
        }
    }


    
    /// <summary>
    /// 
    /// </summary>
    public enum CoreType
    {
        NONE,
        X80Q,      // external C8051F560 MCU
        X80QFD,    // external C8051F553 MCU 
        X80QC,     // internal C8051F930 MCU
        X80J,       // external C8051F553 MCU 
        X80QARM   //ARM Impl
    }

    /// <summary>
    /// 
    /// </summary>
	public enum Cmd : byte
	{
		// basic transceiver commands
		INFO,					// query transceiver info
		FIRMWARE_ID,			// read part number
		SERIAL_NO,				// read serial number
		PRODUCT_CODE,			// read product code (customer field)
		STATUS,					// read status (fault, etc.)
		RESET,					// reset transceiver (and MCU, if applicable)
		TEMP,					// calibrated temperature reading
		MODE,					// operating mode (ex. apc, test mode)
		CHIP_ADDR,				// read malbec chip address
        RXSIGNALDETECT,			// read Rx signal detect status
        SQUELCH,				// disable/query squelch for individual rx channel(s)
		RX_DISABLE,				// disable/query individual rx channel(s)
		TX_DISABLE,				// disable/query individual tx channel(s)

		CONFIG,					// configuration memory read/write/erase
		TXDIS,					// controls TXDIS signal to Malbec
		SPI_READ,				// SPI read cycle
		SPI_WRITE,				// SPI write cycle

		BIT_MEASURE,			// BIT measurement (raw)
		BIT_IA_CONTROL,			// IA gain/offset
		BIT_RESET_FLAG,			// read reset flag (unexpected resets)

		SCI_ADC_READ,			// raw ADC read
		SCI_OVERRIDE,			// science mode: override sensor

        OTDR_CONFIG,			// configures transeiver for OTDR
        TRANSCEIVER_SEL,
        TRANSCEIVERS_NUM,
        VBIT_MEAS,
        VO_MEAS,
        TXMODE,
        NUM_RATE,
        RATE_LIST,
        RXRATE_SEL,
        TXRATE_SEL,
        BURNIN = 27,
        ADC_SCALER,
        FIRMWARE_EN,
        LAST,

        RADIO_PWR_LVL = 29,
        RADIO_TC_LVL = 30,
        RADIO_SERVER = 0xFF //RADIO Commands
	}

 
    public enum RADIOCmds : byte
    {
        RADIO_SLAVE_SELECT = 0x00,
        RADIO_SLAVE_LIST = 0x01,
        RADIO_SLAVE_LIST_NUM = 0x02,
    }

    /// <summary>
    /// 
    /// </summary>
	public enum TempRead : byte
	{
		MCU,
		MALCBEC,
		MUSCAT
	}

    /// <summary>
    /// 
    /// </summary>
	public enum Mode
	{
		// note: these values map directly to firmware values (except for DEFAULT)
		APC,			// run transceiver
		FIXED_BIAS,		// run transceiver
		TEST,			// test mode (stop all Malbec register activity)
		PAUSED,			// PAUSE mode (stop all Malbec register activity except bias maintenance)
		BURNIN,			// VCSEL burn-in mode
		DEFAULT,		// use setting from config memory
        OTDR,
        CHMODE
	}

    /// <summary>
    /// 
    /// </summary>
	public enum OverrideSelect
	{
		TEMPERATURE,
		RSSI,
		TSSI,
		TMSI,
	}

#if true
    /// <summary>
    /// 
    /// </summary>
    public enum BIT_Measurement : byte
    {
        READ,
        SENSOR_READ,
        READ_CLRHIST
    }

    /// <summary>
    /// Sensor codes, must match firmware.
    /// </summary>
    public enum BIT_Sensor : byte
    {
        BIT_RMSI,   // + ch
        BIT_RSSI,   // + ch
        BIT_TMSI,   // + ch
        BIT_TSSI,   // + ch
        BIT_VVXL,   // + ch
        BIT_TEMP,
        BIT_VDDMIN,
        BIT_VDDMAX,

        TMSI,		// + ch
        TSSI,		// + ch
        TXITST,
        TVXL,
        VBG,
        VPTAT,
        VDDM,
        VDDC,
        VDDLFL,
        V_ITEST1,

        RMSI1,		// + ch
        RMSI2,		// + ch
        VCMON,	    // + ch
        VCMON_REF,	// + ch
        IMAIN,		// + ch
        IBLEED,		// + ch
        IZERO,		// + ch
        IMAIN_IBLEED,	// + ch

        RMSI2_REF,
        IREF_FIXED_M,
        IREF_FIXED,
        IREF_1P8_M,
        IREF_1P8,
        IREF_0P6,
        IREF_OP,
        IREF_VE,
        IREF_AGC_TST,
        VREF,
        IA_OFFSET_RX,
        IA_OFFSET_TX,
        VSS,
        VDDLFR,
        VDDLFM,
        VDDA,
        VDDB,
        VDDL,
        V_ITEST2
    }

    public enum BURNINCMD : byte
    {
        // Supplemental commands
        BURNIN_DUT_PRES,
        BURNIN_DUT_IDD,
        BURNIN_VXXL_CURRENT,
        BURNIN_VOLT_REG_TEMP,
        BURNIN_OPTICAL_PWR,
        BURNIN_INST_OPTICAL_PWR,
        BURNIN_VREF,
        BURNIN_IREF,
        BURNIN_OPTICAL_PWR_CH,
        BURNIN_PHOTODETECTOR_SCALER,
        BURNIN_BIT_VVXL_not_used,
        BURNIN_BIT_INST_VVXL,
        BURNIN_BIT_IMAIN,
        BURNIN_REG_TEST,
        BURNIN_MCU_TMP,
        BURNIN_LIV,
        BURNIN_NONE
    };
#else
	public enum BIT_Mode
	{
		IA,
		MUXP,
		MUXN,
		CURRENT
	}

	public enum BIT_Measurement
	{
		RX_RMSI1,
		RX_RMSI2,
		RX_IMAIN,
		RX_IBLEED,
		RX_IZERO,
		RX_IMAIN_IBLEED,
		RX_IREF_FIXED,
		RX_IREF_1P8,
		RX_IREF_0P6,
		RX_IREF_OP,
		RX_IREF_VE,
		RX_IREF_AGC_TST,
		RX_IA_OFFSET,
		RX_RMSI2_REF,
		RX_VDDLFR,
		RX_VDDLFM,
		RX_VDDA,
		RX_VDDB,
		RX_VDDL,
		RX_V_ITEST2,

		TX_TMSI,
		TX_TSSI,
		TX_IREF_FIXED,
		TX_IREF_1P8,
		TX_TXITST,
		TX_IA_OFFSET,
		TX_TVXL,
		TX_VBG,
		TX_VPTAT,
		TX_VDDM,
		TX_VDDC,
		TX_VDDLFL,
		TX_V_ITEST1
	}
#endif
    /// <summary>
    /// 
    /// </summary>
    public enum Config
	{
		READ,					// read byte(s)
		WRITE,					// write byte(s)
		ERASE_PAGE,				// erase FLASH page
        CHECKSUM,               //configuration checksum
        LENGTH                  //length of the internal Config structure
	}

    /// <summary>
    /// 
    /// </summary>
    public enum ADC_MUX : byte
    {
#if true
        // 503 firmware
        VBIT1 = 0x01,
        VBIT2 = 0x02,
        VO = 0x03,
        TEMPERATURE = 0x30,
        GND = 0x32,
        VDD = 0x31
#else
        // 900 firmware
        VBIT1 = 0x80,
        VBIT2,
        TEMPERATURE,
        GND,
        VDD
#endif
    }
}
