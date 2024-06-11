﻿using BookshopAPI.Models;
using BookshopAPI.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookshopAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : Controller
    {
        private IConfiguration configuration = new MyDbContextService().GetConfiguration();
        private MyDbContext myDbContext = new MyDbContextService().GetMyDbContext();
        [HttpGet("getAllProduct")]
        public IActionResult getAllProduct()
        {
            return Ok(myDbContext.Products);
        }

        [HttpGet("getProductByName")]
        public IActionResult getByName(string name)
        {
            var products = myDbContext.Products.Where(x =>
            x.name.Contains(name)).ToList();
            if (products != null)
            {
                return Ok(products);
            }
            else
            {
                return NotFound();
            }

        }
        [HttpGet("getSimilarProduct")]
        public IActionResult getSimilarProduct(long productId)
        {
            var category_product = myDbContext.Product_Categories.SingleOrDefault(x => x.productId == productId);
            if (category_product != null)
            {
                var products = from p in myDbContext.Products
                               join c in myDbContext.Product_Categories on p.id equals c.productId
                               where c.categoryId == category_product.categoryId
                               select new { p };
                if (products != null)
                {
                    return Ok(products);
                }
            }
            return NotFound();
        }


        [HttpGet("getProductByCategory{Id}")]
        public IActionResult getProductByCategoryId(long categoryId)
        {
            
            var products = from p in myDbContext.Products
                            join c in myDbContext.Product_Categories on p.id equals c.productId
                            where c.categoryId == categoryId
                            select new { p };
            if (products != null)
            {
                return Ok(products);
            }
            
            return NotFound();
        }
        [HttpGet("getProductByCategory{Name}")]
        public IActionResult getProductByCategoryName(String categoryName)
        {
            var category = myDbContext.Categories.FirstOrDefault(x => x.name ==  categoryName);
            if(category != null)
            {
                var products = from p in myDbContext.Products
                               join c in myDbContext.Product_Categories on p.id equals c.productId
                               where c.categoryId == category.id
                               select new { p };
                if (products != null)
                {
                    return Ok(products);
                }
            }

            return NotFound();
        }


        [HttpPut("updateProduct")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult update(Product product)
        {
            var _product = myDbContext.Products.SingleOrDefault(x => (x.id) == product.id);
            if (_product != null)
            {
                _product = product;
                var rs = myDbContext.SaveChanges();
                if (rs > 0)
                {
                    return Ok(_product);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Có lỗi từ server, vui lòng thử lại sau!");
                }
            }
            else
            {
                return NotFound("Id của sản phẩm trên không tồn tại");
            }


        }
    }
}