﻿using Boxnet.Aws.Mvp.Newtworking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.IntegrationTests.Networking
{
    [TestClass]
    public class VPCTests
    {
        private readonly string boxnetAwsAccessKeyId = Environment.GetEnvironmentVariable("BoxnetAwsAccessKeyId");
        private readonly string boxnetAwsAccessKey = Environment.GetEnvironmentVariable("BoxnetAwsAccessKey");
        private readonly string infraAppAccessKeyId = Environment.GetEnvironmentVariable("InfraAppAccessKeyId");
        private readonly string infraAppAccessKey = Environment.GetEnvironmentVariable("InfraAppAccessKey");
        private readonly string defaultAwsEndpointRegion = Environment.GetEnvironmentVariable("DefaultAwsEndpointRegion");

        private const string StackName = "Summer";
        private const string StackEnvironment = "Homolog";
        private const string FilterName = "SummerProd";
        private const string VPCName = "SummerProd_REDE_BOXNET";
        private const string SubnetsPrefix = "SummerProd_SUB_MIDDLE_";
        private const string SecurityGroupName = "SummerProd_lambda-integracoes";

        [TestMethod]
        public async Task TestVPCs()
        {
            var stack = new Stack()
            {
                Name = StackName,
                Environment = StackEnvironment
            };

            using (var service = new VpcsService(
                stack,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion,
                boxnetAwsAccessKeyId,
                boxnetAwsAccessKey,
                defaultAwsEndpointRegion))
            {
                await service.CopyAllNetworkingResources(
                    new ResourceNamePrefixInsensitiveCaseFilter(VPCName),
                    new ResourceNamePrefixInsensitiveCaseFilter(SubnetsPrefix),
                    new ResourceNamePrefixInsensitiveCaseFilter(SecurityGroupName));
            }
        }
    }
}
