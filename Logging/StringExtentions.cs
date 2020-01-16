using System;

namespace Tecan.Sila2.Logging
{

    /// <summary>
    /// Extention methods for strings to simplify application logging
    /// </summary>
    public static class StringExtentions
    {

        /// <summary>
        /// Create a Func<string>-Deleate from a string. Usefull for filling it into the ILoggingChannel.
        /// Example usage: ILoggingChannel.Log("Log this".ToDel());
        /// </summary>
        /// <param name="str">String to create Dlegate from</param>
        /// <returns></returns>
        public static Func<string> ToDel(this string str)
        {
            return () => str;
        }
    }

}
