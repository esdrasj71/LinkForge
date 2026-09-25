using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkForge.Shared.Models
{
    public class ClickEvent
    {
        public long Id { get; set; }
        public int LinkId { get; set; }
        public string? Referrer { get; set; }
        public string? UserAgent { get; set; }
        public string? Country { get; set; }
        public DateTime ClickedAt { get; set; } = DateTime.UtcNow;

        public Link Link { get; set; } = null!;
    }
}
