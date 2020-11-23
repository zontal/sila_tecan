using System;

namespace Tecan.Sila2
{
    /// <summary>
    /// Describes a uri of the vendor of the current assembly
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class VendorUriAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new attribute using the given vendor uri string
        /// </summary>
        /// <param name="vendorUri"></param>
        public VendorUriAttribute(string vendorUri)
        {
            VendorUri = vendorUri;
        }

        /// <summary>
        /// The vendor Uri string
        /// </summary>
        public string VendorUri { get; }
    }
}
