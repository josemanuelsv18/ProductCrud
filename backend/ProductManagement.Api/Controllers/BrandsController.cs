using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Data;
using ProductManagement.Api.Dtos.Brands;

namespace ProductManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BrandsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BrandResponse>>> GetAll()
    {
        var brands = await context.Brands
            .OrderBy(x => x.Name)
            .Select(x => new BrandResponse(x.Id, x.Name))
            .ToListAsync();

        return Ok(brands);
    }
}
