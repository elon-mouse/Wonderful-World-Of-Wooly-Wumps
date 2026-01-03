using System;
using UnityEngine;

namespace Unity.Services.Core.Device
{
    class UnityEngineIdentifier : IUserIdentifierProvider
    {
        public string UserId
        {
            get
            {
#if ENABLE_UNITY_CLOUD_IDENTIFIERS
                return UnityEngine.Identifiers.Identifiers.installationId;
#else
                return null;
#endif
            }
            set
            {
            }
        }
    }
}
