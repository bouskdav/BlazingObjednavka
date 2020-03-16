using System;
using System.Collections.Generic;
using System.Text;

namespace BlazingObjednavka.Shared
{
    public class OrderWithStatus
    {
        public readonly static TimeSpan PreparationDuration = TimeSpan.FromSeconds(10);
        public readonly static TimeSpan DeliveryDuration = TimeSpan.FromMinutes(30); // Unrealistic, but more interesting to watch

        public Order Order { get; set; }

        public string StatusText { get; set; }

        public List<Marker> MapMarkers { get; set; }

        public static OrderWithStatus FromOrder(Order order)
        {
            // To simulate a real backend process, we fake status updates based on the amount
            // of time since the order was placed
            string statusText;
            List<Marker> mapMarkers;
            var dispatchTime = order.CreatedTime.Add(PreparationDuration);

            //if (DateTime.Now < dispatchTime)
            if (!order.CanceledTime.HasValue && !order.DeliveredTime.HasValue && !order.PreparedTime.HasValue)
            {
                statusText = "Připravuje se";
                mapMarkers = new List<Marker>
                {
                    ToMapMarker("Vy", order.DeliveryLocation, showPopup: true)
                };
            }
            //else if (DateTime.Now < dispatchTime + DeliveryDuration)
            else if (!order.CanceledTime.HasValue && !order.DeliveredTime.HasValue && order.PreparedTime.HasValue)
            {
                statusText = "Na cestě";

                var startPosition = ComputeStartPosition(order);
                var proportionOfDeliveryCompleted = Math.Min(1, (DateTime.Now - dispatchTime).TotalMilliseconds / DeliveryDuration.TotalMilliseconds);
                var driverPosition = LatLong.Interpolate(startPosition, order.DeliveryLocation, proportionOfDeliveryCompleted);
                mapMarkers = new List<Marker>
                {
                    ToMapMarker("Vy", order.DeliveryLocation),
                    ToMapMarker("Řidič", driverPosition, showPopup: true),
                };
            }
            else if (!order.CanceledTime.HasValue && order.DeliveredTime.HasValue && order.PreparedTime.HasValue)
            {
                statusText = "Doručeno";
                mapMarkers = new List<Marker>
                {
                    ToMapMarker("Adresa doručení", order.DeliveryLocation, showPopup: true),
                };
            }
            else
            {
                statusText = "Neznámý";
                mapMarkers = new List<Marker>
                {
                    ToMapMarker("Adresa doručení", order.DeliveryLocation, showPopup: true),
                };
            }

            return new OrderWithStatus
            {
                Order = order,
                StatusText = statusText,
                MapMarkers = mapMarkers,
            };
        }

        private static LatLong ComputeStartPosition(Order order)
        {
            //// Random but deterministic based on order ID
            //var rng = new Random(order.OrderId);
            //var distance = 0.01 + rng.NextDouble() * 0.02;
            //var angle = rng.NextDouble() * Math.PI * 2;
            //var offset = (distance * Math.Cos(angle), distance * Math.Sin(angle));
            //return new LatLong(order.DeliveryLocation.Latitude + offset.Item1, order.DeliveryLocation.Longitude + offset.Item2);

            return new LatLong(50.0861328282, 14.4518118271);
        }

        static Marker ToMapMarker(string description, LatLong coords, bool showPopup = false)
            => new Marker { Description = description, X = coords.Longitude, Y = coords.Latitude, ShowPopup = showPopup };
    }
}
