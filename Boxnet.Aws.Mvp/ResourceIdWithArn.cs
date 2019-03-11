namespace Boxnet.Aws.Mvp
{
    public class ResourceIdWithArn : ResourceId
    {
        public string PreviousArn { get; set; }
        public string NewArn { get; set; }
    }
}
