﻿using Amazon.IdentityManagement;
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

        public async Task CopyAllRolesAsync(string nameFilter)
        {
            var collectionData = await GetAllRolesAsync(nameFilter);
            var collection = ConvertToRoles(collectionData);
            await CreateAllRolesOnDestinationAsync(collection);

            stack.IamRoles = collection;
        }

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>>> GetAllRolesAsync(string nameFilter)
        {
            var collectionData = await GetRolesFromSourceAsync(nameFilter);
            var collectionDataWithPolicies = await GetAllPoliciesAsync(collectionData);
            var collectionDataWithInlinePoliciesNames = await GetAllInlinePoliciesNamesAsync(collectionDataWithPolicies);
            return await GetAllInlinePoliciesAsync(collectionDataWithInlinePoliciesNames);
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

        private async Task<List<Tuple<Role, List<AttachedPolicyType>>>> GetAllPoliciesAsync(List<Role> rolesDataCollection)
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

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>>> GetAllInlinePoliciesNamesAsync(List<Tuple<Role, List<AttachedPolicyType>>> rolesDataCollection)
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

        private async Task<List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<GetRolePolicyResponse>>>> GetAllInlinePoliciesAsync(List<Tuple<Tuple<Role, List<AttachedPolicyType>>, List<string>>> rolesDataCollection)
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

        private async Task<List<string>> GetAllInlinePoliciesNamesAsync(List<Role> rolesDataCollection)
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

        private async Task<List<Role>> GetRolesFromSourceAsync(string nameFilter)
        {
            var roles = await GetRolesAsync(new ResourceNameContainsTermInsensitiveCaseFilter(nameFilter), sourceClient);

            return roles.Where(role => role.Tags == null || role.Tags.Count() < 1).ToList();
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

            var items = collection.Where(item => item.Id.PreviousName.StartsWith("AWSLambdasMorpheus")).ToList();
            foreach (var item in items)
            {
                foreach(var policy in item.AttachedPoliciesIds)
                {
                    var request = new AttachRolePolicyRequest()
                    {
                        PolicyArn = policies.FirstOrDefault(existingPolicy =>
                        {
                            var newName =  policy.PreviousName.ToLower().StartsWith("morpheus") ? NewNameFor(policy.PreviousName) : policy.PreviousName;                            
                            return newName == existingPolicy.PolicyName;
                        })?.Arn,
                        RoleName = item.Id.NewName
                    };

                    var response = await destinationClient.AttachRolePolicyAsync(request);
                }
            }

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

            foreach (var item in filteredCollection)
            {
                foreach (var policy in item.InlinePolicies)
                {
                    var request = new PutRolePolicyRequest()
                    {
                        PolicyDocument = policy.Document,
                        PolicyName = policy.NewName,
                        RoleName = item.Id.NewName
                    };

                    await destinationClient.PutRolePolicyAsync(request);
                }                
            }
        }

        private async Task<List<IamRole>> FilterAsync(List<IamRole> collection)
        {
            var existingCollection = await GetRolesFromDestinationAsync(Prefix());
            var newCollection = collection.ToList();
            foreach (var item in newCollection)
            {
                var arn = existingCollection.FirstOrDefault(existingItem => existingItem.RoleName == item.Id.NewName)?.Arn;

                if (!string.IsNullOrWhiteSpace(arn))
                    item.Id.NewArn = arn;
            }

            return newCollection.Where(item => string.IsNullOrWhiteSpace(item.Id.NewArn)).ToList();
        }
    }
}
