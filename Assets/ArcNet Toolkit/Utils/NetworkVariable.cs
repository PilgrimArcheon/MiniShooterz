using System;
using System.Collections.Generic;

namespace ArcNet
{
    /// <summary>
    /// A wrapper for variables that need to be synced over the network.
    /// Handles dirty flags and change events.
    /// </summary>
    public class NetworkVariable<T>
    {
        public delegate void OnValueChangedDelegate(T previousValue, T newValue);
        public event OnValueChangedDelegate OnValueChanged;

        private T _value;
        private string _name; // Unique key for the stream (e.g., "hp")
        
        public NetworkVariable(string name, T initialValue = default)
        {
            _name = name;
            _value = initialValue;
        }

        /// <summary>
        /// Access or set the value. Setting triggers a sync on the next tick.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                // Equality check to avoid spamming events/network
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    T prev = _value;
                    _value = value;
                    OnValueChanged?.Invoke(prev, _value);
                }
            }
        }

        /// <summary>
        /// Call this inside your script's OnSerializeArcNetView
        /// </summary>
        public void Sync(ArcNetStream stream)
        {
            if (stream.IsWriting)
            {
                // Write
                stream.SendNext(_name, _value);
            }
            else
            {
                // Read
                object received = stream.ReceiveNext(_name);
                if (received != null)
                {
                    try 
                    {
                        // Convert based on type
                        T networkVal = (T)Convert.ChangeType(received, typeof(T));
                        
                        // Only trigger logic if remote value is different
                        if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(_value, networkVal))
                        {
                            T prev = _value;
                            _value = networkVal;
                            OnValueChanged?.Invoke(prev, _value);
                        }
                    }
                    catch 
                    {
                        // Handle parsing errors quietly
                    }
                }
            }
        }
    }
}