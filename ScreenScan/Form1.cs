using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ScreenScan
{
    public partial class ScreenScan : Form
    {
        Socket socket;
        List<Socket> userlist = new List<Socket>();
        int index = -1;
        byte[] receive = new byte[400000];
        bool stop = false;

        public ScreenScan()
        {
            InitializeComponent();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            socket.Bind(new IPEndPoint(IPAddress.Any, 3000));
            socket.Listen(1);
            socket.BeginAccept(new AsyncCallback(AcceptConnection), null);
        }

        public void AcceptConnection(IAsyncResult ar)
        {
            // 리스트에 클라이언트 정보를 담음
            // listbox에 아이피 주소 표시

            Socket client = socket.EndAccept(ar);
            userlist.Add(client);
            this.Invoke(new Action(delegate ()
            {
                listBox1.Items.Add(client.RemoteEndPoint);
            }));
            socket.BeginAccept(AcceptConnection, null);
        }

        private void listbox_Click(object sender, MouseEventArgs e)
        {
            // 리스트 박스 ip 클릭시 해당 유저 스캔 시작

            Point point = e.Location;
            byte[] start = Encoding.Default.GetBytes("go");
            index = listBox1.IndexFromPoint(point);

            if (index != -1)
            {
                userlist[index].Send(start);
                userlist[index].BeginReceive(receive, 0, receive.Length, SocketFlags.None, new AsyncCallback(byteToImage), null);
            }

            else
            {
                MessageBox.Show("정확히 클릭해 주세요.", "알림");
            }
        }

        public void byteToImage(IAsyncResult ar)
        {
            try
            {
                userlist[index].EndReceive(ar);

                using(MemoryStream ms = new MemoryStream(receive))
                {
                    Image image = Image.FromStream(ms);
                    pictureBox1.Image = image;
                    Array.Clear(receive, 0, receive.Length);
                    userlist[index].BeginReceive(receive, 0, receive.Length, SocketFlags.None, byteToImage, null);
                }

            }

            catch
            {
                pictureBox1.Image = null;
                userlist[index].BeginReceive(receive, 0, receive.Length, SocketFlags.None, byteToImage, null);
            }


        }

        private void on_Closed(object sender, FormClosedEventArgs e)
        {  
            socket.Dispose();
            socket.Close();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            // 전송 일시정지 기능
            
            if (userlist[index].Connected)
            {
                if (stop == false)
                {
                    byte[] stopped = Encoding.Default.GetBytes("st");
                    userlist[index].Send(stopped);
                    stopButton.Text = "다시 시작";
                    stop = true;
                }

                else
                {
                    byte[] start = Encoding.Default.GetBytes("go");
                    userlist[index].Send(start);
                    stopButton.Text = "일시 정지";
                    stop = false;
                }

            }
            else
            {
                MessageBox.Show("이미 끊긴 연결입니다.", "알림");
            }

        }
    }
}
