﻿using System.Windows.Forms;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace sniffMe
{
    public partial class Form1 : Form
    {
        public enum Protocol
        {
            TCP = 6,
            UDP = 17,
            Unknown = -1
        }

        private Socket socket; //собственно это и есть наше прослушивающее устройство
        private byte[] buffer; //а сюда мы будем записывать полученные пакеты

        private void Print(byte[] buf, int len)
        {
            string temp = string.Empty;
            for (int i = 0; i < len; i++)
            {
                temp += buf[i].ToString("X2");
                if ((i + 1) % 16 == 0)
                {
                    string txt = Encoding.ASCII.GetString(buf, i, 16);
                    temp += string.Format(" | {0}\n", txt);
                }
                else
                    temp += " ";
            }
            richTextBox1.BeginInvoke(new Action(() => richTextBox1.AppendText(Parse(buf, len) + temp)));
        }

        private string Parse(byte[] buf, int len)
        {
            IPHeader ipHeader = new IPHeader(buf, len);
            return string.Format("\n\n+---------------------------------------------+\n" +
            "|From: {0}\tTo: {1}\n" +
            "|Protocol: {3}\tLength: {2}\n" +
            "+---------------------------------------------+\n",
            ipHeader.SourceAddress,
            ipHeader.DestinationAddress,
            ipHeader.TotalLength,
            ipHeader.ProtocolType.ToString());
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int nReceived = socket.EndReceive(ar);
                Print(buffer, nReceived);
                buffer = new byte[4096];
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None,
                OnReceive, null);
            }
            catch { }
        }

        public Form1()
        {
            InitializeComponent();
            IPHostEntry HosyEntry = Dns.GetHostEntry((Dns.GetHostName()));
            if (HosyEntry.AddressList.Length > 0)
            {
                foreach (IPAddress ip in HosyEntry.AddressList)
                    comboBox1.Items.Add(ip.ToString());
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if ((sender as CheckBox).Checked)
            {
                (sender as CheckBox).Text = "&Stop me";
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                socket.Bind(new IPEndPoint(IPAddress.Parse(comboBox1.SelectedItem.ToString()), 0));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

                byte[] byInc = new byte[] { 1, 0, 0, 0 };
                byte[] byOut = new byte[4];
                buffer = new byte[4096];
                socket.IOControl(IOControlCode.ReceiveAll, byInc, byOut);
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, null);
            }
            else
            {
                socket.Close();
                (sender as CheckBox).Text = "&Start me";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }
    }
}
