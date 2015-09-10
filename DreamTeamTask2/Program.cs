using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

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
        }
    }
    
    
    class Server
    {
        TcpListener Listener; // Объект, принимающий TCP-клиентов
        int value; //Изменяемое TCP клиентами значение. В виде свойства не подходит,т.к. в Interlocked нужен ref int передавать.
        int N; //Ограничение макс значения value

        public string IncValue()
        {
            //Interlocked.Increment(ref value);
            if (value <= N)
            {
                value++;
                return "200";
            }
            else return "400";
        }
        public string DecValue()
        {
            //Interlocked.Decrement(ref value);
            if (value >= 1)
            {
                value--;
                return "200";
            }
            else return "400";
        }
        public string ZeroValue()
        {
            //Interlocked.Exchange(ref value, 0);
            return "200";
        }
        /// <summary>
        /// Создаёт сервер, принимающий все входящие IP адреса
        /// </summary>
        /// <param name="Port">Порт, который слушает сервер</param>
        /// <param name="maxvalue">Максимальное значение изменяемой переменной, хранящейся на сервере</param>
        public Server(int Port, int maxvalue)
        {
            N = maxvalue;
            /*IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11000);
             */
            //Не знаю подходит ли IPAddress в конструкторе TcpListener
            Listener = new TcpListener(IPAddress.Any, Port); // Создаем "слушателя" для указанного порта
            Listener.Start(); // Запускаем его

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
                            ClientThread(state);//Listener.AcceptTcpClient());
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }, 
                    Listener.AcceptTcpClient());
            }
        }
        private void ClientThread(Object StateInfo)
        {
            // Просто создаем новый экземпляр класса Client и передаем ему приведенный к классу TcpClient объект StateInfo
            new Client((TcpClient)StateInfo, this);
        }
        ~Server()
        {
            // Если "слушатель" был создан
            if (Listener != null)
            {
                // Остановим его
                Listener.Stop();
            }
        }
    }
    

    


    // Класс-обработчик клиента
    class Client
    {
        //private Object thisLock = new Object();
        // Отправка страницы с ошибкой
       /* private void SendError(TcpClient Client, int Code)
        {
            // Получаем строку вида "200 OK"
            // HttpStatusCode хранит в себе все статус-коды HTTP/1.1
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            // Код простой HTML-странички
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            // Необходимые заголовки: ответ сервера, тип и длина содержимого. После двух пустых строк - само содержимое
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            // Отправим его клиенту
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            // Закроем соединение
            Client.Close();
        }

        */

        public interface IActionProcessor<ActionTypeStruct>
            where ActionTypeStruct : IEquatable<ActionTypeStruct>
        {
            int MaxActionsCount { get; }

            void RequestAction(ActionTypeStruct actionId);

            event EventHandler<ActionTypeStruct> ProcessingAction;

            event EventHandler<ActionResult<ActionTypeStruct>> ProcessedAction;
        }
        public class ActionProcessor<TActionId> : IActionProcessor<TActionId>
            where TActionId : IEquatable<TActionId>
        {
            private Object thislock = new Object();
            public int MaxActionsCount { get; }
            //Как то надо передать указатель на сервер для вызова его функций. Статическим полем в Client не хочется его делать
            public void RequestAction(ActionTypeStruct actionID, Server srv)
            {
                string Request = Console.ReadLine();
                //Впринципе lock не нужен, т.к. я реализовал последовательный вызов RequestAction.
                lock (thislock)
                {
                    switch (actionID.Type)
                    {
                        case ActionType.Increment:
                            srv.IncValue();
                            break;
                        case ActionType.Decrement:
                            srv.DecValue();
                            break;
                        case ActionType.Flush:
                            srv.ZeroValue();
                            break;
                        //Это не нужно,т.к. поступают уже проверенные команды
                        default:
                            throw new Exception("Incorrect incoming request");
                            break;
                    }
                }
            }


            public event EventHandler<ActionTypeStruct> ProcessingAction;

            public event EventHandler<ActionResult<ActionTypeStruct>> ProcessedAction;
        }

        public struct ActionResult<ActionTypeStruct>
            where ActionTypeStruct : IEquatable<ActionTypeStruct>
        {
            private readonly ActionTypeStruct _identificator;
            private readonly bool _result;

            public ActionResult(ActionTypeStruct identificator, bool Result)
            {
                _identificator = identificator;
                _result = Result;
            }

            public ActionTypeStruct Identificator
            {
                get { return _identificator; }
            }

            public bool Result
            {
                get { return _result; }
            }
        }
        public enum ActionType
        {
            Increment,
            Decrement,
            Flush
        }
        public struct ActionTypeStruct : IEquatable<ActionTypeStruct>
        {
            private readonly ActionType _type;

            public ActionTypeStruct(ActionType type)
            {
                _type = type;
            }

            public ActionType Type
            {
                get { return _type; }
            }

            public bool Equals(ActionTypeStruct other)
            {
                return _type == other._type;
            }
        }
        Server srv;

        // Конструктор класса. Ему нужно передавать принятого клиента от TcpListener
        public Client(TcpClient client, Server srv)
        {
            this.srv = srv; 
            // Объявим строку, в которой будет храниться запрос клиента
            string Request = "";
            // Буфер для хранения принятых от клиента данных
            byte[] Buffer = new byte[1024];
            
            //Думать надо ли.
            //Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            
            // Переменная для хранения количества байт, принятых от клиента
            int Count;
            // Читаем из потока клиента до тех пор, пока от него поступают данные
            while ((Count = client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                // Преобразуем эти данные в строку и добавим ее к переменной Request
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                // Запрос должен обрываться последовательностью \r\n\r\n
                // Либо обрываем прием данных сами, если длина строки Request превышает 4 килобайта
                // Нам не нужно получать данные из POST-запроса (и т. п.), а обычный запрос
                // по идее не должен быть больше 4 килобайт
                if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096)
                {
                    break;
                }
            }
            
            ActionProcessor ap = new ActionProcessor(srv, 2);
            //Убираем все пробелы
            string[] Actions = Request.Split(new char[]{' ',',','\t','\n'}, StringSplitOptions.RemoveEmptyEntries);
            //Если количество запрошенных команд больше максимальо определённого то завершаем программу (по заданию)
            if (Actions.Length > ap.MaxActionsCount)
                throw new Exception("Too many actions requested");
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
                    throw new Exception("Incorrect query");
                else{
                    //Тут вызываем выполнение ActionID команды
                    ap.RequestAction(new ActionTypeStruct(ActionID), this.srv);
                }
            }
            //Можно ещё синхронизировать работу каждого клиента. Но это по желанию. 

            client.Close();
        }
    }
}


/*
            string.Compare(Request, )
            string Reply;
            lock (thisLock)
            {
                switch (Request)
                {
                    case "increment":
                        Reply = srv.IncValue();
                        break;
                    case "decrement":
                        Reply = srv.DecValue();
                        break;
                    case "zero":
                        Reply = srv.ZeroValue();
                        break;
                    default:
                        throw new Exception("Incorrect incoming request");
                        break;
                }
            }
            byte[] ReplyBuffer = Encoding.ASCII.GetBytes(Reply);
            Client.GetStream().Write(ReplyBuffer, 0, ReplyBuffer.Length);
            Client.Close();
        }
    }
}
*/