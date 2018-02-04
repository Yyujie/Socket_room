using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace TalkToYou
{
    public partial class Form1 : Form
    {
        private static int port = 5200;
        public Form1()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            //关闭对文本框的非法线程操作检查
            TextBox.CheckForIllegalCrossThreadCalls = false;
            //string strlP = Dns.GetHostEntry("1w616580c9.iok.la").AddressList[0].ToString();
            //textBoxIP.Text = strlP;
            textBoxPort.Text = port.ToString();
        }
        //创建 1个客户端套接字 和1个负责监听服务端请求的线程
        Thread threadclient = null;
        Socket socketclient = null;
        
        private void buttonStart_Click(object sender, EventArgs e)
        {
            try
            {
                //SocketException exception;
                this.buttonStart.Enabled = false;
                //定义一个套接字监听
                socketclient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //获取文本框中的IP地址

                IPAddress address = IPAddress.Parse(textBoxIP.Text.Trim());



                //将获取的IP地址和端口号绑定在网络节点上
                IPEndPoint point = new IPEndPoint(address, int.Parse(textBoxPort.Text.Trim()));



                try
                {
                    //客户端套接字连接到网络节点上，用的是Connect
                    socketclient.Connect(point);
                }
                catch (Exception)
                {

                    MessageBox.Show("连接失败\r\n");
                    this.buttonStart.Enabled = true;
                    return;
                }
            }
            catch
            {
                
                MessageBox.Show("请重新确认IP/端口格式");
                this.buttonStart.Enabled = true;
                return;
            }
            byte[] name = Encoding.UTF8.GetBytes(textBoxName.Text);
            //调用客户端套接字发送字节数组   
            socketclient.Send(name);

            threadclient = new Thread(recv);

            threadclient.IsBackground = true;

            threadclient.Start(socketclient);
           
        }
        // 接收服务端发来信息的方法  
        private void recv(object socketserverpara)//
        {
            
            Socket socketServer = socketserverpara as Socket;
            while (true)//持续监听服务端发来的消息
            {
                try
                {
                    
                    //定义一个1M的内存缓冲区，用于临时性存储接收到的消息
                    byte[] arrRecvmsg = new byte[1024 * 1024];

                    //将客户端套接字接收到的数据存入内存缓冲区，并获取长度
                    int length = socketclient.Receive(arrRecvmsg);

                    //将套接字获取到的字符数组转换为人可以看懂的字符串
                    string strRevMsg = Encoding.UTF8.GetString(arrRecvmsg, 0, length);

                    string []s=strRevMsg.Split('，');
                                       
                     if (s[0] != textBoxName.Text)
                     {
                         if (strRevMsg == "")
                         {
                             socketServer.Close();
                             txtLog.AppendText(GetCurrentTime() + "\r\n" + "远程服务器已经中断连接" + "\r\n\n");
                             this.buttonStart.Enabled = true;
                             break;
                         }
                        txtLog.AppendText(GetCurrentTime() + "\r\n" + strRevMsg + "\r\n\n");
                        
                     }
                  
                }
                catch (Exception ex)
                {                  
                    txtLog.AppendText("远程服务器已经中断连接" + "\r\n\n");
                    this.buttonStart.Enabled = true;                    
                    break;
                }
            }
        }

        //获取当前系统时间
        private DateTime GetCurrentTime()
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }

        //发送字符信息到服务端的方法
        private void ClientSendMsg(string sendMsg)
        {
            //将输入的内容字符串转换为机器可以识别的字节数组   
            byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(textBoxName.Text+':'+sendMsg);
            //调用客户端套接字发送字节数组   
            socketclient.Send(arrClientSendMsg);
            //将发送的信息追加到聊天内容文本框中   
            txtLog.AppendText(GetCurrentTime() + "\r\n" + textBoxName.Text + ":" + sendMsg + "\r\n\n");
        }

        private void SendMessage_Click(object sender, EventArgs e)
        {
            //调用ClientSendMsg方法 将文本框中输入的信息发送给服务端   
            ClientSendMsg(richTextBoxSend.Text.Trim());
            richTextBoxSend.Clear();
        }

        private void richTextBoxSend_KeyDown(object sender, KeyEventArgs e)
        {
            //当光标位于文本框时 如果用户按下了键盘上的Enter键
            if (e.KeyCode == Keys.Enter)
            {
                //则调用客户端向服务端发送信息的方法  
                ClientSendMsg(richTextBoxSend.Text.Trim());
                richTextBoxSend.Clear();

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("是否退出？选否,最小化到托盘", "操作提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                
                this.Dispose();
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true;

            }
            else
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
                this.notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            base.Visible = true;
            this.notifyIcon1.Visible = false;
            this.ShowInTaskbar = true;          
            base.WindowState = FormWindowState.Normal;
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            richTextBoxSend.Clear();
        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret(); 
        }

       

    }
}
