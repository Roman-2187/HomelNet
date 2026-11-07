using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNetOrm.Exeptions
{
    public class DatabaseException : Exception
    {
        public DatabaseException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
