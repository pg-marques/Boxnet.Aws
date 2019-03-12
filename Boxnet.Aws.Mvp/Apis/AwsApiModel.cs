namespace Boxnet.Aws.Mvp.Apis
{
    public class AwsApiModel
    {
        public ResourceIdWithAwsId Id { get; set; }
        public string ContentType { get; set; }
        public string Description { get; set; }
        public ResourceIdWithAwsId RestApiId { get; set; }
        public string Schema { get; set; }
    }
}
