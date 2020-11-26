using System;
using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2.Generator.Helper
{
    internal class AnonymousTypeHelper
    {
        private Dictionary<string, StructureType> _structureTypes = new Dictionary<string, StructureType>();


        public void RegisterAnonymousType( string name, StructureType anonymousType )
        {
            if( !_structureTypes.ContainsKey( name ) )
            {
                _structureTypes.Add( name, anonymousType );
            }
        }

        public void ProcessAll( Action<string, StructureType> processAction )
        {
            var processed = new HashSet<string>(_structureTypes.Keys);
            foreach( var typeName in processed )
            {
                processAction?.Invoke( typeName, _structureTypes[typeName] );
            }

            while( _structureTypes.Keys.Except(processed).Any() )
            {
                var typeName = _structureTypes.Keys.Except( processed ).First();
                processed.Add( typeName );
                processAction?.Invoke( typeName, _structureTypes[typeName] );
            }
        }
    }
}
