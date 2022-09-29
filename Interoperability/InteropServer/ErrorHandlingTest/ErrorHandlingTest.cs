using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace Tecan.Sila2.Interop.Server.ErrorHandlingTest
{
    [Export( typeof( IErrorHandlingTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ErrorHandlingTest : IErrorHandlingTest
    {
        public long RaiseDefinedExecutionErrorOnGet => throw new TestErrorException( "SiLA2_test_error_message" );

        public long RaiseDefinedExecutionErrorOnSubscribe => throw new TestErrorException( "SiLA2_test_error_message" );

        public long RaiseUndefinedExecutionErrorOnGet => throw new ApplicationException( "SiLA2_test_error_message" );

        public long RaiseUndefinedExecutionErrorOnSubscribe => throw new ApplicationException( "SiLA2_test_error_message" );

        private bool _raiseDefinedExecutionError = false;
        public long RaiseDefinedExecutionErrorAfterValueWasSent
        {
            get
            {
                if(_raiseDefinedExecutionError)
                {
                    _raiseDefinedExecutionError = false;
                    throw new TestErrorException( "SiLA2_test_error_message" );
                }
                else
                {
                    _raiseDefinedExecutionError = true;
                    return 1;
                }
            }
        }

        private bool _raiseUndefinedExecutionError = false;
        public long RaiseUndefinedExecutionErrorAfterValueWasSent
        {
            get
            {
                if(_raiseUndefinedExecutionError)
                {
                    _raiseUndefinedExecutionError = false;
                    throw new ApplicationException( "SiLA2_test_error_message" );
                }
                else
                {
                    _raiseUndefinedExecutionError = true;
                    return 1;
                }
            }
        }

        public void RaiseDefinedExecutionError()
        {
            throw new TestErrorException( "SiLA2_test_error_message" );
        }

        public IObservableCommand RaiseDefinedExecutionErrorObservably()
        {
            return ObservableCommands.Create( () => Task.Run( RaiseDefinedExecutionError ) );
        }

        public void RaiseUndefinedExecutionError()
        {
            throw new ApplicationException( "SiLA2_test_error_message" );
        }

        public IObservableCommand RaiseUndefinedExecutionErrorObservably()
        {
            return ObservableCommands.Create( () => Task.Run( RaiseUndefinedExecutionError ) );
        }
    }
}
