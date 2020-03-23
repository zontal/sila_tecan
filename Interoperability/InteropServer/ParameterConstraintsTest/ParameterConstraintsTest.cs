using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Text.RegularExpressions;

namespace Tecan.Sila2.Interop.Server
{
    [Export(typeof(IParameterConstraintsTest))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ParameterConstraintsTest : IParameterConstraintsTest
    {
        private readonly Regex _patternRegex = new Regex( "[0-9]{2}/[0-9]{2}/[0-9]{2}/[0-9]{4}", RegexOptions.Compiled );

        public void ExactStringLength( string stringLength, string maximalLengthString, string minimalLengthString, string minMaxLengthString, string setString, string patternString )
        {
            if( stringLength.Length != 10 )
            {
                throw new ArgumentException( "Length must be exactly 10", nameof(stringLength) );
            }

            if( maximalLengthString.Length > 20 )
            {
                throw new ArgumentException( "Length must be at most 20", nameof(maximalLengthString) );
            }

            if( minimalLengthString.Length < 5 )
            {
                throw new ArgumentException( "Length must be at least 5", nameof(minimalLengthString) );
            }

            if( minMaxLengthString.Length < 5 || minMaxLengthString.Length > 20 )
            {
                throw new ArgumentException("Length must be between 5 and 20", nameof(minMaxLengthString));
            }

            if( setString != "First option" && setString != "Second option" && setString != "Third option" )
            {
                throw new ArgumentException("String must be either of the predefined value", nameof(setString));
            }

            if( !_patternRegex.IsMatch( patternString ) )
            {
                throw new ArgumentException("String must be according to pattern", nameof(patternString));
            }
        }

        public void ConstrainedNumericParameters( long integersSet, double maximalExclusiveReal, double minimalExclusiveReal, double minMaxExclusiveReal, long maximalInclusiveInteger,
            long minimalInclusiveInteger, long minMaxInclusiveInteger, long unit )
        {
            if( integersSet != 1 && integersSet != 2 && integersSet != 3 )
            {
                throw new ArgumentException("Value must be 1, 2 or 3", nameof(integersSet));
            }

            if( maximalExclusiveReal >= 10 )
            {
                throw new ArgumentException("Value must be less than 10", nameof(maximalExclusiveReal));
            }

            if( minimalExclusiveReal <= 0 )
            {
                throw new ArgumentException( "Value must be greater than 0", nameof(minimalExclusiveReal) );
            }

            if( minMaxExclusiveReal >= 10 || minMaxExclusiveReal <= 0 )
            {
                throw new ArgumentException( "Value must be between 0 and 10", nameof(minMaxExclusiveReal) );
            }

            if( maximalInclusiveInteger > 10 )
            {
                throw new ArgumentException( "Value must be at most 10", nameof(maximalInclusiveInteger) );
            }

            if( minimalInclusiveInteger < 0 )
            {
                throw new ArgumentException("Value must be at least 0", nameof(minimalInclusiveInteger));
            }

            if( minMaxInclusiveInteger > 10 || minMaxInclusiveInteger < 0 )
            {
                throw new ArgumentException( "Value must be between 0 and 10", nameof(minMaxInclusiveInteger) );
            }
        }

        public void ConstrainedDateTimeParameters( DateTimeOffset dateSet, DateTimeOffset maximalExclusiveDate, DateTimeOffset minimalExclusiveDate, DateTimeOffset minMaxExclusiveDate,
            TimeSpan maximalInclusiveTime, TimeSpan minimalInclusiveTime, TimeSpan minMaxInclusiveTime )
        {
            if( dateSet.Year != 2019 || dateSet.Month != 9 || dateSet.Day < 9 || dateSet.Day > 13 )
            {
                throw new ArgumentException("Value must be in predefined set", nameof(dateSet));
            }

            if( maximalExclusiveDate.Date >= new DateTime( 2019, 12, 31 ) )
            {
                throw new ArgumentException("Value must be before 31 DEC 2019", nameof(maximalExclusiveDate));
            }

            if( minimalExclusiveDate.Date <= new DateTime( 2019, 1, 1 ) )
            {
                throw new ArgumentException("Value must be after 1 JAN 2019", nameof(minimalExclusiveDate));
            }

            if( minMaxExclusiveDate.Date >= new DateTime( 2019, 12, 31 ) || minMaxExclusiveDate.Date <= new DateTime( 2019, 1, 1 ) )
            {
                throw new ArgumentException("Value must be in 2019", nameof(minMaxExclusiveDate));
            }

            var eightOClock = new TimeSpan(8, 0, 0);
            var evening = new TimeSpan(18, 59, 59);
            if( maximalInclusiveTime > evening )
            {
                throw new ArgumentException("Value must be before 7pm", nameof(maximalInclusiveTime));
            }

            if( minimalInclusiveTime < eightOClock )
            {
                throw new ArgumentException("Value must be after 8", nameof(minimalInclusiveTime));
            }

            if( minMaxInclusiveTime > evening || minMaxInclusiveTime < eightOClock )
            {
                throw new ArgumentException("Value must be between 8 and 7pm", nameof(minMaxInclusiveTime));
            }
        }

        public void ConstrainedListParameters( ICollection<string> elementCountList, ICollection<string> maximalElementCountList, ICollection<string> minimalElementCountList, ICollection<string> minMaxElementCountList )
        {
            if( elementCountList?.Count != 5 )
            {
                throw new ArgumentException("Element count must be 5", nameof(elementCountList));
            }

            if( maximalElementCountList?.Count > 5 )
            {
                throw new ArgumentException("Element count must be at most 5", nameof(maximalElementCountList));
            }

            if( minimalElementCountList?.Count < 3 )
            {
                throw new ArgumentException("Element count must be at least 3", nameof(minimalElementCountList));
            }

            if( minMaxElementCountList?.Count > 5 || minMaxElementCountList?.Count < 2 )
            {
                throw new ArgumentException("Element count must be between 2 and 5", nameof(minMaxElementCountList));
            }
        }
    }
}
