﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Boxnet.Aws.Mvp.Iam
{
    public class IamGroup
    {
        public ResourceIdWithArn Id { get; set; }
        public string Path { get; set; }
        public IEnumerable<ResourceIdWithArn> AttachedPoliciesIds { get; set; }
        public IEnumerable<IamInlinePolicy> InlinePolicies { get; set; }
    }
}
