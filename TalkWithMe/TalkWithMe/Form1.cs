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
using System.Collections;

namespace TalkWithMe
{
    public partial class Form1 : Form
    {
        private static string host = GetLocalIP();
        private static int port = 5200;
        private static Boolean Flag=false;
        

        public Form1()
        {
            InitializeComponent();        
            textBoxIP.Text = host;
            textBoxPort.Text = port.ToString();
            StartPosition = FormStartPosition.CenterScreen;

            //关闭对文本框的非线程操作检查
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }

        string RemoteEndPoint;     //客户端的网络结点       
        Thread threadwatch = null;//负责监听客户端的线程
        Socket socketwatch = null;//负责监听客户端的套接字
        //创建一个和客户端通信的套接字
        Dictionary<string, Socket> dic = new Dictionary<string, Socket> { };   //定义一个集合，存储客户端信息
        Dictionary<string, string> dicName = new Dictionary<string, string> { };   //昵称与客户端对应

        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取本机IP出错:" + ex.Message);
                return "";
            }
        }
       

        ///    
        /// 获取当前系统时间的方法   
        ///    
        /// 当前时间   
        private DateTime GetCurrentTime()
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }
                   
        private void buttonStart_Click(object sender, EventArgs e)
        {
            this.buttonStart.Enabled = false;
            //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议）
            socketwatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //服务端发送信息需要一个IP地址和端口号
            IPAddress address = IPAddress.Parse(textBoxIP.Text.Trim());//获取文本框输入的IP地址

            //将IP地址和端口号绑定到网络节点point上
            IPEndPoint point = new IPEndPoint(address, int.Parse(textBoxPort.Text.Trim()));//获取文本框上输入的端口号
            //此端口专门用来监听的

            //监听绑定的网络节点
            socketwatch.Bind(point);

            //将套接字的监听队列长度限制为20
            socketwatch.Listen(20);



            //创建一个监听线程
            threadwatch = new Thread(watchconnecting);



            //将窗体线程设置为与后台同步，随着主线程结束而结束
            threadwatch.IsBackground = true;

            //启动线程   
            threadwatch.Start();

            //启动线程后 textBox3文本框显示相应提示
            txtLog.AppendText(GetCurrentTime()+"\r\n"+"开始监听客户端传来的信息!" + "\r\n\n");
        }

        void OnlineList_Disp(string Info)
        {
            if(!listBoxOnlineList.Items.Contains(Info))
            listBoxOnlineList.Items.Add(Info);   //在线列表中显示连接的客户端套接字
        }

        private void watchconnecting()
        {
            Socket connection = null;
            while (true)  //持续不断监听客户端发来的请求   
            {
                try
                {
                    connection = socketwatch.Accept();
                    
                }
                catch (Exception ex)
                {
                    txtLog.AppendText(ex.Message); //提示套接字监听异常   
                    break;
                }
                //获取客户端的IP和端口号
                IPAddress clientIP = (connection.RemoteEndPoint as IPEndPoint).Address;
                int clientPort = (connection.RemoteEndPoint as IPEndPoint).Port;

                //让客户显示"连接成功的"的信息
                string sendmsg = "连接服务端成功！\r\n" + "本地IP:" + clientIP + "，本地端口" + clientPort.ToString();
                byte[] arrSendMsg = Encoding.UTF8.GetBytes(sendmsg);
                connection.Send(arrSendMsg);

                
                RemoteEndPoint = connection.RemoteEndPoint.ToString(); //客户端网络结点号
                txtLog.AppendText(GetCurrentTime() + "\r\n" + "成功与" + RemoteEndPoint + "客户端建立连接！" + "\r\n\n");     //显示与客户端连接情况
                

                //获取昵称
                byte[] RecMsg = new byte[1024 * 1024];
                int length = connection.Receive(RecMsg); 
                string strRecMsg = Encoding.UTF8.GetString(RecMsg, 0, length);
                
                //检查昵称重名
                if (dicName.Count>0)              
                {                 
                    string ack = "昵称已存在，请重新输入昵称";
                    byte[] ackMsg = Encoding.UTF8.GetBytes(ack);
                    if (dicName.ContainsKey(strRecMsg))
                    {
                        Flag = true;
                        connection.Send(ackMsg);
                        //离线处理                                      
                        connection.Close();
                        txtLog.AppendText(GetCurrentTime() + "\r\n" + "客户端" + RemoteEndPoint + "已经中断连接" + "\r\n\n");
                        
                    }
                    else
                    {
                        Flag = false;
                        dicName.Add(strRecMsg, RemoteEndPoint); //绑定昵称与网络节点号
                        dic.Add(RemoteEndPoint, connection);    //添加客户端信息
                    }                                                      
                }
                else
                {
                    Flag = false;
                    dicName.Add(strRecMsg, RemoteEndPoint); //绑定昵称与网络节点号
                    dic.Add(RemoteEndPoint, connection);    //添加客户端信息
                }

               
                OnlineList_Disp(strRecMsg);    //显示在线客户端
               
                //上线提醒
                string msg = strRecMsg +"，"+"上线了";
                byte[] Msg = Encoding.UTF8.GetBytes(msg);
                foreach (string Client in dicName.Values)
                {
                    if (Client != RemoteEndPoint)
                        dic[Client].Send(Msg);   //发送数据
                }


                //创建一个通信线程    
                ParameterizedThreadStart pts = new ParameterizedThreadStart(recv);
                Thread thread = new Thread(pts);
                thread.IsBackground = true;//设置为后台线程，随着主线程退出而退出   
                //启动线程   
                thread.Start(connection);
            }
        }

        ///   
        /// 接收客户端发来的信息    
        ///   
        ///客户端套接字对象  
        private void recv(object socketclientpara)
        {

            Socket socketServer = socketclientpara as Socket;
            while (true)
            {

                //创建一个内存缓冲区 其大小为1024*1024字节  即1M   
                byte[] arrServerRecMsg = new byte[1024 * 1024];
                //将接收到的信息存入到内存缓冲区,并返回其字节数组的长度  

                try
                {
                    int length = socketServer.Receive(arrServerRecMsg);

                    //将机器接受到的字节数组转换为人可以读懂的字符串   
                    string strSRecMsg = Encoding.UTF8.GetString(arrServerRecMsg, 0, length);

                    //转发消息给其他客户端
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(strSRecMsg);

                    foreach (string Client in dicName.Values)
                    {
                        if (Client != socketServer.RemoteEndPoint.ToString())
                            dic[Client].Send(bytes);   //发送数据
                    }


                    //将发送的字符串信息附加到文本框txtMsg上   
                    txtLog.AppendText(GetCurrentTime() + "\r\n" + strSRecMsg + "\r\n\n");
                }
                catch (Exception ex)
                {
                    //下线处理
                    if (!Flag)
                    {
                        var name = dicName.Where(q => q.Value == socketServer.RemoteEndPoint.ToString()).Select(q => q.Key);
                        txtLog.AppendText(GetCurrentTime()+"\r\n"+name.FirstOrDefault() + "已经中断连接" + "\r\n\n"); //提示套接字监听异常


                        //下线提醒
                        string leavemsg = name.FirstOrDefault() + "下线了";
                        byte[] LeaveMsg = Encoding.UTF8.GetBytes(leavemsg);
                        foreach (string Client in dicName.Values)
                        {
                            if (Client != socketServer.RemoteEndPoint.ToString())
                                dic[Client].Send(LeaveMsg);   //发送数据
                        }
                        //离线处理
                        listBoxOnlineList.Items.Remove(name.FirstOrDefault());//从listbox中移除断开连接的客户端
                        dicName.Remove(name.FirstOrDefault());
                        dic.Remove(socketServer.RemoteEndPoint.ToString());
                        socketServer.Close();//关闭之前accept出来的和客户端进行通信的套接字
                        break;
                    }
                    else {                       
                        break;
                    }
                    
                }
            }
        }

            
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            base.Visible = true;
            this.notifyIcon1.Visible = false;
            this.ShowInTaskbar = true;        
            base.WindowState = FormWindowState.Normal;
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

        private void SendMessage_Click(object sender, EventArgs e)
        {
            string sendMsg = "管理员:"+richTextBoxSend.Text.Trim();  //要发送的信息
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(sendMsg);   //将要发送的信息转化为字节数组，因为Socket发送数据时是以字节的形式发送的

            if (listBoxOnlineList.SelectedIndex == -1)
            {
                MessageBox.Show("请选择要发送的客户端！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                string selectClient = listBoxOnlineList.Text;  //选择要发送的客户端
                var point = dicName.Where(q => q.Key == selectClient).Select(q => q.Value);
                dic[point.FirstOrDefault()].Send(bytes);   //发送数据
                richTextBoxSend.Clear();
                txtLog.AppendText( GetCurrentTime() + "\r\n" + sendMsg + "\r\n\n");

            }
        }

        private void richTextBoxSend_KeyDown(object sender, KeyEventArgs e)
        {
            //如果用户按下了Enter键   
            if (e.KeyCode == Keys.Enter)
            {
                
                string sendMsg = "管理员:"+richTextBoxSend.Text.Trim();  //要发送的信息
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(sendMsg);
                if (listBoxOnlineList.SelectedIndex == -1)
                {
                    MessageBox.Show("请选择要发送的客户端！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                else
                {

                    string selectClient = listBoxOnlineList.Text;  //选择要发送的客户端
                    var point = dicName.Where(q => q.Key == selectClient).Select(q => q.Value);
                    dic[point.FirstOrDefault()].Send(bytes);   //发送数据
                    richTextBoxSend.Clear();
                    txtLog.AppendText( GetCurrentTime() + "\r\n" + sendMsg + "\r\n\n");

                }
            }
        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret(); 
        }



    }
}
