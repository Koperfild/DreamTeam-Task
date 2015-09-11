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
            new Server(80, 20);
            Console.ReadKey();
        }
    }

    
    
    
    class Server
    {
        private Object thislock = new Object();
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

            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(10);

                // В бесконечном цикле
                while (true)
                {
                    // Принимаем новых клиентов. После того, как клиент был принят, он передается в новый поток (ClientThread)
                    // с использованием пула потоков.
                    ThreadPool.QueueUserWorkItem(
                        state =>
                        {
                            try
                            {
                                ConnectClient(state);//Listener.AcceptTcpClient());
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        },
                        sListener.Accept()
                        );
                        // Listener.AcceptTcpClient());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("error:{0}",e.Message);
            }
        }
        private void ConnectClient(Object StateInfo)
        {
            // Просто создаем новый экземпляр класса Client и передаем ему приведенный к классу TcpClient объект StateInfo
            //new Client((TcpClient)StateInfo, this);
            Socket srvSocket = (Socket)StateInfo;
            byte[] bytes = new byte[1024];
            srvSocket.Receive(bytes);
            ActionTypeStruct command;
            XmlSerializer deserializer = new XmlSerializer(typeof(ActionTypeStruct));
            using (Stream netstream = new NetworkStream(srvSocket))
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
            XmlSerializer serializer = new XmlSerializer(typeof(ActionResult<ActionTypeStruct>));
            using (Stream netstream = new NetworkStream(srvSocket))
            {
                serializer.Serialize(netstream, result);
            }

            srvSocket.Shutdown(SocketShutdown.Both);
            srvSocket.Close();
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
            //this.srv = srv; 
            // Объявим строку, в которой будет храниться запрос клиента
            
            Console.WriteLine("Input your query for server.\nIt can consist of commands separated by ',' ' '.' , new line or tab:\n");
            Console.WriteLine("1)Increase");
            Console.WriteLine("2)Decrease");
            Console.WriteLine("3)Flush");
            
            string Request = Console.ReadLine();
            //Убираем все пробелы
            string[] Actions = Request.Split(new char[]{' ',',','\t','\n'}, StringSplitOptions.RemoveEmptyEntries);
            
            ActionProcessor<ActionTypeStruct> ap = new ActionProcessor<ActionTypeStruct>();
            //Если количество запрошенных команд больше максимальо определённого то завершаем программу (по заданию)
            if (Actions.Length > ap.MaxActionsCount)
                throw new TooManyActionsException("Too many actions requested");
            
            //Проверяем соответствие полученных команд командам обрабатываемым сервером. Обрабатываемые команды описаны в enum ActionType
            IEnumerable<ActionType> values = Enum.GetValues(typeof(ActionType)).Cast<ActionType>();
            //Для каждой команды определяем её в списке команд (ActionID)
            foreach(string action in Actions)
            {
                //Присваиваем дефолтное значение. Далее оно обязательно меняется или не используется (выкидывается исключение)
                ActionType ActionID = ActionType.Decrement;
                bool CurrActiontypeFound = false;
                //Проверяем есть ли полученный запрос в ActionType  
                foreach(ActionType val in values)
                {
                    //приводим val к виду string, чтобы затем сравнить
                    string valstring = val.ToString();
                    //Если команда из запроса совпадает с ActionType командой, то
                    if (string.Compare(action,valstring,true) == 0)
                    {
                        ActionID = val;
                        CurrActiontypeFound = true;
                        break;
                    }
                }
                //Если полученный запрос не соответстует никакой команде, то кидаем исключение
                if (!CurrActiontypeFound)
                    Environment.Exit(-1);
                    //throw new Exception("Incorrect query");
                else{
                    //Тут вызываем выполнение ActionID команды
                    ap.RequestAction(new ActionTypeStruct(ActionID));
                }
            }
        }
    }
}