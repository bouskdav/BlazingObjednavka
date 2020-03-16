using BlazingObjednavka.Server.Models;
using BlazingObjednavka.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazingObjednavka.Server.Controllers
{
    [Route("specials")]
    [ApiController]
    public class SpecialsController : Controller
    {
        private readonly PizzaStoreContext _db;

        public SpecialsController(PizzaStoreContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<PizzaSpecial>>> GetSpecials()
        {
            return (await _db.Specials.Where(i => i.Active).ToListAsync())
                .OrderBy(s => s.Group)
                //.ThenByDescending(s => s.BasePrice)
                .ToList();
        }
    }
}
