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
using System.IO;

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
         * 保存先のフォルダ名を作成
         */
        string getSaveDir()
        {
            return ".\\";
        }

        /**
         * 受信データからファイル名を取り出す
         */
        string getFileName(MemoryStream mem)
        {
            // ファイル名のバイト数を取り出す
            int size = mem.ReadByte() & 0xff;
            byte[] bt = new byte[size];
            mem.Read(bt, 0, size);
            return Encoding.UTF8.GetString(bt);
        }

        /**
         * 受信データをからデータ本体を取り出す
         */
        byte[] getData(MemoryStream mem)
        {
            byte [] dt = new byte[mem.Length-mem.Position];
            mem.Read(dt, 0, dt.Length);
            return dt;
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

                // 受信データ容量を確認
                int fileSize = (stream.ReadByte() << 24) + (stream.ReadByte() << 16) + (stream.ReadByte() << 8) + stream.ReadByte();
                // サイズが異常な時はエラーにしておく
                if ((fileSize < 0) || (fileSize > 10000000))
                {
                    textStatus.Text += "ファイルサイズが異常なのでキャンセルします。\r\n";
                    stream.Close();
                    client.Close();
                    continue;
                }

                // クライアントからのデータを受け取る
                bool disconnected = false;
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                byte[] resBytes = new byte[256];
                int resSize = 0;
                do
                {
                    // データの一部を受信
                    resSize = stream.Read(resBytes, 0, resBytes.Length);
                    textStatus.Text += "recv:"+resSize + "bytes\r\n";
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
                while (stream.DataAvailable || (ms.Length<fileSize-4));

                // 受信データを保存
                ms.Position = 0;
                string fname = getSaveDir()+getFileName(ms);
                byte [] savedata = getData(ms);
                File.WriteAllBytes(fname, savedata);
                ms.Close();

                // 末尾の\nを削除
                textStatus.Text += fname+":"+savedata.Length + "bytes\r\n";

                if (!disconnected)
                {
                    // クライアントにデータ送信
                    string sendMsg = savedata.Length.ToString();
                    byte[] sendBytes = Encoding.UTF8.GetBytes(sendMsg + '\n');
                    stream.Write(sendBytes, 0, sendBytes.Length);
                    textStatus.Text += "send "+sendMsg+"bytes\r\n";
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
