using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.ServerPooling
{
    internal class TemporaryException : Exception
    {
        public SiLAErrorDto Error
        {
            get;
        }

        public TemporaryException(SiLAErrorDto error)
        {
            Error = error;
        }
    }
}
