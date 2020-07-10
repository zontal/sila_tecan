using System;
using System.Collections.Generic;
using System.Text;

namespace Tecan.Sila2.Authentication
{
    /// <summary>
    /// Denotes user credentials
    /// </summary>
    public class Credentials
    {
        /// <summary>
        /// Gets the user name
        /// </summary>
        public string Username
        {
            get;
        }

        /// <summary>
        /// Gets the password
        /// </summary>
        public string Password
        {
            get;
        }

        /// <summary>
        /// Creates new credentials
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="password">The password</param>
        public Credentials( string username, string password )
        {
            Username = username;
            Password = password;
        }
    }
}
