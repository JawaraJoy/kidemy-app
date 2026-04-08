using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EduGame
{
    public class AssetLoader<T>
    {
        public static void Load(string assetAddress, Action<AsyncOperationHandle<T>> onLoadComplete)
        {
            AsyncOperationHandle<T> loadHandler = Addressables.LoadAssetAsync<T>(assetAddress);
            
            loadHandler.Completed += onLoadComplete;
        }
    }
}