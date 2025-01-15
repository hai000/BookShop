using BookshopAPI.Models;
using BookshopAPI.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookshopAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private MyDbContext myDbContext = new MyDbContextService().GetMyDbContext();
        private ResponeMessage responeMessage = new ResponeMessage();
        [HttpGet("getAll")]
        public async Task<IActionResult> getAllCategorie()
        {
            return Ok(await myDbContext.Categories.ToListAsync());
        }

    }
}