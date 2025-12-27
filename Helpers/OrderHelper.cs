using System;

namespace OrMan.Helpers
{
    public static class OrderHelper
    {
        /// <summary>
        /// Generate a consistent order id. Default prefix is "HD".
        /// Format: HDyyyyMMddHHmmss (year-month-day-hour-minute-second) to avoid collisions.
        /// </summary>
        public static string GenerateOrderId(string prefix = "HD")
        {
            return prefix + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }
}