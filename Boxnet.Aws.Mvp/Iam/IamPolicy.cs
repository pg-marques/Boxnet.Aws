namespace Boxnet.Aws.Mvp.Iam
{
    public class IamPolicy
    {
        public string PreviousName { get; set; }
        public string NewName { get; set; }

        public string PreviousArn { get; set; }
        public string NewArn { get; set; }

        public string Description { get; set; }
        public string Document { get; set; }
        public string Path { get; set; }
    }
}
