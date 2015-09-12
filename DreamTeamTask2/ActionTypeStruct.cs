using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamTeamTask2
{
    public enum ActionType
    {
        Increment,
        Decrement,
        Flush
    }
    [Serializable]
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
}
