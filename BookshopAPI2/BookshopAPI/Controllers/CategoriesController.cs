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
            return Ok(responeMessage.response200(await myDbContext.Categories.ToListAsync()));
        }
        [HttpGet("getByProduct/productId:{productId}")]
        public async Task<IActionResult> getByProduct(long productId)
        {
            var product = await myDbContext.Products.SingleOrDefaultAsync(x => x.id == productId);
            if (product == null)
            {
                return Ok(responeMessage.response400(null, "Product id không chính xác"));
            }
            else
            {
                var product_Category = await myDbContext.Product_Categories.SingleOrDefaultAsync(x => x.productId == productId);
                var category = await myDbContext.Categories.SingleOrDefaultAsync(x => x.id == product_Category.categoryId);
                if (category == null)
                {
                    return Ok(responeMessage.response400);
                }
                else
                {
                    return Ok(responeMessage.response200(category));
                }
            }
        }
    }
}