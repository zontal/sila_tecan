using Common.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tecan.Sila2.Interop.Server.Pages
{
    public class InteroperabilityModel : PageModel
    {
        private readonly ILog _logger;

        public InteroperabilityModel( ILog logger )
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}