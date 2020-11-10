using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenClient
{
    public partial class Form1 : Form
    {
        private Socket socket;
        private bool isRun = false;

        public Form1()
        {
            InitializeComponent();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 3000));

            Task.Run(() => WaitCommand());
        }


        private void WaitCommand()
        { 

            while (true)
            {
                try
                {
                    byte[] command = new byte[2];
                    socket.Receive(command);

                    if(Encoding.ASCII.GetString(command) == "st")
                    {
                        isRun = false;
                    }

                    if(Encoding.ASCII.GetString(command) == "go")
                    {
                        isRun = true;
                        Task.Run(() => SendToServer());
                    }

                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    break;
                }
            }
        }

        private void SendToServer()
        {
            while(isRun)
            {
                try
                {
                    byte[] imageByte = screenShot();

                    if(imageByte.Length != 1)
                    {
                        byte[] imageSize = BitConverter.GetBytes(imageByte.Length);
                        socket.Send(imageSize, 0, 4, SocketFlags.None);

                        int offset = 0;

                        while(offset < imageByte.Length)
                        {
                            offset += socket.Send(imageByte, offset, imageByte.Length - offset, SocketFlags.None);
                        }
                        
                    }
                }

                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    continue;
                }
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
                    Byte = new byte[1];

                    return Byte;
                }

                finally
                {
                    g.Dispose();
                }

            }

           
        }


    }
}
