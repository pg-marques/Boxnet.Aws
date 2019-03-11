using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boxnet.Aws.Mvp.Iam
{
    public class IamGroupsService : IamService
    {
        public IamGroupsService(Stack stack, string sourceAccessKey, string sourceSecretKey, string sourceRegion, string destinationAccessKey, string destinationSecretKey, string destinationRegion) : base(stack, sourceAccessKey, sourceSecretKey, sourceRegion, destinationAccessKey, destinationSecretKey, destinationRegion)
        {
        }

        public async Task CopyAllGroupsAsync(string filterName)
        { 
            var collectionData = await GetAllGroupsAsync(filterName);
            var collection = ConvertToGroups(collectionData);
            await CreateAllGroupsOnDestinationAsync(collection);

            stack.IamGroups = collection;
        }

        private async Task<List<Tuple<Tuple<Group, List<AttachedPolicyType>>, List<GetGroupPolicyResponse>>>> GetAllGroupsAsync(string nameFilter)
        {
            var collectionData = await GetGroupsFromSourceAsync(nameFilter);
            var collectionDataWithPolicies = await GetAllPoliciesAsync(collectionData);
            var collectionDataWithInlinePoliciesNames = await GetAllInlinePoliciesNamesAsync(collectionDataWithPolicies);
            return await GetAllInlinePoliciesAsync(collectionDataWithInlinePoliciesNames);
        }

        private List<IamGroup> ConvertToGroups(List<Tuple<Tuple<Group, List<AttachedPolicyType>>, List<GetGroupPolicyResponse>>> collection)
        {
            return collection.Select(item => new IamGroup()
            {
                Id = new ResourceIdWithArn()
                {
                    PreviousName = item.Item1.Item1.GroupName,
                    NewName = NewNameFor(item.Item1.Item1.GroupName),
                    PreviousArn = item.Item1.Item1.Arn,
                },
                Path = item.Item1.Item1.Path,
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

        private async Task<List<Tuple<Group, List<AttachedPolicyType>>>> GetAllPoliciesAsync(List<Group> collection)
        {
            var resultCollection = new List<Tuple<Group, List<AttachedPolicyType>>>();

            foreach (var itemData in collection)
            {
                var policies = new List<AttachedPolicyType>();
                string marker = null;
                do
                {
                    var request = new ListAttachedGroupPoliciesRequest()
                    {
                        Marker = marker,
                        GroupName = itemData.GroupName
                    };
                    var response = await sourceClient.ListAttachedGroupPoliciesAsync(request);

                    policies.AddRange(response.AttachedPolicies);

                    marker = response.Marker;

                } while (marker != null);

                resultCollection.Add(new Tuple<Group, List<AttachedPolicyType>>(itemData, policies));
            }

            return resultCollection;
        }

        private async Task<List<Tuple<Tuple<Group, List<AttachedPolicyType>>, List<string>>>> GetAllInlinePoliciesNamesAsync(List<Tuple<Group, List<AttachedPolicyType>>> collection)
        {
            var resultCollection = new List<Tuple<Tuple<Group, List<AttachedPolicyType>>, List<string>>>();

            foreach (var itemData in collection)
            {
                var policiesNames = new List<string>();
                string marker = null;
                do
                {
                    var request = new ListGroupPoliciesRequest()
                    {
                        Marker = marker,
                        GroupName = itemData.Item1.GroupName
                    };
                    var response = await sourceClient.ListGroupPoliciesAsync(request);

                    policiesNames.AddRange(response.PolicyNames);

                    marker = response.Marker;

                } while (marker != null);

                resultCollection.Add(new Tuple<Tuple<Group, List<AttachedPolicyType>>, List<string>>(itemData, policiesNames));
            }

            return resultCollection;
        }

        private async Task<List<Tuple<Tuple<Group, List<AttachedPolicyType>>, List<GetGroupPolicyResponse>>>> GetAllInlinePoliciesAsync(List<Tuple<Tuple<Group, List<AttachedPolicyType>>, List<string>>> collection)
        {
            var resultCollection = new List<Tuple<Tuple<Group, List<AttachedPolicyType>>, List<GetGroupPolicyResponse>>>();

            foreach (var itemData in collection)
            {
                var policies = new List<GetGroupPolicyResponse>();

                foreach (var policyName in itemData.Item2)
                {
                    var request = new GetGroupPolicyRequest()
                    {
                        GroupName = itemData.Item1.Item1.GroupName,
                        PolicyName = policyName
                    };
                    var response = await sourceClient.GetGroupPolicyAsync(request);

                    policies.Add(response);
                }
                resultCollection.Add(new Tuple<Tuple<Group, List<AttachedPolicyType>>, List<GetGroupPolicyResponse>>(itemData.Item1, policies));
            }

            return resultCollection;
        }

        private async Task<List<string>> GetAllInlinePoliciesNamesAsync(List<Group> collection)
        {
            var resultCollection = new List<string>();

            foreach (var item in collection)
            {
                string marker = null;
                do
                {
                    var request = new ListGroupPoliciesRequest()
                    {
                        Marker = marker,
                        GroupName = item.GroupName
                    };
                    var response = await sourceClient.ListGroupPoliciesAsync(request);

                    resultCollection.AddRange(response.PolicyNames);

                    marker = response.Marker;

                } while (marker != null);
            }

            return resultCollection;
        }

        private async Task<List<Group>> GetGroupsFromSourceAsync(string nameFilter)
        {
            var collection = await GetGroupsAsync(new ResourceNameContainsTermInsensitiveCaseFilter(nameFilter), sourceClient);
            var prefixFilter = new ResourceNamePrefixInsensitiveCaseFilter(Prefix());
            return collection.Where(item => !prefixFilter.IsValid(item.GroupName)).ToList();
        }

        private async Task<List<Group>> GetGroupsFromDestinationAsync(string nameFilter)
        {
            return await GetGroupsAsync(new ResourceNamePrefixInsensitiveCaseFilter(nameFilter), destinationClient);            
        }

        private async Task<List<Group>> GetGroupsAsync(IResourceNameFilter filter, AmazonIdentityManagementServiceClient client)
        {
            var collection = new List<Group>();

            string marker = null;
            do
            {
                var request = new ListGroupsRequest()
                {
                    Marker = marker
                };

                var response = await client.ListGroupsAsync(request);

                collection.AddRange(response.Groups.Where(item => filter.IsValid(item.GroupName)));

                marker = response.Marker;

            } while (marker != null);

            return collection;
        }

        private async Task CreateAllGroupsOnDestinationAsync(List<IamGroup> collection)
        {
            var filteredCollection = await FilterAsync(collection);

            foreach (var item in filteredCollection)
            {
                var request = new CreateGroupRequest()
                {
                    GroupName = item.Id.NewName,
                    Path = item.Path                    
                };

                var response = await destinationClient.CreateGroupAsync(request);
                item.Id.NewArn = response.Group.Arn;
            }

            foreach (var item in filteredCollection)
            {
                foreach (var policy in item.InlinePolicies)
                {
                    var request = new PutGroupPolicyRequest()
                    {
                        PolicyDocument = policy.Document,
                        PolicyName = policy.NewName,
                        GroupName = item.Id.NewName
                    };

                    await destinationClient.PutGroupPolicyAsync(request);
                }
            }
        }

        private async Task<List<IamGroup>> FilterAsync(List<IamGroup> collection)
        {
            var existingCollection = await GetGroupsFromDestinationAsync(Prefix());
            var newCollection = collection.ToList();
            foreach (var item in newCollection)
            {
                var arn = existingCollection.FirstOrDefault(existingItem => existingItem.GroupName == item.Id.NewName)?.Arn;

                if (!string.IsNullOrWhiteSpace(arn))
                    item.Id.NewArn = arn;
            }

            return newCollection.Where(item => string.IsNullOrWhiteSpace(item.Id.NewArn)).ToList();
        }
    }
}
