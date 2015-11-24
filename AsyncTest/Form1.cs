using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace AsyncTest
{
    public partial class Form1 : Form
    {
        // ListenするIPポート
        const int TCP_PORT = 60100;
        // TCPサーバー
        TcpListener listener;

        public void StartServer() {
            IPAddress ipAddr = IPAddress.Any;
            // TcpListener
            listener = new TcpListener(ipAddr, TCP_PORT);

            // 開始
            Console.WriteLine("接続開始");
            listener.Start();
            Console.WriteLine("Start");

            // 接続要求を受け入れる
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("クライアント({0}:{1})と接続しました。",
                        ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address,
                        ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port);

            // NetworkStreamを取得
            NetworkStream stream = client.GetStream();

            // タイムアウトを10秒に設定
            stream.ReadTimeout = 10000;
            stream.WriteTimeout = 10000;

            // クライアントからのデータを受け取る
            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            bool disconnected = false;
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] resBytes = new byte[256];
            int resSize = 0;
            do
            {
                // データの一部を受信
                resSize = stream.Read(resBytes, 0, resBytes.Length);
                // Readが0の時は切断
                if (resSize == 0)
                {
                    disconnected = true;
                    Console.WriteLine("クライアントが切断しました。"); 
                    break;
                }
                // 受信したデータを蓄積
                ms.Write(resBytes, 0, resSize);
            }
            while (stream.DataAvailable || resBytes[resSize - 1] != '\n');

            // 受信データを文字列に変換
            string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Close();

            // 末尾の\nを削除
            resMsg = resMsg.TrimEnd('\n');
            Console.WriteLine(resMsg);

            if (!disconnected)
            {
                // クライアントにデータ送信
                string sendMsg = resMsg.Length.ToString();
                byte[] sendBytes = enc.GetBytes(sendMsg + '\n');
                stream.Write(sendBytes, 0, sendBytes.Length);
                Console.WriteLine(sendMsg);
            }

            // 閉じる
            stream.Close();
            client.Close();
            Console.WriteLine("クライアントとの接続を閉じました。");

            // リスナを閉じる
            listener.Stop();
            Console.WriteLine("Listenerを閉じました。");

            Console.ReadLine();
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            StartServer();
        }
    }
}
