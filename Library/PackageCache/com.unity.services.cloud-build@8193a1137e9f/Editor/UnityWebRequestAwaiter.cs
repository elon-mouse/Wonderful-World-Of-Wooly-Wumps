using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Services.CloudBuild.Editor
{
    internal struct UnityWebRequestAwaiter : INotifyCompletion
    {
        UnityWebRequestAsyncOperation asyncOp;
        Action continuation;

        internal UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
        {
            this.asyncOp = asyncOp;
            continuation = null;
        }

        internal bool IsCompleted => asyncOp.isDone;

        internal void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            this.continuation = continuation;
            asyncOp.completed += OnRequestCompleted;
        }

        void OnRequestCompleted(AsyncOperation obj)
        {
            continuation?.Invoke();
        }
    }

    internal static class ExtensionMethods
    {
        internal static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            return new UnityWebRequestAwaiter(asyncOp);
        }
    }
}
