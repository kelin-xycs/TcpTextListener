using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.IO;

namespace TcpTextListener
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Socket serverSocket;
        private MemoryStream stream = new MemoryStream();
        private object lockObj = new object();

        private void btnStart_Click(object sender, EventArgs e)
        {

            if (serverSocket != null)
            {
                WriteMsg("已启动监听 。");
                return;
            }

            try
            {
                string ipStr = txtIP.Text;
                int port = int.Parse(txtPort.Text);

                IPAddress ip = IPAddress.Parse(ipStr);
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(ip, port));  //绑定IP地址：端口
                serverSocket.Listen(10);    //设定最多10个排队连接请求

                WriteMsg("启动监听 " + serverSocket.LocalEndPoint.ToString() + " 成功 。");

                //通过Clientsoket发送数据
                Thread thread = new Thread(ListenClientConnect);
                thread.IsBackground = true;
                thread.Start();
            }
            catch(Exception ex)
            {
                WriteMsg(ex.ToString());
            }
        }

        private void ListenClientConnect()
        {
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                
                Thread thread = new Thread(ReceiveMessage);
                thread.IsBackground = true;
                thread.Start(clientSocket);
            }
        }

        private void ReceiveMessage(object clientSocket)
        {
            Socket myClientSocket = (Socket)clientSocket;

            byte[] b = new byte[1024];

            while (true)
            {
                try
                {
                    //通过clientSocket接收数据
                    int receiveCount = myClientSocket.Receive(b);

                    //  receiveCount == 0 表示 客户端 连接 已关闭
                    if (receiveCount == 0)
                    {
                        break;
                    }

                    lock (lockObj)
                    {
                        stream.Write(b, 0, receiveCount);
                    }

                    if (receiveCount > 0)
                    {
                        this.Invoke(new Action<string>(WriteMsg), "接收到 " + receiveCount + " 个 Byte 的数据 。");
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action<string>(WriteMsg), "ReceiveMessage Error: " + ex.ToString());
                    myClientSocket.Shutdown(SocketShutdown.Both);
                    myClientSocket.Close();
                    break;
                }
            }
        }


        private void btnShowText_Click(object sender, EventArgs e)
        {            
            string s;

            lock (lockObj)
            {
                stream.Position = 0;

                StreamReader sr = new StreamReader(stream);

                s = sr.ReadToEnd();

                stream.Dispose();
                stream = new MemoryStream();
            }

            WriteMsg("接收到的文本是 ：" + s);   
        }

        private void WriteMsg(string msg)
        {
            txtMsg.AppendText(DateTime.Now.ToString("HH:mm:ss") + "\r\n" + msg + "\r\n\r\n");
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                string ipStr = txtIP.Text;
                int port = int.Parse(txtPort.Text);

                IPAddress ip = IPAddress.Parse(ipStr);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                s.Connect(new IPEndPoint(ip, port)); //配置服务器IP与端口

                s.Send(Encoding.UTF8.GetBytes("Hello Server !  This a test message from client ."));

                s.Dispose();
            }
            catch(Exception ex)
            {
                WriteMsg(ex.ToString());
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtMsg.Clear();
        }
    }
}
