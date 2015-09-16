using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Serialization;

namespace DreamTeamTask2
{
    class Program
    {
        static void Main(string[] args)
        {
            // Определим нужное максимальное количество потоков
            // Пусть будет по 4 на каждый процессор
            int MaxThreadsCount = Environment.ProcessorCount * 4;
            // Установим максимальное количество рабочих потоков
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            // Установим минимальное количество рабочих потоков
            ThreadPool.SetMinThreads(2, 2);
            // Создадим новый сервер на порту 80
            Server srv = new Server(80, 20);
            Console.ReadKey();
        }
    }

    
    
    
    class Server
    {
        private Object thislock = new Object();
        Socket srvSocket;
        //TcpListener Listener; // Объект, принимающий TCP-клиентов
        int value; //Изменяемое TCP клиентами значение. В виде свойства не подходит,т.к. в Interlocked нужен ref int передавать.
        int N; //Ограничение макс значения value

        //Можно сделать и public ActionResult<ActionTypeStruct IncValue() и др. функции. Но пока так
        public bool IncValue()
        {
            //Interlocked.Increment(ref value);
            if (value <= N)
            {
                value++;
                return true;
            }
            else return false;
        }
        public bool DecValue()
        {
            //Interlocked.Decrement(ref value);
            if (value >= 1)
            {
                value--;
                return true;
            }
            else return false;
        }
        public bool ZeroValue()
        {
            //Interlocked.Exchange(ref value, 0);
            value = 0;
            return true;
        }

        /// <summary>
        /// Создаёт сервер, принимающий все входящие IP адреса
        /// </summary>
        /// <param name="Port">Порт, который слушает сервер</param>
        /// <param name="maxvalue">Максимальное значение изменяемой переменной, хранящейся на сервере</param>
        public Server(int Port, int maxvalue)
        {
            N = maxvalue;
            

            // Устанавливаем для сокета локальную конечную точку
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, Port);

            // Создаем сокет Tcp/Ip
            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.AcceptConnection, true);

            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(10);
            }
            //Обработать различные исключения
            catch (Exception e)
            {

            }
        }

        public void AcceptClients()
        {
            // В бесконечном цикле
            while (true)
            {
                // Принимаем новых клиентов. После того, как клиент был принят, он передается в новый поток (ClientThread)
                // с использованием пула потоков.
                ThreadPool.QueueUserWorkItem(
                    clientSocket =>
                    {
                        try
                        {
                            ConnectClient((Socket)clientSocket); //Listener.AcceptTcpClient());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    },
                    srvSocket.Accept()
                    );
                // Listener.AcceptTcpClient());
            }
        }

        private void ConnectClient(Socket clientSocket)
        {
            // Просто создаем новый экземпляр класса Client и передаем ему приведенный к классу TcpClient объект StateInfo
            //new Client((TcpClient)StateInfo, this);
            byte[] bytes = new byte[1024];
            clientSocket.Receive(bytes);
            ActionTypeStruct command;
            var deserializer = new XmlSerializer(typeof(ActionTypeStruct));
            using (Stream netstream = new NetworkStream(clientSocket))
            {
                command = (ActionTypeStruct)deserializer.Deserialize(netstream);//.Serialize(netstream, actionID);
            }
            bool commandsucceed;
            lock (thislock)
            {
                switch (command.Type)
                {
                    case ActionType.Increment:
                        commandsucceed = IncValue();
                        break;
                    case ActionType.Decrement:
                        commandsucceed = DecValue();
                        break;
                    case ActionType.Flush:
                        commandsucceed = ZeroValue();
                        break;
                    //Это не нужно,т.к. поступают уже проверенные команды
                    default:
                        throw new Exception("Incorrect incoming request");
                        commandsucceed = false;
                        break;
                }
            }
            Console.WriteLine(command.Type.ToString() + " is made");
            Console.WriteLine("N = {0}", N);
            
            //Отправляем результат клиенту
            ActionResult<ActionTypeStruct> result = new ActionResult<ActionTypeStruct>(command, commandsucceed);
            var serializer = new XmlSerializer(typeof(ActionResult<ActionTypeStruct>));
            using (Stream netstream = new NetworkStream(clientSocket))
            {
                serializer.Serialize(netstream, result);
            }

            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
    

    


    // Класс-обработчик клиента
    class Client
    {
        public void ProcessingActionFired(Object sender, ActionTypeStruct e)
        {
            Console.WriteLine("Action" + e.Type.ToString() + " is processing");
        }
        public void ProcessedAdctionFired(Object sender, ActionResult<ActionTypeStruct> e)
        {
            Console.WriteLine("Action" + e.Identificator.Type.ToString() + " is processed");
        }

        public Client()
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //Code to be inserted in Main(). When server and Client apps will be separated

            Console.WriteLine(
                "Input your query for server.\nIt can consist of commands separated by ',' ' '.' , new line or tab:\n");
            Console.WriteLine("1)Increment");
            Console.WriteLine("2)Decrement");
            Console.WriteLine("3)Flush");
            string Request = Console.ReadLine();
            ActionProcessor ap = new ActionProcessor();
            try
            {
                ap.PerformRequest(Request);
            }
            catch (TooManyActionsException e)
            {
                Console.WriteLine(e.Message);
            }

            //End of Main() code


        }
    }
}