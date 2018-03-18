using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ApiGateway
{
    public static class Helper
    {
        private static Dictionary<string, string> _applications;
        private static object syncRoot = new Object();
        public static Dictionary<string, string> Applications
        {
            get
            {
                if (_applications == null)
                {
                    lock (syncRoot)
                    {
                        _applications = new Dictionary<string, string>();
                        var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("serviceendpoint.json");

                        var serviceEndPoint = builder.Build();
                        var endPoints = serviceEndPoint.AsEnumerable();
                        foreach (var endPoint in endPoints)
                        {
                            _applications.Add(endPoint.Key, endPoint.Value);
                        }
                    }
                }
                return _applications;
            }
        }
    }
}
