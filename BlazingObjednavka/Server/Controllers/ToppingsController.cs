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
    [Route("toppings")]
    [ApiController]
    public class ToppingsController : Controller
    {
        private readonly PizzaStoreContext _db;

        public ToppingsController(PizzaStoreContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<Topping>>> GetToppings()
        {
            return await _db.Toppings.OrderBy(t => t.Name).ToListAsync();
        }
    }
}
