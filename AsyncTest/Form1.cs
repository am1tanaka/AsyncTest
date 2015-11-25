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
        // ループ
        bool isLoop = true;

        /**
         * サーバーを開始
         */
        public void StartServer() {
            IPAddress ipAddr = IPAddress.Any;
            // TcpListener
            listener = new TcpListener(ipAddr, TCP_PORT);

            // 開始
            listener.Start();
            textStatus.Text += "リスナー開始\r\n";
        }

        /**
         * クライアントからの接続を待機する
         */
        async void WaitConnect()
        {
            while (isLoop)
            {
                // 接続要求を受け入れる
                Task<TcpClient> taskClient = listener.AcceptTcpClientAsync();
                TcpClient client = await taskClient;
                textStatus.Text += "クライアント(" + ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address
                    + ":" + ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Port + ")と接続しました。\r\n";

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
                        textStatus.Text += "クライアントが切断しました。\r\n";
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
                textStatus.Text += resMsg + "\r\n";

                if (!disconnected)
                {
                    // クライアントにデータ送信
                    string sendMsg = resMsg.Length.ToString();
                    byte[] sendBytes = enc.GetBytes(sendMsg + '\n');
                    stream.Write(sendBytes, 0, sendBytes.Length);
                    textStatus.Text += sendMsg+"bytes\r\n";
                }

                // 閉じる
                stream.Close();
                client.Close();
                textStatus.Text += "クライアントとの接続を閉じました。\r\n";
            }
            // リスナを閉じる
            closeTcpListener();
        }

        /** リスナーを閉じる*/
        void closeTcpListener()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
            textStatus.Text += "Listenerを閉じました。\r\n\r\n";
        }

        public Form1()
        {
            InitializeComponent();
            // TCPサーバーを起動
            StartServer();
            // クライアントを待機
            WaitConnect();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isLoop = false;
        }
    }
}
