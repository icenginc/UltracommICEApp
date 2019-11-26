/**
 * Copyright (c) 2015 Ultra Communications Inc.  All rights reserved.
 * 
 * $Id: FuryInterface.cs 1173 2016-07-22 22:42:23Z vahid $
 **/

using System;
using UltraCommunications.Hardware.Fury;
using UltraCommunications.Hardware;

namespace InnovativeICEApp
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    delegate void WarningDelegate(string msg);

	/// <summary>
	/// This is the Fury firmware interface.
	/// </summary>
	public interface FuryInterface
	{
		void Write(Cmd cmd, byte[] buf);
		void Read(Cmd cmd, byte[] buf);
        void Query(Cmd cmd, byte[] args, int resp_len, out byte[] resp);
		void Disconnect();
	}

    /// <summary>
	/// This is the Innovative firmware interface.
	/// </summary>
	public interface InnovativeInterface
    {
        void GPIO(int DUT);
        void PSU(int state, int phase, byte[] voltage);
		short[] ADCPOLL();
		byte[] PSUPOLL();
		void WRITEMAILBOX(string mailboxName, string mailboxContents);
		string READMAILBOX(string mailboxName);
    }
    /// <summary>
    /// Helper for getting list of attached Aardvarks.
    /// </summary>

    /// <summary>
    /// 
    /// </summary>
    class FuryIceInterface : FuryInterface
    {
        ucdrv_fury_rmt m_fury;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        public FuryIceInterface(string endpoint)
        {
            try
            {
                m_fury = new ucdrv_fury_rmt(endpoint);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ice Interface exception ex = {0}, endpoint = {1}", ex.Message, endpoint));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="buf"></param>
        public void Write(Cmd cmd, byte[] buf)
        {
            m_fury.Write((byte)cmd, buf);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="buf"></param>
        public void Read(Cmd cmd, byte[] buf)
        {
            byte[] tmp = new byte[buf.Length];
            tmp = m_fury.Read((byte)cmd, buf.Length);
            Array.Copy(tmp, buf, buf.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <param name="resp_len"></param>
        /// <param name="resp"></param>
        public void Query(Cmd cmd, byte[] args, int resp_len, out byte[] resp)
        {
            resp = null;
            try
            {
                resp = m_fury.Query_CSharp((byte)cmd, args, resp_len);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(string.Format("Ice Query Exception.  {0}", ex.Message));

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            m_fury.Disconnect();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    class InnovativeIceInterface : InnovativeInterface
    {
        Innovative_rmt m_innovative_rmt;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        public InnovativeIceInterface(string endpoint)
        {
            try
            {
                m_innovative_rmt = new Innovative_rmt(endpoint);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ice Interface exception ex = {0}, endpoint = {1}", ex.Message, endpoint));
            }
        }

        public void GPIO(int DUT)
        {
            m_innovative_rmt.GPIO(DUT);
        }

        public void PSU(int state, int phase, byte[] voltage)
        {
            m_innovative_rmt.PSU(state, phase, voltage);
        }

		public byte[] PSUPOLL()
		{
			byte[] ba = new byte[12];
			ba = m_innovative_rmt.PSUPOLL();
			return ba;
		}

		public short[] ADCPOLL()
		{
			short[] test = new short[6];
			test = m_innovative_rmt.ADCPOLL();
			return test;
		}

		public void WRITEMAILBOX(string mailboxName, string mailboxContents)
		{
			m_innovative_rmt.WRITEMAILBOX(mailboxName, mailboxContents);
		}

		public string READMAILBOX(string mailboxName)
		{
			string output = string.Empty;
			output = m_innovative_rmt.READMAILBOX(mailboxName);
			return output;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    class SYSIO
	{
        /// <summary>
        /// 
        /// </summary>
		public enum SYSIO_STATUS
		{
			SYSIO_STATUS_OK,
			SYSIO_STATUS_ERR
		}
	}
}
