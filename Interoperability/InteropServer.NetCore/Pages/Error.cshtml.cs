using Common.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace Tecan.Sila2.Interop.Server.Pages
{
    [ResponseCache( Duration = 0, Location = ResponseCacheLocation.None, NoStore = true )]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty( RequestId );

        private readonly ILog _logger;

        public ErrorModel()
        {
        }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}