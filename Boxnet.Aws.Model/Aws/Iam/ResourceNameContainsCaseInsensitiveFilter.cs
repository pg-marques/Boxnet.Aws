namespace Boxnet.Aws.Model.Aws.Iam
{
    public class ResourceNameContainsCaseInsensitiveFilter : IResourceIdFilter
    {
        private readonly string term;

        public ResourceNameContainsCaseInsensitiveFilter(string term)
        {
            this.term = term;
        }

        public bool IsSatisfiedBy(IResourceId resourceId)
        {
            return resourceId.Name.ToLower().Contains(term.ToLower());
        }
    }
}
