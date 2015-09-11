using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamTeamTask2
{
    public class TooManyActionsException : Exception
    {
        public TooManyActionsException()
            : base()
        { }
        public TooManyActionsException(string msg)
            : base(msg)
        { }
    }
}
