using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ArcNet
{
    public interface IArcNetObservable
    {
        /// <summary>
        /// Called to write or read data for network synchronization.
        /// </summary>
        void OnSerializeArcNetView(ArcNetStream stream, ArcNetMessageInfo info);
    }

    public struct ArcNetMessageInfo
    {
        public float timestamp;
        public int senderActorNr; // Changed from string to int
    }

    /// <summary>
    /// Helper class to read/write data to a dictionary.
    /// </summary>
    public class ArcNetStream
    {
        public bool IsWriting { get; private set; }
        private Dictionary<string, object> _data;

        public ArcNetStream(bool isWriting, Dictionary<string, object> incomingData = null)
        {
            IsWriting = isWriting;
            _data = incomingData ?? new Dictionary<string, object>();
        }

        // WRITE
        public void SendNext(string key, object value)
        {
            if (!IsWriting) return;

            // Simplify Vectors/Quaternions to simple arrays/objects for safer JSON
            if (value is Vector3 v)
            {
                _data[key] = new float[] { v.x, v.y, v.z };
            }
            else if (value is Quaternion q)
            {
                _data[key] = new float[] { q.x, q.y, q.z, q.w };
            }
            else
            {
                _data[key] = value;
            }
        }

        // READ
        public object ReceiveNext(string key)
        {
            if (IsWriting) return null;
            if (_data.TryGetValue(key, out object val)) return val;
            return null;
        }

        // --- TYPE SAFE HELPERS ---

        public Vector3 ReceiveVector3(string key)
        {
            object val = ReceiveNext(key);
            if (val == null) return Vector3.zero;

            // Handle JArray (Newtonsoft) or float[]
            if (val is JArray arr)
            {
                return new Vector3((float)arr[0], (float)arr[1], (float)arr[2]);
            }
            if (val is float[] fArr)
            {
                return new Vector3(fArr[0], fArr[1], fArr[2]);
            }

            return Vector3.zero;
        }

        public Quaternion ReceiveQuaternion(string key)
        {
            object val = ReceiveNext(key);
            if (val == null) return Quaternion.identity;

            if (val is JArray arr)
            {
                return new Quaternion((float)arr[0], (float)arr[1], (float)arr[2], (float)arr[3]);
            }
            if (val is float[] fArr)
            {
                return new Quaternion(fArr[0], fArr[1], fArr[2], fArr[3]);
            }

            return Quaternion.identity;
        }

        public Dictionary<string, object> GetData() => _data;
    }

    #region External Types

    /// <summary>
    /// Attribute used to mark methods that can be called over the network.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ArcRPCAttribute : Attribute { }

    /// <summary>
    /// Targets for RPC delivery.
    /// </summary>
    public enum RpcTarget
    {
        All,
        Others,
        MasterClient,
        AllBuffered,  
        OthersBuffered
    }

    #endregion
}