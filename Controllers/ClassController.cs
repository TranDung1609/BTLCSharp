using BTL.Middleware;
using BTL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [CustomAuthorization("A_Very_Secure_Secret_Key_With_32_Characters")]
    public class ClassController : ControllerBase
    {
        private readonly AdsMongoDbContext _dbContext;
        public ClassController(AdsMongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("GetClasses")]
        public async Task<IActionResult> GetClasses([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var skip = (page - 1) * pageSize;

                var classList = _dbContext.classes
                    .Where(_ => true)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToArray();

                var totalClasses = _dbContext.classes
                    .Count(_ => true);

                return Ok(new
                {
                    totalItems = totalClasses,
                    totalPages = (int)Math.Ceiling((double)totalClasses / pageSize),
                    currentPage = page,
                    pageSize = pageSize,
                    data = classList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    code = 0,
                    message = "An error occurred",
                    error = ex.Message
                });
            }
        }
    }
}
