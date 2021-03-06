﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Text;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace INCAL_Server
{
    class ServerClass
    {
        public static Socket Server, Server2, Client, Client2;
        public static byte[] getByte = new byte[1024];
        public static byte[] setByte = new byte[1024];
        static string strConn = "Data Source=" + System.IO.Directory.GetCurrentDirectory() + "\\sqlite.db";
        static SQLiteConnection conn = null;
        public const int sPort = 1501;
        public const int sport2 = 1502;
        public static Command console_command;
        [STAThread]
        static void Main(string[] args)
        {
            IPAddress serverIP = IPAddress.Parse("110.10.38.94");
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, sPort);
            Console.WriteLine("INCAL Server v.1.0.0");
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Server2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Server.Bind(serverEndPoint);
            serverEndPoint.Port = sport2;
            Server2.Bind(serverEndPoint);
            Thread server_teacher = new Thread(new ThreadStart(Server_teacher));
            server_teacher.Start();
            Thread server_app = new Thread(new ThreadStart(Server_app));
            server_app.Start();
            while (true)
            {
                try
                {
                    console_command = new Command(new SQLiteConnection(strConn));
                    console_command.CommandLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        public static void Server_teacher()
        {

            while (true)
            {
                try
                {
                    using (conn = new SQLiteConnection(strConn))
                    {
                        conn.Open();
                        Server.Listen(10);

                        Client = Server.Accept();
                        console_command.Showip(Client);
                        if (Client.Connected)
                        {
                            Data data = new Data();
                            String _data = "";
                            Recieve(Client, ref _data);
                            data.Subject = _data;
                            Console.WriteLine("Client Message : {0}", _data);

                            Recieve(Client, ref _data);
                            data.T_Name = _data;
                            Console.WriteLine("Client Message : {0}", _data);

                            Recieve(Client, ref _data);
                            data.Contents = _data.Replace("\n", "<br>");
                            Console.WriteLine("Client Message : {0}", data.Contents);

                            Recieve(Client, ref _data);
                            data.Title = _data;
                            Console.WriteLine("Client Message : {0}", _data);

                            Recieve(Client, ref _data);
                            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
                            data.Date = DateTime.Parse(_data, originalCulture,
                                     DateTimeStyles.NoCurrentDateDefault); ;
                            Console.WriteLine("Client Message : {0}", _data);

                            string sql = string.Format("insert into INCAL_DATA (Subject,T_name,Contents,Title,Date) values (\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\")", data.Subject, data.T_Name, data.Contents, data.Title, data.Date.ToShortDateString());
                            SQLiteCommand command = new SQLiteCommand(sql, conn);
                            command.ExecuteNonQuery();
                            sendnoti();
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException socketEx)
                {
                    Console.WriteLine("[Error]:{0}", socketEx.Message);
                }
                catch (System.Exception commonEx)
                {
                    Console.WriteLine("[Error]:{0}", commonEx.Message);
                }
            }

        }
        public static void Server_app()
        {
            while (true)
            {
                try
                {
                    using (conn = new SQLiteConnection(strConn))
                    {
                        conn.Open();
                        Server2.Listen(10);

                        Client2 = Server2.Accept();
                        console_command.Showip(Client2);
                        if (Client2.Connected)
                        {
                            var cmd = new SQLiteCommand("select * from INCAL_DATA", conn);
                            var rdr = cmd.ExecuteReader();
                            while (rdr.Read())
                            {
                                Send(Client2, (string)rdr["Subject"]);

                                //Send(Client2, (string)rdr[1]);

                                Send(Client2, (string)rdr["T_Name"]);

                                Send(Client2, (string)rdr["Contents"]);

                                //Send(Client2, (string)rdr[4]);

                                Send(Client2, (string)rdr["Title"]);

                                Send(Client2, (string)rdr["Date"]);
                            }
                            Send(Client2, "EOF");
                            rdr.Close();
                            Client2.Close();
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException socketEx)
                {
                    Console.WriteLine("[Error]:{0}", socketEx.Message);
                }
                catch (System.Exception commonEx)
                {
                    Console.WriteLine("[Error]:{0}", commonEx.Message);
                }
            }
        }
        public static bool Send(Socket sock, String msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            int size = data.Length;

            byte[] data_size = new byte[4];
            data_size = BitConverter.GetBytes(size);
            sock.Send(data_size);

            sock.Send(data, 0, size, SocketFlags.None);
            return true;
        }
        public static bool Recieve(Socket sock, ref String msg)
        {
            byte[] data_size = new byte[4];
            sock.Receive(data_size, 0, 4, SocketFlags.None);
            int size = BitConverter.ToInt32(data_size, 0);
            byte[] data = new byte[size];
            sock.Receive(data, 0, size, SocketFlags.None);
            msg = Encoding.Default.GetString(data);
            return true;
        }

        public static int byteArrayDefrag(byte[] sData)
        {
            int endLength = 0;

            for (int i = 0; i < sData.Length; i++)
            {
                if ((byte)sData[i] != (byte)0)
                {
                    endLength = i;
                }
            }

            return endLength;
        }
        static void sendnoti()
        {
            try
            {
                AndroidGCMPushNotification noti = new AndroidGCMPushNotification();
                noti.SendNotification();
            }
            catch (Exception e)
            {
                Console.WriteLine("오류가 발생했습니다.\n" + e.Message);
            }
            Console.WriteLine("알림을 정상적으로 발신했습니다.");
        }
    }
    public class Command
    {
        private SQLiteConnection sqlconn;
        public Command(SQLiteConnection sqlconn1)
        {
            sqlconn = sqlconn1;
            sqlconn.Open();
        }
        public void Showip(Socket Client)
        {
            Console.WriteLine("{0} \"{1}\" IP connected",DateTime.Now.ToLocalTime(), Client.RemoteEndPoint.ToString());
            string sql = string.Format("insert into Log (IPAdress,Date) values (\"{0}\",\"{0}\")", DateTime.Now.ToLocalTime() + DateTime.Now.ToLongDateString(), Client.RemoteEndPoint.ToString());
            SQLiteCommand command = new SQLiteCommand(sql, sqlconn);
            command.ExecuteNonQuery();

        }
        public void CommandLine()
        {
            String cmd = Console.ReadLine();
            if (string.Compare("ShowData", cmd) == 0)
            {
                ShowData();
            }
            else if (string.Compare("del", cmd) == 0)
            {
                cmd = Console.ReadLine();
                del(cmd);
            }
            else if (string.Compare("ipban", cmd) == 0)
            {
                //구현되지 않은 부분
            }
        }
        private void ShowData()
        {
            using (sqlconn)
            {
                var cmd = new SQLiteCommand("select * from INCAL_DATA", sqlconn);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    Console.Write((string)rdr["Subject"] + '|');

                    //Send(Client2,(string)rdr[1]);

                    Console.Write((string)rdr["T_Name"] + '|');

                    Console.Write((string)rdr["Contents"] + '|');

                    //Send(Client2, (string)rdr[4]);

                    Console.Write((string)rdr["Title"] + '|');

                    Console.WriteLine((string)rdr["Date"]);
                }
                rdr.Close();
            }
        }
        private void del(string cmd)
        {
            using(sqlconn)
            {
                string sql = string.Format("delete from INCAL_DATA where Title = {0}",cmd);
                SQLiteCommand command = new SQLiteCommand(sql,sqlconn);
                command.ExecuteNonQuery();
                Console.WriteLine(command.CommandText);
            }
        }
    }
    public class Data
    {
        public string Subject { get; set; }
        public string T_Name { get; set; }
        public string Contents { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
    }
}

