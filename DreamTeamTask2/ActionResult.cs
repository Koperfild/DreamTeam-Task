using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamTeamTask2
{
    [Serializable]
    public struct ActionResult<T>
        where T : IEquatable<ActionTypeStruct>
    {
        private readonly T _identificator;
        private readonly bool _result;

        public ActionResult(T identificator, bool Result)
        {
            _identificator = identificator;
            _result = Result;
        }

        public T Identificator
        {
            get { return _identificator; }
        }

        public bool Result
        {
            get { return _result; }
        }
    }
}
