using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ScreenClient
{
    public partial class Form1 : Form
    {
        Socket socket;
        byte[] Byte;
        byte[] start = new byte[10];
        
        // 시작, 정지를 알리는 flag
        bool flag = false;


        public Form1()
        {
            InitializeComponent();
        }

        private void wait_Connection()
        { 

            while (true)
            {
                try
                {
                    // 연결 대기
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, 3000));
                    socket.BeginReceive(start, 0, start.Length, SocketFlags.None, new AsyncCallback(receiveMsg), null);
                    break;
                }

                catch (SocketException)
                {
                    // 연결 대기
                    continue;
                }
            }
        }

        private void receiveMsg(IAsyncResult ar)
        {
            try
            {
                socket.EndReceive(ar);

                if (Encoding.Default.GetString(start).Substring(0, 2) == "go")
                {
                    // 시작
                    Array.Clear(start, 0, start.Length);
                    socket.BeginReceive(start, 0, start.Length, SocketFlags.None, new AsyncCallback(receiveMsg), null);
                    Byte = screenShot();
                    socket.BeginSend(Byte, 0, Byte.Length, SocketFlags.None, new AsyncCallback(screenCallback), null);
                    flag = true;
                }

                else
                {
                    // 중지
                    Array.Clear(start, 0, start.Length);
                    socket.BeginReceive(start, 0, start.Length, SocketFlags.None, new AsyncCallback(receiveMsg), null);
                    flag = false;
                }

            }

            catch(SocketException)
            {
                socket.Dispose();
                socket.Close();
                this.Invoke(new Action(delegate ()
                {
                    Close();
                }));

            } 
        }

        public void screenCallback(IAsyncResult ar)
        {
            // 캡쳐된 화면 전송
            // flag에 따라 캡쳐, 전송이 작동

            if(flag == true)
            {
                try
                {
                    socket.EndSend(ar);
                    Byte = screenShot();
                    socket.BeginSend(Byte, 0, Byte.Length, SocketFlags.None, new AsyncCallback(screenCallback), null);
                }

                catch
                {
                    socket.Dispose();
                    socket.Close();
                    this.Invoke(new Action(delegate ()
                    {
                        Close();
                    }));
                }
            }

            else
            {
                return;
            }
        }

        public byte[] screenShot()
        {
            // 화면 캡쳐 함수

            int width = Screen.PrimaryScreen.Bounds.Width;
            int height = Screen.PrimaryScreen.Bounds.Height;
            byte[] Byte;
            Point source = new Point(0, 0);

            // 디스플레이 배율에 따른 사이즈 조절
            // 대부분 노트북 패널의 기준인 FHD 125% 확대 기준
            if (width == 1536)
            {
                width = 1920;
                height = 1080;
            }

            using (Bitmap bitmap = new Bitmap(width, height))
            using(MemoryStream ms = new MemoryStream())
            {
                Graphics g = Graphics.FromImage(bitmap);
                try
                {
                    g.CopyFromScreen(source, new Point(0, 0), new Size(width, height));
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    Byte = ms.ToArray();
                    return Byte;
                }

                catch (Win32Exception)
                {
                    // 작업관리자 띄울 시 예외처리
                    Bitmap exception = new Bitmap(@".\exception.png");
                    exception.Save(ms, ImageFormat.Jpeg);
                    Byte = ms.ToArray();
                    return Byte;
                }

            }

           
        }

        private void client_Load(object sender, EventArgs e)
        {
            // 폼 로드, 연결 시작
            Visible = false;
            ShowInTaskbar = false;
            wait_Connection();
        }
    }
}
