using Amazon.EC2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boxnet.Aws.Mvp.Newtworking
{
    internal static class NewtworkingExtensions
    {
        private const string NameKey = "Name";

        public static string Name(this Vpc vpc)
        {
            return vpc.Tags?.FirstOrDefault(tag => tag.Key == NameKey)?.Value;
        }

        public static string Name(this Subnet subnet)
        {
            return subnet.Tags?.FirstOrDefault(tag => tag.Key == NameKey)?.Value;
        }

        public static string Name(this SecurityGroup securityGroup)
        {
            return securityGroup.Tags?.FirstOrDefault(tag => tag.Key == NameKey)?.Value;
        }
    }
}
