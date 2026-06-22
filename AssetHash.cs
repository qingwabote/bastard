using UnityEngine;

namespace Bastard
{
    public struct AssetHash
    {
        public static void Update(ref Unity.Collections.xxHash3.StreamingState hash, Object asset)
        {
            Debug.Assert(UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId));

            var guidBytes = System.Text.Encoding.UTF8.GetBytes(guid);

            hash.Update(guidBytes.Length);
            for (int j = 0; j < guidBytes.Length; ++j)
                hash.Update(guidBytes[j]);

            hash.Update(localId);
        }
    }
}