using System;
using GreenPipes;
using Newtonsoft.Json;

namespace CqrsVibe.Tests
{
    public static class IntrospectionExtensions
    {
        public static void PrintProbe(this IProbeSite probeSite)
        {
            var probe = probeSite.GetProbeResult();
            Console.WriteLine(JsonConvert.SerializeObject(probe, Formatting.Indented));
        }
    }
}