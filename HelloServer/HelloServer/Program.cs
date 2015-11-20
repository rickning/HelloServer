using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace HelloServer
{
	class MainClass
	{
		class ConnectInfo
		{
			public ArrayList tmpList { get; set; }
			public SocketAsyncEventArgs SendArg { get; set; }
			public SocketAsyncEventArgs ReceiveArg { get; set; }
			public Socket ServerSocket{get;set;}
		}

		public static void Main (string[] args)
		{
			IPEndPoint serverPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
			Socket serverSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			serverSocket.Bind (serverPoint);
			serverSocket.Listen (10);

			Console.WriteLine ("waiting for connection");
			SocketAsyncEventArgs ev = new SocketAsyncEventArgs ();
			ev.Completed += new EventHandler<SocketAsyncEventArgs> (Accept_Completed);
			serverSocket.AcceptAsync (ev);

			Console.ReadLine ();

			Console.WriteLine ("Hello World!");
		}

	
		static void Accept_Completed(object sender, SocketAsyncEventArgs e)
		{
		     Socket client = e.AcceptSocket;
		     Socket server = sender as Socket;

		     if (sender == null) return;

		     SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
		     SocketAsyncEventArgs receciveArg = new SocketAsyncEventArgs();

		     ConnectInfo info = new ConnectInfo();
		     info.tmpList = new ArrayList();
		     info.SendArg = sendArg;
		     info.ReceiveArg = receciveArg;
		     info.ServerSocket=server;

		     byte[] sendbuffers=Encoding.ASCII.GetBytes("hello connector\n");
		     sendArg.SetBuffer(sendbuffers, 0, sendbuffers.Length);



		     sendbuffers=new byte[1024];
		     receciveArg.SetBuffer(sendbuffers, 0, 1024);
		     receciveArg.UserToken = info;
		     receciveArg.Completed += new EventHandler<SocketAsyncEventArgs>(Rececive_Completed);

		     client.SendAsync(sendArg);
		     client.ReceiveAsync(receciveArg);

		     e.Dispose();
		 }


		static void Rececive_Completed(object sender, SocketAsyncEventArgs e)
		{
			 ConnectInfo info = e.UserToken as ConnectInfo;
			 if (info == null) return;
			 Socket client = sender as Socket;
			 if (client == null) return;

			 if (e.SocketError== SocketError.Success)
			 {
			     int rec = e.BytesTransferred;
			     //收不到数据表明客户端终止了通信
			     //关闭相关的socket和释放资源
			     if (rec == 0)
			     {
			         client.Close();
			         client.Dispose();

			         info.ReceiveArg.Dispose();
			         info.SendArg.Dispose();

			         if (info.ServerSocket != null)
			         {
			             info.ServerSocket.Close();
			             info.ServerSocket.Dispose();
			         }
			         return;
			     }

			     byte[] datas=e.Buffer;

			     //如果数据还没接收完的就把已接收的数据暂存
			     //新开辟一个足够大的buffer来接收数据
			     if (client.Available > 0)
			     {
			         for (int i = 0; i < rec; i++)
			             info.tmpList.Add(datas[i]);
			         Array.Clear(datas, 0, datas.Length);

			         datas = new byte[client.Available];
			         e.SetBuffer(datas, 0, datas.Length);
			         client.ReceiveAsync(e);
			     }
			     else
			     {
			         //检查暂存数据的ArrayList中有没有数据，有就和本次的数据合并
			         if (info.tmpList.Count > 0)
			         {
			             for (int i = 0; i < rec; i++)
			                 info.tmpList.Add(datas[i]);
			             datas = info.tmpList.ToArray(typeof(byte)) as byte[];
			             rec = datas.Length;
			         }

			         //对接收的完整数据进行简单处理，回发给客户端
			         string msg = Encoding.ASCII.GetString(datas).Trim('\0');
			         if (msg.Length > 10) msg = msg.Substring(0, 10) + "...";
			         msg = string.Format("rec={0}\r\nmessage={1}", rec, msg);

			         info.SendArg.SetBuffer(Encoding.ASCII.GetBytes(msg),0,msg.Length);
			         client.SendAsync(info.SendArg);

			         //如果buffer过大的，把它换成一个小的
			         info.tmpList.Clear();
			         if (e.Buffer.Length > 1024)
			         {
			             datas = new byte[1024];
			             e.SetBuffer(datas, 0, datas.Length);
			         }

			         //再次进行异步接收
			         client.ReceiveAsync(e);
				} 
			}
		}
	
	}
}
