using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using TTRBookings.Core.Entities;
using TTRBookings.Core.Interfaces;
using TTRBookings.Web.Models;

namespace TTRBookings.Web.Pages.Roses
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;
        private readonly IRepository repository;

        [BindProperty] public RoseVM RoseVM { get; set; }

        public CreateModel(IRepository repository)
        {
            this.repository = repository;
        }

        public void OnGet()
        {

        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Rose rose = new Rose(RoseVM.Name);
            rose.HouseId = Guid.Parse(HttpContext.Session.GetString("HouseId"));

            repository.CreateEntry(rose);

            return RedirectToPage("/Roses/Details", new { rose.Id });
        }
    }
}