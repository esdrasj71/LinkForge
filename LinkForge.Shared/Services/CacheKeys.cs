using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkForge.Shared.Services;

public static class CacheKeys
{
    public static string Link(string shortCode) => $"link:{shortCode}";
}