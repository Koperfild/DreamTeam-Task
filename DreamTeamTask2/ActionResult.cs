using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamTeamTask2
{
    [Serializable]
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
}
