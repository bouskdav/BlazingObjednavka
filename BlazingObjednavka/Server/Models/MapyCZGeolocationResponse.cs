using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BlazingObjednavka.Server.Models
{
    [XmlRoot("result")]
    public class MapyCZGeolocationResponse
    {
        [XmlElement("point")]
        public List<MapyCZPoint> Points { get; set; }
    }

    public class MapyCZPoint
    {
        [XmlAttribute("query")]
        public string Query { get; set; }

        [XmlAttribute("status")]
        public int Status { get; set; }

        [XmlAttribute("message")]
        public string Message { get; set; }

        [XmlElement("item")]
        public List<MapyCZItem> Items { get; set; }
    }

    public class MapyCZItem
    {
        [XmlAttribute("id")]
        public int ID { get; set; }

        [XmlAttribute("y")]
        public double Latitude { get; set; }

        [XmlAttribute("x")]
        public double Longitude { get; set; }

        [XmlAttribute("source")]
        public string Source { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("title")]
        public string Title { get; set; }
    }
}
