using BlazingObjednavka.Server.Models;
using BlazingObjednavka.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using WebPush;

namespace BlazingObjednavka.Server.Controllers
{
    [Route("orders")]
    [ApiController]
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly PizzaStoreContext _db;

        public OrdersController(PizzaStoreContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<OrderWithStatus>>> GetOrders()
        {
            var orders = await _db.Orders
                .Where(o => o.UserId == GetUserId())
                .Include(o => o.DeliveryLocation)
                .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                .OrderByDescending(o => o.CreatedTime)
                .ToListAsync();

            return orders.Select(o => OrderWithStatus.FromOrder(o)).ToList();
        }

        [HttpGet("adminorders")]
        public async Task<ActionResult<List<OrderWithStatus>>> GetOrdersAdmin()
        {
            var orders = await _db.Orders
                .Where(o => !o.CanceledTime.HasValue && !o.DeliveredTime.HasValue)
                .Include(o => o.DeliveryLocation)
                .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                .OrderByDescending(o => o.CreatedTime)
                .ToListAsync();

            return orders.Select(o => OrderWithStatus.FromOrder(o)).ToList();
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<OrderWithStatus>> GetOrderWithStatus(int orderId)
        {
            var order = await _db.Orders
                .Where(o => o.OrderId == orderId)
                .Where(o => o.UserId == GetUserId())
                .Include(o => o.DeliveryLocation)
                .Include(o => o.Pizzas).ThenInclude(p => p.Special)
                .Include(o => o.Pizzas).ThenInclude(p => p.Toppings).ThenInclude(t => t.Topping)
                .SingleOrDefaultAsync();

            if (order == null)
            {
                return NotFound();
            }

            return OrderWithStatus.FromOrder(order);
        }

        [HttpPost]
        public async Task<ActionResult<int>> PlaceOrder(Order order)
        {
            order.CreatedTime = DateTime.Now;

            // address and geolocation
            string addressString = String.Join(", ", order.DeliveryAddress.Line1, order.DeliveryAddress.Line2, order.DeliveryAddress.City, order.DeliveryAddress.PostalCode).Replace(", ,", ",");

            var geolocation = await MapyCZGeolocation(addressString);

            if (geolocation != null)
            {
                var bestMatch = geolocation.Points?.FirstOrDefault();

                if (bestMatch != null && bestMatch.Items != null && bestMatch.Items.Count > 0)
                {
                    var bestMatchItem = bestMatch.Items.FirstOrDefault();

                    order.DeliveryLocation = new LatLong(bestMatchItem.Latitude, bestMatchItem.Longitude);
                }
                else
                {
                    order.DeliveryLocation = new LatLong(0, 0);
                }
            }
            else
            {
                order.DeliveryLocation = new LatLong(0, 0);
            }

            order.UserId = GetUserId();

            // Enforce existence of Pizza.SpecialId and Topping.ToppingId
            // in the database - prevent the submitter from making up
            // new specials and toppings
            foreach (var pizza in order.Pizzas)
            {
                pizza.SpecialId = pizza.Special.Id;
                pizza.Special = null;

                foreach (var topping in pizza.Toppings)
                {
                    topping.ToppingId = topping.Topping.Id;
                    topping.Topping = null;
                }
            }

            _db.Orders.Attach(order);
            await _db.SaveChangesAsync();

            // In the background, send push notifications if possible
            var subscription = await _db.NotificationSubscriptions.Where(e => e.UserId == GetUserId()).SingleOrDefaultAsync();
            if (subscription != null)
            {
                _ = TrackAndSendNotificationsAsync(order, subscription);
            }

            return order.OrderId;
        }

        [HttpGet("setprepared/{id}")]
        public async Task<bool> SetPrepared(int id)
        {
            var dbOrder = await _db.Orders.SingleOrDefaultAsync(i => i.OrderId == id);

            dbOrder.PreparedTime = DateTime.Now;

            _db.Entry(dbOrder).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return true;
        }

        [HttpGet("setdelivered/{id}")]
        public async Task<bool> SetDelivered(int id)
        {
            var dbOrder = await _db.Orders.SingleOrDefaultAsync(i => i.OrderId == id);

            dbOrder.DeliveredTime = DateTime.Now;

            _db.Entry(dbOrder).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return true;
        }

        [HttpGet("setcanceled/{id}")]
        public async Task<bool> SetCanceled(int id)
        {
            var dbOrder = await _db.Orders.SingleOrDefaultAsync(i => i.OrderId == id);

            dbOrder.CanceledTime = DateTime.Now;

            _db.Entry(dbOrder).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return true;
        }

        [HttpGet("setnotcanceled/{id}")]
        public async Task<bool> SetNotCanceled(int id)
        {
            var dbOrder = await _db.Orders.SingleOrDefaultAsync(i => i.OrderId == id);

            dbOrder.CanceledTime = null;

            _db.Entry(dbOrder).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return true;
        }

        [HttpGet("getlocation/{address}")]
        public async Task<MapyCZGeolocationResponse> MapyCZGeolocation(string address)
        {
            //We will make a GET request to a really cool website...

            string baseUrl = $"https://api.mapy.cz/geocode?query={WebUtility.UrlEncode(address)}&count=1";
            //The 'using' will help to prevent memory leaks.
            //Create a new instance of HttpClient
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage res = await client.GetAsync(baseUrl))
            using (HttpContent content = res.Content)
            {
                string data = await content.ReadAsStringAsync();
                if (data != null)
                {
                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
                    XmlSerializer serializer = new XmlSerializer(typeof(MapyCZGeolocationResponse));

                    var response = (MapyCZGeolocationResponse)serializer.Deserialize(ms);

                    return response;
                }
                else
                {
                    return null;
                }
            }
        }

        private string GetUserId()
        {
            // This will be the user's twitter username
            return HttpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
        }

        private static async Task TrackAndSendNotificationsAsync(Order order, NotificationSubscription subscription)
        {
            // In a realistic case, some other backend process would track
            // order delivery progress and send us notifications when it
            // changes. Since we don't have any such process here, fake it.
            await Task.Delay(OrderWithStatus.PreparationDuration);
            await SendNotificationAsync(order, subscription, "Vaše objednávka se vypravila na cestu!");

            await Task.Delay(OrderWithStatus.DeliveryDuration);
            await SendNotificationAsync(order, subscription, "Vaše objednávka je doručena. Dobrou chuť!");
        }

        private static async Task SendNotificationAsync(Order order, NotificationSubscription subscription, string message)
        {
            // For a real application, generate your own
            var publicKey = "BLC8GOevpcpjQiLkO7JmVClQjycvTCYWm6Cq_a7wJZlstGTVZvwGFFHMYfXt6Njyvgx_GlXJeo5cSiZ1y4JOx1o";
            var privateKey = "OrubzSz3yWACscZXjFQrrtDwCKg-TGFuWhluQ2wLXDo";

            var pushSubscription = new PushSubscription(subscription.Url, subscription.P256dh, subscription.Auth);
            var vapidDetails = new VapidDetails("mailto:<someone@example.com>", publicKey, privateKey);
            var webPushClient = new WebPushClient();
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    message,
                    url = $"myorders/{order.OrderId}",
                });
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error sending push notification: " + ex.Message);
            }
        }
    }
}
