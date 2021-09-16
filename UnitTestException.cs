using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    class UnitTestException : Exception
    {
        public UnitTestException(string message)
            : base(message)
        {

        }
    }
}
