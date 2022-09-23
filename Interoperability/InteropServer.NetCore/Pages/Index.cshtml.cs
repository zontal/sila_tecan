using Common.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tecan.Sila2.Interop.Server.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILog _logger;

        public IndexModel( ILog logger )
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}