namespace Boxnet.Aws.Mvp.Iam
{
    public class IamInlinePolicy
    {
        public string PreviousName { get; set; }
        public string NewName { get; set; }
        public string Document { get; set; }
        public bool Created { get; internal set; }
    }
}
