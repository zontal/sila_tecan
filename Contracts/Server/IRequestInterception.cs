using System;

namespace Tecan.Sila2.Server
{
    /// <summary>
    /// Interface to denote a command interception
    /// </summary>
    public interface IRequestInterception
    {
        /// <summary>
        /// Reports that the command execution completed without errors
        /// </summary>
        void ReportSuccess();

        /// <summary>
        /// Reports that the command execution completed with an exception
        /// </summary>
        /// <param name="ex"></param>
        void ReportException(Exception ex);
    }
}
