using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.API.Pages.Verwaltung;

[Authorize(Roles = "Administrator")]
public sealed class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
