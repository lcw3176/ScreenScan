using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenScan
{
    public partial class ScreenScan : Form
    {
        private Socket socket;
        private Socket targetSocket;

        private List<Socket> userlist = new List<Socket>();
        private bool isRun = false;

        /// <summary>
        /// 2020.11.11 수정
        /// 1. 비동기 -> 동기
        /// 2. 바이트 크기 설정 변경
        /// </summary>
        public ScreenScan()
        {
            InitializeComponent();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            socket.Bind(new IPEndPoint(IPAddress.Any, 3000));
            socket.Listen(1);
            Task.Run(() => StartAccept());
        }


        public void StartAccept()
        {
            // 리스트에 클라이언트 정보를 담음
            // listbox에 아이피 주소 표시
            while(true)
            {
                try
                {
                    Socket client = socket.Accept();
                    userlist.Add(client);

                    this.Invoke(new Action(delegate ()
                    {
                        listBox1.Items.Add(client.RemoteEndPoint);
                    }));
                }

                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    continue;
                }

            }
        }


        private void listbox_Click(object sender, MouseEventArgs e)
        {
            // 리스트 박스 ip 클릭시 해당 유저 스캔 시작
            int index = listBox1.SelectedIndex;

            if(index != -1)
            {
                isRun = true;
                targetSocket = userlist[index];

                byte[] start = Encoding.ASCII.GetBytes("go");
                targetSocket.Send(start);

                Task.Run(() => StartReceive());
   
            }

        }


        private void StartReceive()
        {
            while(isRun)
            {
                try
                {
                    byte[] dataSizeByte = new byte[4];
                    targetSocket.Receive(dataSizeByte, 4, SocketFlags.None);

                    int dataSizeInt = BitConverter.ToInt32(dataSizeByte, 0);

                    byte[] imageData = new byte[dataSizeInt];
                    int offset = 0;

                    while(offset < dataSizeInt)
                    {
                        offset += targetSocket.Receive(imageData, offset, dataSizeInt - offset, SocketFlags.None);
                    }

                    byteToImage(imageData);
                }

                catch
                {
                    continue;
                }
            }
        }

        public void byteToImage(byte[] imageByte)
        {
            try
            {
                using(MemoryStream ms = new MemoryStream(imageByte))
                {
                    Image image = Image.FromStream(ms);
                    pictureBox1.Image = image;
                }

            }

            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }


        }

        private void on_Closed(object sender, FormClosedEventArgs e)
        {
            socket.Close();
            socket.Dispose();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            // 전송 일시정지 기능
            
            if (targetSocket.Connected)
            {
                if (isRun)
                {
                    byte[] stopped = Encoding.ASCII.GetBytes("st");
                    targetSocket.Send(stopped);
                    stopButton.Text = "다시 시작";
                    isRun = false;
                }

                else
                {
                    byte[] start = Encoding.ASCII.GetBytes("go");
                    targetSocket.Send(start);
                    stopButton.Text = "일시 정지";
                    isRun = true;

                    Task.Run(() => StartReceive());
                }

            }
            else
            {
                MessageBox.Show("이미 끊긴 연결입니다.", "알림");
            }

        }
    }
}
