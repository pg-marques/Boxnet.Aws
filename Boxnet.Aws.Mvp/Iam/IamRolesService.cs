using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.Iam
{
    public class IamRolesService : IamService
    {
        public IamRolesService(Stack stack, string sourceAccessKey, string sourceSecretKey, string sourceRegion, string destinationAccessKey, string destinationSecretKey, string destinationRegion) : base(stack, sourceAccessKey, sourceSecretKey, sourceRegion, destinationAccessKey, destinationSecretKey, destinationRegion)
        {
        }

        public async Task CopyAllRolesAsync(IResourceNameFilter nameFilter)
        {
            var collectionData = await GetAllRolesAsync(nameFilter);
            var collection = ConvertToRoles(collectionData);
            await CreateAllRolesOnDestinationAsync(collection);

            stack.IamRoles = collection;
        }

        public async Task FillStackWithExistingRoles(IResourceNameFilter filter)
        {
            var collectionData = await GetAllRolesAsync(filter);
            var collection = ConvertToRoles(collectionData);
            await FilterAsync(collection);
            stack.IamRoles = collection;
        }

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>>> GetAllRolesAsync(IResourceNameFilter nameFilter)
        {
            var collectionData = await GetRolesFromSourceAsync(nameFilter);
            var collectionDataWithPolicies = await GetAllPoliciesFromSourceAsync(collectionData);
            var collectionDataWithInlinePoliciesNames = await GetAllInlinePoliciesNamesFromSourceAsync(collectionDataWithPolicies);
            return await GetAllInlinePoliciesFromSourceAsync(collectionDataWithInlinePoliciesNames);
        }

        private List<IamRole> ConvertToRoles(List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>> collection)
        {
            return collection.Select(item => new IamRole()
            {
                Id = new ResourceIdWithArn()
                {
                    PreviousName = item.Item1.Item1.RoleName,
                    NewName = NewNameFor(item.Item1.Item1.RoleName),
                    PreviousArn = item.Item1.Item1.Arn,
                },
                AssumeRolePolicyDocument = ExtracDocumentFrom(item.Item1.Item1.AssumeRolePolicyDocument),
                Description = item.Item1.Item1.Description,
                MaxSessionDuration = item.Item1.Item1.MaxSessionDuration,
                Path = item.Item1.Item1.Path,
                PermissionsBoundary = item.Item1.Item1.PermissionsBoundary,
                AttachedPoliciesIds = item.Item1.Item2.Select(policy => new ResourceIdWithArn()
                {
                    PreviousArn = policy.PolicyArn,
                    PreviousName = policy.PolicyName,
                    NewName = NewNameFor(policy.PolicyName)
                }),
                InlinePolicies = item.Item2.Select(policy => new IamInlinePolicy()
                {
                    Document = ExtracDocumentFrom(policy.PolicyDocument),
                    PreviousName = policy.PolicyName,
                    NewName = NewNameFor(policy.PolicyName)
                })
            }).ToList();
        }

        private async Task<List<Tuple<Role, List<AttachedPolicyType>>>> GetAllPoliciesFromSourceAsync(List<Role> rolesDataCollection)
        {
            var collection = new List<Tuple<Role, List<AttachedPolicyType>>>();

            foreach (var roleData in rolesDataCollection)
            {
                var policies = new List<AttachedPolicyType>();
                string marker = null;
                do
                {
                    var request = new ListAttachedRolePoliciesRequest()
                    {
                        Marker = marker,
                        RoleName = roleData.RoleName
                    };
                    var response = await sourceClient.ListAttachedRolePoliciesAsync(request);

                    policies.AddRange(response.AttachedPolicies);

                    marker = response.Marker;

                } while (marker != null);

                collection.Add(new Tuple<Role, List<AttachedPolicyType>>(roleData, policies));
            }

            return collection;
        }

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>>> GetAllInlinePoliciesNamesFromSourceAsync(List<Tuple<Role, List<AttachedPolicyType>>> rolesDataCollection)
        {
            var collection = new List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>>();

            foreach (var roleData in rolesDataCollection)
            {
                var policiesNames = new List<string>();
                string marker = null;
                do
                {
                    var request = new ListRolePoliciesRequest()
                    {
                        Marker = marker,
                        RoleName = roleData.Item1.RoleName
                    };
                    var response = await sourceClient.ListRolePoliciesAsync(request);

                    policiesNames.AddRange(response.PolicyNames);

                    marker = response.Marker;

                } while (marker != null);

                collection.Add(new Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>(roleData, policiesNames));
            }

            return collection;
        }

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>>> GetAllInlinePoliciesFromSourceAsync(List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>> rolesDataCollection)
        {
            var collection = new List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>>();

            foreach (var roleData in rolesDataCollection)
            {
                var policies = new List<GetRolePolicyResponse>();

                foreach (var policyName in roleData.Item2)
                {
                    var request = new GetRolePolicyRequest()
                    {
                        RoleName = roleData.Item1.Item1.RoleName,
                        PolicyName = policyName
                    };
                    var response = await sourceClient.GetRolePolicyAsync(request);

                    policies.Add(response);
                }
                collection.Add(new Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>(roleData.Item1, policies));
            }

            return collection;
        }

        private async Task<List<string>> GetAllInlinePoliciesNamesFromSourcesAsync(List<Role> rolesDataCollection)
        {
            var collection = new List<string>();

            foreach (var roleData in rolesDataCollection)
            {
                string marker = null;
                do
                {
                    var request = new ListRolePoliciesRequest()
                    {
                        Marker = marker,
                        RoleName = roleData.RoleName
                    };
                    var response = await sourceClient.ListRolePoliciesAsync(request);

                    collection.AddRange(response.PolicyNames);

                    marker = response.Marker;

                } while (marker != null);
            }

            return collection;
        }

        private async Task<List<Role>> GetRolesFromSourceAsync(IResourceNameFilter nameFilter)
        {
            var roles = await GetRolesAsync(nameFilter, sourceClient);

            return roles;//.Where(role => role.Tags == null || role.Tags.Count() < 1).ToList();
        }

        private async Task<List<Role>> GetRolesFromDestinationAsync(string nameFilter)
        {
            var roles = await GetRolesAsync(new ResourceNamePrefixInsensitiveCaseFilter(nameFilter), destinationClient);

            return roles.ToList().Where(role =>
            {
                if (role.Tags == null || role.Tags.Count != tags.Count())
                    return false;

                foreach (var tag in tags)
                    if (!role.Tags.Any(item => item.Key == tag.Key && item.Value == tag.Value))
                        return false;

                return true;
            }).ToList();
        }

        private async Task<List<Tuple<Role, List<AttachedPolicyType>>>> GetAllPoliciesFromDestinationAsync(List<Role> rolesDataCollection)
        {
            var collection = new List<Tuple<Role, List<AttachedPolicyType>>>();

            foreach (var roleData in rolesDataCollection)
            {
                var policies = new List<AttachedPolicyType>();
                string marker = null;
                do
                {
                    var request = new ListAttachedRolePoliciesRequest()
                    {
                        Marker = marker,
                        RoleName = roleData.RoleName
                    };
                    var response = await destinationClient.ListAttachedRolePoliciesAsync(request);

                    policies.AddRange(response.AttachedPolicies);

                    marker = response.Marker;

                } while (marker != null);

                collection.Add(new Tuple<Role, List<AttachedPolicyType>>(roleData, policies));
            }

            return collection;
        }

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>>> GetAllInlinePoliciesNamesFromDestinationAsync(List<Tuple<Role, List<AttachedPolicyType>>> rolesDataCollection)
        {
            var collection = new List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>>();

            foreach (var roleData in rolesDataCollection)
            {
                var policiesNames = new List<string>();
                string marker = null;
                do
                {
                    var request = new ListRolePoliciesRequest()
                    {
                        Marker = marker,
                        RoleName = roleData.Item1.RoleName
                    };
                    var response = await destinationClient.ListRolePoliciesAsync(request);

                    policiesNames.AddRange(response.PolicyNames);

                    marker = response.Marker;

                } while (marker != null);

                collection.Add(new Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>(roleData, policiesNames));
            }

            return collection;
        }

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>>> GetAllInlinePoliciesFromDestinationAsync(List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>> rolesDataCollection)
        {
            var collection = new List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>>();

            foreach (var roleData in rolesDataCollection)
            {
                var policies = new List<GetRolePolicyResponse>();

                foreach (var policyName in roleData.Item2)
                {
                    var request = new GetRolePolicyRequest()
                    {
                        RoleName = roleData.Item1.Item1.RoleName,
                        PolicyName = policyName
                    };
                    var response = await destinationClient.GetRolePolicyAsync(request);

                    policies.Add(response);
                }
                collection.Add(new Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>(roleData.Item1, policies));
            }

            return collection;
        }

        private async Task<List<string>> GetAllInlinePoliciesNamesFromDestinationAsync(List<Role> rolesDataCollection)
        {
            var collection = new List<string>();

            foreach (var roleData in rolesDataCollection)
            {
                string marker = null;
                do
                {
                    var request = new ListRolePoliciesRequest()
                    {
                        Marker = marker,
                        RoleName = roleData.RoleName
                    };
                    var response = await destinationClient.ListRolePoliciesAsync(request);

                    collection.AddRange(response.PolicyNames);

                    marker = response.Marker;

                } while (marker != null);
            }

            return collection;
        }

        private async Task<List<Role>> GetRolesFromDestinationAsync()
        {
            var roles = await GetRolesAsync(new ResourceNamePrefixInsensitiveCaseFilter(Prefix()), destinationClient);

            return roles;//.Where(role => role.Tags == null || role.Tags.Count() < 1).ToList();
        }

        private async Task<List<Role>> GetRolesAsync(IResourceNameFilter filter, AmazonIdentityManagementServiceClient client)
        {
            var collection = new List<Role>();

            string marker = null;
            do
            {
                var request = new ListRolesRequest()
                {
                    Marker = marker
                };

                var response = await client.ListRolesAsync(request);

                collection.AddRange(response.Roles.Where(item => filter.IsValid(item.RoleName)));

                marker = response.Marker;

            } while (marker != null);

            foreach (var item in collection)
            {
                marker = null;
                do
                {
                    var request = new ListRoleTagsRequest()
                    {
                        RoleName = item.RoleName,
                        Marker = marker
                    };

                    var response = await client.ListRoleTagsAsync(request);

                    item.Tags.AddRange(response.Tags);

                    marker = response.Marker;

                } while (marker != null);
            }

            return collection;
        }

        private async Task CreateAllRolesOnDestinationAsync(List<IamRole> collection)
        {
            var filteredCollection = await FilterAsync(collection);
            var policies = new List<ManagedPolicy>();
            string marker = null;
            do
            {
                var existingPoliciesRequest = new ListPoliciesRequest()
                {
                    Marker = marker                    
                };

                var existingPoliciesResponse = await destinationClient.ListPoliciesAsync(existingPoliciesRequest);

                policies.AddRange(existingPoliciesResponse.Policies);

                marker = existingPoliciesResponse.Marker;

            } while (marker != null);

            foreach (var item in filteredCollection)
            {
                var request = new CreateRoleRequest()
                {
                    AssumeRolePolicyDocument = item.AssumeRolePolicyDocument,
                    Description = item.Description,
                    RoleName = item.Id.NewName,
                    MaxSessionDuration = item.MaxSessionDuration,
                    Path = item.Path,
                    PermissionsBoundary = item.PermissionsBoundary?.PermissionsBoundaryArn,
                    Tags = tags
                };

                var response = await destinationClient.CreateRoleAsync(request);
                item.Id.NewArn = response.Role.Arn;
            }

            foreach (var item in collection)
            {
                foreach (var policy in item.InlinePolicies.Where(it => !it.Created).ToList())
                {
                    var request = new PutRolePolicyRequest()
                    {
                        PolicyDocument = policy.Document,
                        PolicyName = policy.NewName,
                        RoleName = item.Id.NewName
                    };

                    await destinationClient.PutRolePolicyAsync(request);
                    policy.Created = true;
                }                
            }
        }

        private async Task<List<IamRole>> FilterAsync(List<IamRole> collection)
        {
            var existingCollection = await GetRolesFromDestinationAsync(Prefix());
            //var collectionData = await GetRolesFromDestinationAsync();
            var collectionDataWithPolicies = await GetAllPoliciesFromDestinationAsync(existingCollection);
            var collectionDataWithInlinePoliciesNames = await GetAllInlinePoliciesNamesFromDestinationAsync(collectionDataWithPolicies);
            var collectionDataWithInlinePolicies = await GetAllInlinePoliciesFromDestinationAsync(collectionDataWithInlinePoliciesNames);
            var items = ConvertToRoles(collectionDataWithInlinePolicies);

            var newCollection = collection.ToList();
            foreach (var item in newCollection)
            {
                var existingItem = items.FirstOrDefault(it => it.Id.PreviousName == item.Id.NewName);

                if (existingItem != null)
                {
                    var arn = existingItem.Id.PreviousArn;

                    if (!string.IsNullOrWhiteSpace(arn))
                        item.Id.NewArn = arn;

                    if(existingItem.InlinePolicies != null)
                    foreach (var policy in item.InlinePolicies)
                    {
                            var existingPolicy = existingItem.InlinePolicies.FirstOrDefault(it => it.PreviousName == policy.NewName);
                            if (existingPolicy != null)
                                policy.Created = true;
                    }
                }
            }

            return newCollection.Where(item => string.IsNullOrWhiteSpace(item.Id.NewArn) && !item.Id.PreviousName.StartsWith("SummerHomolog")).ToList();
        }
    }
}
