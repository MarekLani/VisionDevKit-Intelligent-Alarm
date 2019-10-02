using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class Entry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> Entrants { get; set; }
        public string Location { get; set; } = "area1";
        public string ImageName { get; set; }

        public Entry(string id, DateTime timeStamp, List<string> entrants, string imageName)
        {
            this.Timestamp = timeStamp;
            this.Entrants = entrants;
            this.Id = id;
            this.ImageName = imageName;
        }
    }
}
