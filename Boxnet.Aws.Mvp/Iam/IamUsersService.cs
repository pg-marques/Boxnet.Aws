//using Amazon.IdentityManagement;
//using Amazon.IdentityManagement.Model;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Boxnet.Aws.Mvp.Iam
//{
//    public class IamUsersService : IamService
//    {
//        public IamUsersService(Stack stack, string sourceAccessKey, string sourceSecretKey, string sourceRegion, string destinationAccessKey, string destinationSecretKey, string destinationRegion) : base(stack, sourceAccessKey, sourceSecretKey, sourceRegion, destinationAccessKey, destinationSecretKey, destinationRegion)
//        {
//        }

//        public async Task CopyAllUsersAsync(string filterName)
//        {
//            var collectionData = await GetAllUsersAsync(filterName);
//            var collection = ConvertToUsers(collectionData);
//            //await CreateAllUsersOnDestinationAsync(collection);
            
//            stack.IamUsers = collection;
//        }

//        private async Task<List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>, List<ListGroupsForUserResponse>>>> GetAllUsersAsync(string nameFilter)
//        {
//            var collectionData = await GetUsersFromSourceAsync(nameFilter);
//            var collectionDataWithPolicies = await GetAllPoliciesAsync(collectionData);
//            var collectionDataWithInlinePoliciesNames = await GetAllInlinePoliciesNamesAsync(collectionDataWithPolicies);
//            var collectionDataWithInlinePolicies = await GetAllInlinePoliciesAsync(collectionDataWithInlinePoliciesNames);
//            return await GetAllUsersGroupsAsync(collectionDataWithInlinePolicies);
//        }

//        private List<IamUser> ConvertToUsers(List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>, List<ListGroupsForUserResponse>>> collection)
//        {
//            return collection.Select(item => new IamUser()
//            {
//                Id = new ResourceId()
//                {
//                    PreviousName = item.Item1.Item1.UserName,
//                    NewName = NewNameFor(item.Item1.Item1.UserName),
//                    PreviousArn = item.Item1.Item1.Arn,
//                },
//                Path = item.Item1.Item1.Path,
//                PermissionsBoundary = item.Item1.Item1.PermissionsBoundary,
//                AttachedPoliciesIds = item.Item1.Item2.Select(policy => new ResourceId()
//                {
//                    PreviousArn = policy.PolicyArn,
//                    PreviousName = policy.PolicyName,
//                    NewName = NewNameFor(policy.PolicyName)
//                }),
//                InlinePolicies = item.Item2.Select(policy => new IamInlinePolicy()
//                {
//                    Document = ExtracDocumentFrom(policy.PolicyDocument),
//                    PreviousName = policy.PolicyName,
//                    NewName = NewNameFor(policy.PolicyName)
//                }),
//                GroupsIds = item.Item3.SelectMany(response => response.Groups).Select(group => new ResourceId()
//                {
//                    PreviousArn = group.Arn,
//                    PreviousName = group.GroupName,
//                    NewName = NewNameFor(group.GroupName)
//                })
//            }).ToList();
//        }

//        private async Task<List<Tuple<User, List<AttachedPolicyType>>>> GetAllPoliciesAsync(List<User> collection)
//        {
//            var resultCollection = new List<Tuple<User, List<AttachedPolicyType>>>();

//            foreach (var itemData in collection)
//            {
//                var policies = new List<AttachedPolicyType>();
//                string marker = null;
//                do
//                {
//                    var request = new ListAttachedUserPoliciesRequest()
//                    {
//                        Marker = marker,
//                        UserName = itemData.UserName
//                    };
//                    var response = await sourceClient.ListAttachedUserPoliciesAsync(request);

//                    policies.AddRange(response.AttachedPolicies);

//                    marker = response.Marker;

//                } while (marker != null);

//                resultCollection.Add(new Tuple<User, List<AttachedPolicyType>>(itemData, policies));
//            }

//            return resultCollection;
//        }

//        private async Task<List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<string>>>> GetAllInlinePoliciesNamesAsync(List<Tuple<User, List<AttachedPolicyType>>> collection)
//        {
//            var resultCollection = new List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<string>>>();

//            foreach (var itemData in collection)
//            {
//                var policiesNames = new List<string>();
//                string marker = null;
//                do
//                {
//                    var request = new ListUserPoliciesRequest()
//                    {
//                        Marker = marker,
//                        UserName = itemData.Item1.UserName
//                    };
//                    var response = await sourceClient.ListUserPoliciesAsync(request);

//                    policiesNames.AddRange(response.PolicyNames);

//                    marker = response.Marker;

//                } while (marker != null);

//                resultCollection.Add(new Tuple<Tuple<User, List<AttachedPolicyType>>, List<string>>(itemData, policiesNames));
//            }

//            return resultCollection;
//        }

//        private async Task<List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>>>> GetAllInlinePoliciesAsync(List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<string>>> collection)
//        {
//            var resultCollection = new List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>>>();

//            foreach (var itemData in collection)
//            {
//                var policies = new List<GetUserPolicyResponse>();

//                foreach (var policyName in itemData.Item2)
//                {
//                    var request = new GetUserPolicyRequest()
//                    {
//                        UserName = itemData.Item1.Item1.UserName,
//                        PolicyName = policyName
//                    };
//                    var response = await sourceClient.GetUserPolicyAsync(request);

//                    policies.Add(response);
//                }
//                resultCollection.Add(new Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>>(itemData.Item1, policies));
//            }

//            return resultCollection;
//        }

//        private async Task<List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>, List<ListGroupsForUserResponse>>>> GetAllUsersGroupsAsync(List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>>> collection)
//        {
//            var resultCollection = new List<Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>, List<ListGroupsForUserResponse>>>();

//            foreach (var itemData in collection)
//            {
//                var policies = new List<ListGroupsForUserResponse>();

//                foreach (var policyName in itemData.Item2)
//                {
//                    string marker = null;
//                    do
//                    {
//                        var request = new ListGroupsForUserRequest()
//                        {
//                            UserName = itemData.Item1.Item1.UserName,
//                            Marker = marker                            
//                        };
//                        var response = await sourceClient.ListGroupsForUserAsync(request);

//                        policies.Add(response);
//                        marker = response.Marker;

//                    } while (marker != null);

//                }
//                resultCollection.Add(new Tuple<Tuple<User, List<AttachedPolicyType>>, List<GetUserPolicyResponse>, List<ListGroupsForUserResponse>>(itemData.Item1, itemData.Item2, policies));
//            }

//            return resultCollection;
//        }

//        private async Task<List<string>> GetAllInlinePoliciesNamesAsync(List<User> collection)
//        {
//            var resultCollection = new List<string>();

//            foreach (var item in collection)
//            {
//                string marker = null;
//                do
//                {
//                    var request = new ListUserPoliciesRequest()
//                    {
//                        Marker = marker,
//                        UserName = item.UserName
//                    };
//                    var response = await sourceClient.ListUserPoliciesAsync(request);

//                    resultCollection.AddRange(response.PolicyNames);

//                    marker = response.Marker;

//                } while (marker != null);
//            }

//            return resultCollection;
//        }

//        private async Task<List<User>> GetUsersFromSourceAsync(string nameFilter)
//        {
//            var collection = await GetUsersAsync(new ResourceNameContainsTermInsensitiveCaseFilter(nameFilter), sourceClient);
//            var prefixFilter = new ResourceNameStarsWithTermInsensitiveCaseFilter(Prefix());
//            return collection.Where(item => !prefixFilter.IsValid(item.UserName)).ToList();
//        }

//        private async Task<List<User>> GetUsersFromDestinationAsync(string nameFilter)
//        {
//            return await GetUsersAsync(new ResourceNameStarsWithTermInsensitiveCaseFilter(nameFilter), destinationClient);
//        }

//        private async Task<List<User>> GetUsersAsync(IResourceNameFilter filter, AmazonIdentityManagementServiceClient client)
//        {
//            var collection = new List<User>();

//            string marker = null;
//            do
//            {
//                var request = new ListUsersRequest()
//                {
//                    Marker = marker
//                };

//                var response = await client.ListUsersAsync(request);

//                collection.AddRange(response.Users.Where(item => filter.IsValid(item.UserName)));

//                marker = response.Marker;

//            } while (marker != null);

//            marker = null;
//            do
//            {
//                var request = new ListEntitiesForPolicyRequest()
//                {
//                    Marker = marker,
                    
//                };

//                var response = await client.ListEntitiesForPolicyAsync(request);

//                collection.AddRange(response.Users.Where(item => filter.IsValid(item.UserName)));

//                marker = response.Marker;

//            } while (marker != null);
             

//            return collection;
//        }

//        private async Task CreateAllUsersOnDestinationAsync(List<IamUser> collection)
//        {
//            var filteredCollection = await FilterAsync(collection);

//            foreach (var item in collection)
//            {
//                var request = new CreateUserRequest()
//                {
//                    UserName = item.Id.NewName,
//                    Path = item.Path,
//                    PermissionsBoundary = item.PermissionsBoundary?.PermissionsBoundaryArn,
//                    Tags = tags
//                };

//                var response = await destinationClient.CreateUserAsync(request);
//                item.Id.NewArn = response.User.Arn;
//            }

//            foreach (var item in collection)
//            {
//                foreach (var policy in item.InlinePolicies)
//                {
//                    var request = new PutUserPolicyRequest()
//                    {
//                        PolicyDocument = policy.Document,
//                        PolicyName = policy.NewName,
//                        UserName = item.Id.NewName
//                    };

//                    await destinationClient.PutUserPolicyAsync(request);
//                }
//            }
//        }

//        private async Task<List<IamUser>> FilterAsync(List<IamUser> collection)
//        {
//            var existingCollection = await GetUsersFromDestinationAsync(Prefix());
//            var newCollection = collection.ToList();
//            foreach (var item in newCollection)
//            {
//                var arn = existingCollection.FirstOrDefault(existingItem => existingItem.UserName == item.Id.NewName)?.Arn;

//                if (!string.IsNullOrWhiteSpace(arn))
//                    item.Id.NewArn = arn;
//            }

//            return newCollection.Where(item => string.IsNullOrWhiteSpace(item.Id.NewArn)).ToList();
//        }
//    }
//}
