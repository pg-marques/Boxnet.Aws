using Boxnet.Aws.Model.Core;
using System;

namespace Boxnet.Aws.Infra.Core.Json
{
    public abstract class GuidEntityIdConverter<TEntityId> : IJObjectWrapperConverter<TEntityId>
        where TEntityId : GuidEntityId
    {
        private const string IdField = "id";
        private const string GuidField = "value";

        public TEntityId Convert(JObjectWrapper wrapper)
        {
            var tokenWrapper = wrapper[IdField];
            return CreateEntityId(new Guid(tokenWrapper[GuidField].AsStringOrEmpty()));
        }

        protected abstract TEntityId CreateEntityId(Guid value);
    }
}
