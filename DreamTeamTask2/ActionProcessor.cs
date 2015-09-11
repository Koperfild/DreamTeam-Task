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
    public interface IActionProcessor<ActionTypeStruct>
            where ActionTypeStruct : IEquatable<ActionTypeStruct>
    {
        int MaxActionsCount { get; }

        void RequestAction(ActionTypeStruct actionId);

        event EventHandler<ActionTypeStruct> ProcessingAction;

        event EventHandler<ActionResult<ActionTypeStruct>> ProcessedAction;
    }
    public class ActionProcessor<ActionTypeStruct> : IActionProcessor<ActionTypeStruct>
        where ActionTypeStruct : IEquatable<ActionTypeStruct>
    {
        private int maxactionscount = 5;
        public int MaxActionsCount { get{return maxactionscount;} }
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

        public event EventHandler<ActionTypeStruct> ProcessingAction;

        public event EventHandler<ActionResult<ActionTypeStruct>> ProcessedAction;
    }
}
