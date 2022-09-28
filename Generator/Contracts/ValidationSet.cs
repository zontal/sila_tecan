using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Generator.Contracts
{
    /// <summary>
    /// Denotes a validation check
    /// </summary>
    public class ValidationSet
    {
        /// <summary>
        /// Creates a new validation set
        /// </summary>
        /// <param name="checkExpression">the code expression that represents the actual check</param>
        /// <param name="errorMessage">the error message</param>
        public ValidationSet( CodeExpression checkExpression, CodeExpression errorMessage )
        {
            CheckExpression = checkExpression;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets the code expression that represents the actual check
        /// </summary>
        public CodeExpression CheckExpression { get; }

        /// <summary>
        /// Gets the format for the error message
        /// </summary>
        public CodeExpression ErrorMessage { get; }
    }
}
