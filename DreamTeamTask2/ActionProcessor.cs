using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Xml.Serialization;

namespace DreamTeamTask2
{
    public interface IActionProcessor<T>
            where T : IEquatable<ActionTypeStruct>
    {
        int MaxActionsCount { get; }

        void RequestAction(T actionId);

        event EventHandler<T> ProcessingAction;

        event EventHandler<ActionResult<T>> ProcessedAction;
    }
    public class ActionProcessor : IActionProcessor<ActionTypeStruct>
    {
#region private Fields
        private int maxactionscount = 5;
        public int MaxActionsCount { get { return maxactionscount; } }
#endregion

#region Methods
        /// <summary>
        /// Parse string containing commands and try to perform it
        /// </summary>
        /// <param name="Request"></param>
        /// <exception cref="TooManyActionsException">If there are too many commands</exception>
        public void PerformRequest(string Request)
        {
            

            //del string Request = Console.ReadLine();
            //Убираем все пробелы
            string[] Actions = Request.Split(new char[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            //Если количество запрошенных команд больше максимальо определённого то завершаем программу (по заданию)
            if (Actions.Length > MaxActionsCount)
                throw new TooManyActionsException("Too many actions requested");

            // del ActionProcessor<ActionTypeStruct> ap = new ActionProcessor<ActionTypeStruct>();

            foreach(string actionString in Actions)
            {
                ActionType action;
                if (Enum.TryParse(actionString, out action))
                {
                    if (Enum.IsDefined(typeof(ActionType), action))
                        RequestAction(new ActionTypeStruct(action));
                }
                else
                    throw new Exception("Wrong command was asked: " + action.ToString());
            }


        }
        //Как то надо передать указатель на сервер для вызова его функций. Статическим полем в Client не хочется его делать
        public void RequestAction(ActionTypeStruct actionID)
        {
            OnProcessingAction(actionID);

            //Создаём сокет
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            //80 - порт сервера
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 80);
            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            // Соединяем сокет с удаленной точкой
            sender.Connect(ipEndPoint);

            //Отправляем команду на серверу
            XmlSerializer serializer = new XmlSerializer(typeof(ActionTypeStruct));
            using (Stream netstream = new NetworkStream(sender))
            {
                serializer.Serialize(netstream, actionID);
            }

            //Получаем результат от сервера
            byte[] buffer = new byte[1024];
            sender.Receive(buffer);
            XmlSerializer deserializer = new XmlSerializer(typeof(ActionResult<ActionTypeStruct>));
            ActionResult<ActionTypeStruct> result;
            using (MemoryStream readstream = new MemoryStream(buffer))
            {
                result = (ActionResult<ActionTypeStruct>)deserializer.Deserialize(readstream);
            }

            Console.WriteLine("Command " + result.Identificator.ToString() + " is "
                + (result.Result ? "done" : "failed"));


            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
            OnProcessedAction(result);
        }
        protected virtual void OnProcessingAction(ActionTypeStruct e)
        {
            EventHandler<ActionTypeStruct> handler = ProcessingAction;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void OnProcessedAction(ActionResult<ActionTypeStruct> e)
        {
            EventHandler<ActionResult<ActionTypeStruct>> handler = ProcessedAction;
            if (handler != null)
            {

                handler(this, e);
            }
        }
#endregion

        public event EventHandler<ActionTypeStruct> ProcessingAction;

        public event EventHandler<ActionResult<ActionTypeStruct>> ProcessedAction;
    }

    public class Proxy : IActionProcessor<ActionTypeStruct>
    {
        private IActionProcessor<ActionTypeStruct> realActionProcessor; 
        public int MaxActionsCount { get; private set; }
        public void RequestAction(ActionTypeStruct actionId)
        {
            throw new NotImplementedException();
        }

        public Proxy(IActionProcessor<ActionTypeStruct> realActionProcessor)
        {
            this.realActionProcessor = realActionProcessor;
        }

        public bool GoodRequestAction()
        {
            
        }

        public event EventHandler<ActionTypeStruct> ProcessingAction;
        public event EventHandler<ActionResult<ActionTypeStruct>> ProcessedAction;
    }
}
