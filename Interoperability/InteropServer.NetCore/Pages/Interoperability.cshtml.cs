using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tecan.Sila2.Interop.Server.Pages
{
    public class InteroperabilityModel : PageModel
    {
        private readonly ILogger<InteroperabilityModel> _logger;

        public InteroperabilityModel( ILogger<InteroperabilityModel> logger )
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}