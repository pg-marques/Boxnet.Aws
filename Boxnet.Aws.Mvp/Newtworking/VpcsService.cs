using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;

namespace Boxnet.Aws.Mvp.Newtworking
{
    public class VpcsService : IDisposable
    {
        private const string NameKey = "Name";
        private const string VpcIdFilter = "vpc-id";
        protected readonly AmazonEC2Client sourceClient;
        protected readonly AmazonEC2Client destinationClient;
        protected readonly Stack stack;
        protected readonly List<Tag> tags;


        public VpcsService(
            Stack stack,
            string sourceAccessKey,
            string sourceSecretKey,
            string sourceRegion,
            string destinationAccessKey,
            string destinationSecretKey,
            string destinationRegion)
        {
            this.stack = stack;
            sourceClient = new AmazonEC2Client(new BasicAWSCredentials(sourceAccessKey, sourceSecretKey), RegionEndpoint.GetBySystemName(sourceRegion));
            destinationClient = new AmazonEC2Client(new BasicAWSCredentials(destinationAccessKey, destinationSecretKey), RegionEndpoint.GetBySystemName(destinationRegion));
            tags = new List<Tag>()
            {
                new Tag()
                {
                    Key = "Project",
                    Value = stack.Name
                },
                new Tag()
                {
                    Key = "Environment",
                    Value = stack.Environment
                },
                new Tag()
                {
                    Key = "ProjectEnvironment",
                    Value = string.Format("{0}{1}", stack.Name, stack.Environment)
                }
            };
            //new CreateDhcpOptionsRequest()
            //{

            //};

            //new CreateRouteTableRequest()
            //{

            //};

            //new Amazon.EC2.Model.CreateVpcRequest()
            //{                  
            //    AmazonProvidedIpv6CidrBlock = false,
            //    CidrBlock = null,
            //    InstanceTenancy = null
            //};
            //new Amazon.EC2.Model.CreateSubnetRequest()
            //{
            //    AvailabilityZone = null,
            //    AvailabilityZoneId = null,
            //    CidrBlock = null,
            //    Ipv6CidrBlock = null,
            //    VpcId = null,
            //};
            //new Amazon.EC2.Model.CreateSecurityGroupRequest()
            //{
            //    Description = "",
            //    GroupName = "",
            //    VpcId = "",
            //};

        }

        public async Task CopyAllNetworkingResources(IResourceNameFilter vpcFilter, IResourceNameFilter subnetsFilter, IResourceNameFilter securityGroupsFilter)
        {
            var collection = await ListVpcsOnSourceAsync(vpcFilter);
            var convertedCollection = ConvertToVpcs(collection);
            await CreateVPCs(convertedCollection);
            await CopySubnetsAsync(convertedCollection, subnetsFilter);
            await CopySecurityGroupsAsync(convertedCollection, securityGroupsFilter);
        }

        private async Task CopySecurityGroupsAsync(List<AwsVpc> convertedCollection, IResourceNameFilter securityGroupsFilter)
        {
            var sourceSecurityGroups = await ListSecurityGroupsOnSourceAsync(convertedCollection, securityGroupsFilter);
            foreach (var group in sourceSecurityGroups)
            {
                var vpc = convertedCollection.FirstOrDefault(item => item.Id.PreviousId == group.VpcId);
                if (vpc != null)
                    vpc.SecurityGroups.Add(new AwsSecurityGroup()
                    {
                        Id = new ResourceIdWithAwsId()
                        {
                            PreviousId = group.GroupId,
                            PreviousName = group.Name(),
                            NewName = NewNameFor(group.Name())
                        },
                        Description = group.Description,
                        VpcId = vpc.Id
                    });
            }

            var groups = convertedCollection.SelectMany(item => item.SecurityGroups).ToList();
            var existingSecurityGroups = await ListSecurityGroupsOnDestinationAsync(convertedCollection);
            foreach (var subnet in groups)
            {
                var existingSubnet = existingSecurityGroups.FirstOrDefault(item => item.Name() == subnet.Id.NewName);
                if (existingSubnet != null)
                {
                    subnet.Id.NewId = existingSubnet.GroupId;
                }
            }

            var pendingGroups = groups.Where(item => string.IsNullOrWhiteSpace(item.Id.NewId)).ToList();

            foreach (var group in pendingGroups)
            {
                var groupRequest = new CreateSecurityGroupRequest()
                {
                    Description = group.Description,
                    GroupName = group.Id.NewName,
                    VpcId = group.VpcId.NewId
                };

                var groupResponse = await destinationClient.CreateSecurityGroupAsync(groupRequest);
                group.Id.NewId = groupResponse.GroupId;

                var nameRequest = new CreateTagsRequest()
                {
                    Resources = new List<string>() { group.Id.NewId },
                    Tags = new List<Tag>()
                    {
                        new Tag()
                        {
                            Key = NameKey,
                            Value = group.Id.NewName
                        }
                    }
                };

                await destinationClient.CreateTagsAsync(nameRequest);
            }

            if (pendingGroups.Count() > 0)
            {
                var tagsRequest = new CreateTagsRequest()
                {
                    Resources = groups.Select(group => group.Id.NewId).ToList(),
                    Tags = tags
                };

                await destinationClient.CreateTagsAsync(tagsRequest);
            }
        }

        private async Task<List<SecurityGroup>> ListSecurityGroupsOnSourceAsync(List<AwsVpc> convertedCollection, IResourceNameFilter securityGroupsFilter)
        {
            var groups = new List<SecurityGroup>();
            string token = null;
            do
            {
                var request = new DescribeSecurityGroupsRequest()
                {
                    Filters = new List<Filter>()
                    {
                        new Filter()
                        {                            
                            Name = VpcIdFilter,
                            Values = convertedCollection.Select(item => item.Id.PreviousId).ToList()                            
                        }
                    },
                    NextToken = token
                };

                var response = await sourceClient.DescribeSecurityGroupsAsync(request);

                groups.AddRange(response.SecurityGroups.Where(item => securityGroupsFilter.IsValid(item.Name())).ToList());

                token = response.NextToken;

            } while (token != null);
            return groups;
        }

        private async Task<List<SecurityGroup>> ListSecurityGroupsOnDestinationAsync(List<AwsVpc> convertedCollection)
        {
            var groups = new List<SecurityGroup>();
            string token = null;
            do
            {
                var request = new DescribeSecurityGroupsRequest()
                {
                    Filters = new List<Filter>()
                    {
                        new Filter()
                        {
                            Name = VpcIdFilter,
                            Values = convertedCollection.Select(item => item.Id.NewId).ToList()
                        }
                    },
                    NextToken = token
                };

                var response = await destinationClient.DescribeSecurityGroupsAsync(request);

                groups.AddRange(response.SecurityGroups);

                token = response.NextToken;

            } while (token != null);
            return groups;
        }

        private async Task CopySubnetsAsync(List<AwsVpc> convertedCollection, IResourceNameFilter subnetsFilter)
        {
            var sourceSubnets = await ListSubnetsOnSourceAsync(convertedCollection, subnetsFilter);
            foreach (var subnet in sourceSubnets)
            {
                var vpc = convertedCollection.FirstOrDefault(item => item.Id.PreviousId == subnet.VpcId);
                if (vpc != null)
                    vpc.Subnets.Add(new AwsSubnet()
                    {
                        Id = new AwsSubnetId()
                        {
                            PreviousArn = subnet.SubnetArn,
                            PreviousId = subnet.SubnetId,
                            PreviousName = subnet.Name(),
                            NewName = NewNameFor(subnet.Name())
                        },
                        AvailabilityZone = subnet.AvailabilityZone,
                        AvailabilityZoneId = subnet.AvailabilityZoneId,
                        VpcId = vpc.Id,
                        CidrBlock = subnet.CidrBlock
                    });
            }
            var subnets = convertedCollection.SelectMany(item => item.Subnets).ToList();
            var existingSubnets = await ListSubnetsOnDestinationAsync(convertedCollection);
            foreach (var subnet in subnets)
            {
                var existingSubnet = existingSubnets.Subnets.FirstOrDefault(item => item.Name() == subnet.Id.NewName);
                if (existingSubnet != null)
                {
                    subnet.Id.NewId = existingSubnet.SubnetId;
                    subnet.Id.NewArn = existingSubnet.SubnetArn;
                }
            }

            var pendingSubnets = subnets.Where(item => string.IsNullOrWhiteSpace(item.Id.NewId)).ToList();

            foreach (var subnet in pendingSubnets)
            {
                var subnetRequest = new CreateSubnetRequest()
                {
                    AvailabilityZone = subnet.AvailabilityZone,
                    //AvailabilityZoneId = subnet.AvailabilityZoneId,
                    CidrBlock = subnet.CidrBlock,
                    VpcId = subnet.VpcId.NewId
                };

                var subnetResponse = await destinationClient.CreateSubnetAsync(subnetRequest);
                subnet.Id.NewId = subnetResponse.Subnet.SubnetId;
                subnet.Id.NewArn = subnetResponse.Subnet.SubnetArn;

                var nameRequest = new CreateTagsRequest()
                {
                    Resources = new List<string>() { subnet.Id.NewId },
                    Tags = new List<Tag>()
                    {
                        new Tag()
                        {
                            Key = NameKey,
                            Value = subnet.Id.NewName
                        }
                    }
                };

                await destinationClient.CreateTagsAsync(nameRequest);
            }

            if (pendingSubnets.Count() > 0)
            {
                var tagsRequest = new CreateTagsRequest()
                {
                    Resources = subnets.Select(subnet => subnet.Id.NewId).ToList(),
                    Tags = tags
                };

                await destinationClient.CreateTagsAsync(tagsRequest);
            }
        }

        private async Task<DescribeSubnetsResponse> ListSubnetsOnDestinationAsync(List<AwsVpc> convertedCollection)
        {
            var request = new DescribeSubnetsRequest()
            {
                Filters = new List<Filter>()
                {
                    new Filter()
                    {
                        Name = VpcIdFilter,
                        Values = convertedCollection.Select(item => item.Id.NewId).ToList()
                    }
                }
            };

            var response = await destinationClient.DescribeSubnetsAsync(request);
            return response;
        }

        private async Task<List<Subnet>> ListSubnetsOnSourceAsync(List<AwsVpc> convertedCollection, IResourceNameFilter subnetsFilter)
        {
            var request = new DescribeSubnetsRequest()
            {
                Filters = new List<Filter>()
                {
                    new Filter()
                    {
                        Name = VpcIdFilter,
                        Values = convertedCollection.Select(item => item.Id.PreviousId).ToList()
                    }
                }
            };

            var response = await sourceClient.DescribeSubnetsAsync(request);
            return response.Subnets.Where(item => subnetsFilter.IsValid(item.Name())).ToList();
        }

        private List<AwsVpc> ConvertToVpcs(List<Vpc> collection)
        {
            return collection.Select(item => new AwsVpc()
            {
                Id = new ResourceIdWithAwsId()
                {
                    PreviousName = item.Name(),
                    NewName = NewNameFor(item.Name()),
                    PreviousId = item.VpcId
                },
                CidrBlock = item.CidrBlock,
                Tenancy = item.InstanceTenancy.Value
            }).ToList();
        }

        private async Task CreateVPCs(List<AwsVpc> vpcs)
        {
            var existingVpcs = await ListVpcsOnDestinationAsync();
            foreach (var vpc in vpcs)
            {
                var existingVpc = existingVpcs.FirstOrDefault(item => item.Name() == vpc.Id.NewName);
                if (existingVpc != null)
                    vpc.Id.NewId = existingVpc.VpcId;
            }

            var pendingVpcs = vpcs.Where(vpc => string.IsNullOrWhiteSpace(vpc.Id.NewId)).ToList();

            foreach (var vpc in pendingVpcs)
            {
                var request = new CreateVpcRequest()
                {
                    AmazonProvidedIpv6CidrBlock = false,
                    CidrBlock = vpc.CidrBlock,
                    InstanceTenancy = Tenancy.FindValue(vpc.Tenancy)
                };

                var response = await destinationClient.CreateVpcAsync(request);
                vpc.Id.NewId = response.Vpc.VpcId;

                var tagsRequest = new CreateTagsRequest()
                {
                    Resources = new List<string>() { vpc.Id.NewId },
                    Tags = new List<Tag>()
                    {
                        new Tag()
                        {
                            Key = NameKey,
                            Value = vpc.Id.NewName
                        }
                    }
                };
                await destinationClient.CreateTagsAsync(tagsRequest);
            }

            if (pendingVpcs.Count > 0)
            {
                var allTagsRequest = new CreateTagsRequest()
                {
                    Resources = pendingVpcs.Select(vpc => vpc.Id.NewId).ToList(),
                    Tags = tags
                };

                await destinationClient.CreateTagsAsync(allTagsRequest);
            }
        }

        private async Task<List<Vpc>> ListVpcsOnSourceAsync(IResourceNameFilter filter)
        {
            var vpcs = await ListVPCs(filter, sourceClient);
            var existingFilter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            return vpcs.Where(vpc => !existingFilter.IsValid(vpc.Name())).ToList();
        }

        private async Task<List<Vpc>> ListVpcsOnDestinationAsync()
        {
            return await ListVPCs(new ResourceNamePrefixInsensitiveCaseFilter(Prefix()), sourceClient);
        }

        private async Task<List<Vpc>> ListVPCs(IResourceNameFilter filter, AmazonEC2Client client)
        {
            var vpcs = new List<Vpc>();
            string token = null;
            do
            {
                var request = new DescribeVpcsRequest()
                {
                    NextToken = token
                };

                var response = await client.DescribeVpcsAsync(request);
                vpcs.AddRange(response.Vpcs.Where(vpc => filter.IsValid(vpc.Name())));
                token = response.NextToken;

            } while (token != null);

            return vpcs;
        }

        protected string NewNameFor(string name)
        {
            return string.Format("{0}{1}", Prefix(), name);
        }

        protected string Prefix()
        {
            return string.Format("{0}{1}_", stack.Name, stack.Environment);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    sourceClient.Dispose();
                    destinationClient.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
