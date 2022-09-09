using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Tecan.Sila2.Interop.Server.ObservablePropertyTest
{
    [Export( typeof( IObservablePropertyTest ) )]
    [PartCreationPolicy( CreationPolicy.Shared )]
    internal class ObservablePropertyTest : IObservablePropertyTest, INotifyPropertyChanged
    {
        private bool _alternating;
        private long _editable;
        private readonly Timer _timer;

        public ObservablePropertyTest()
        {
            _timer = new Timer( Alternate );
            _timer.Change( TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 1 ) );
        }

        private void Alternate( object state )
        {
            Alternating = !Alternating;
        }

        public long FixedValue => 42;

        public bool Alternating
        {
            get => _alternating;
            set => Set( ref _alternating, value );
        }

        public long Editable
        {
            get => _editable;
            set => Set( ref _editable, value );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetValue( long value )
        {
            Editable = value;
        }

        private void Set<T>( ref T field, T value, [CallerMemberName] string property = null )
        {
            if(!EqualityComparer<T>.Default.Equals( field, value ))
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( property ) );
            }
        }
    }
}
