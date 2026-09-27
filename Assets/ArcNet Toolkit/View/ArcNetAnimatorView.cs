using UnityEngine;
using System.Collections.Generic;

namespace ArcNet
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(ArcNetView))] 
    [AddComponentMenu("ArcNet/ArcNet Animator View")]
    public class ArcNetAnimatorView : ArcNetBehaviour, IArcNetObservable
    {
        [System.Serializable]
        public class SyncedParam
        {
            public string name;
            public AnimatorControllerParameterType type;
            [HideInInspector] public int hash;
        }

        [Header("Configuration")]
        public List<SyncedParam> parameters = new List<SyncedParam>();
        
        [Tooltip("If true, layer weights are synced.")]
        public bool syncLayerWeights = true;
        
        [Tooltip("Smoothing speed for float parameters.")]
        public float interpolationSpeed = 10f;

        private Animator _anim;
        
        // Caching current values to detect changes
        private Dictionary<int, float> _floatCache = new Dictionary<int, float>();
        private Dictionary<int, int> _intCache = new Dictionary<int, int>();
        private Dictionary<int, bool> _boolCache = new Dictionary<int, bool>();

        private void Awake()
        {
            _anim = GetComponent<Animator>();
            foreach (var p in parameters)
            {
                p.hash = Animator.StringToHash(p.name);
            }
        }

        public void OnSerializeArcNetView(ArcNetStream stream, ArcNetMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // OWNER: Write Data
                foreach (var p in parameters)
                {
                    switch (p.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            float fVal = _anim.GetFloat(p.hash);
                            stream.SendNext(p.name, fVal);
                            break;
                            
                        case AnimatorControllerParameterType.Int:
                            int iVal = _anim.GetInteger(p.hash);
                            stream.SendNext(p.name, iVal);
                            break;

                        case AnimatorControllerParameterType.Bool:
                            bool bVal = _anim.GetBool(p.hash);
                            stream.SendNext(p.name, bVal);
                            break;

                        case AnimatorControllerParameterType.Trigger:
                            // Triggers are transient; we usually RPC them, 
                            // but if synced here, we check state.
                            if (_anim.GetBool(p.hash))
                            {
                                stream.SendNext(p.name, true);
                                // Reset strictly local immediately to avoid sending true twice
                                _anim.ResetTrigger(p.hash); 
                            }
                            else
                            {
                                stream.SendNext(p.name, false);
                            }
                            break;
                    }
                }

                if (syncLayerWeights)
                {
                    for (int i = 0; i < _anim.layerCount; i++)
                    {
                        stream.SendNext($"lyr_{i}", _anim.GetLayerWeight(i));
                    }
                }
            }
            else
            {
                // REMOTE: Read Data
                foreach (var p in parameters)
                {
                    object val = stream.ReceiveNext(p.name);
                    if (val == null) continue;

                    switch (p.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            // We don't set immediately; we store in cache for interpolation in Update()
                            _floatCache[p.hash] = ConvertToFloat(val);
                            break;

                        case AnimatorControllerParameterType.Int:
                            _anim.SetInteger(p.hash, ConvertToInt(val));
                            break;

                        case AnimatorControllerParameterType.Bool:
                            _anim.SetBool(p.hash, ConvertToBool(val));
                            break;

                        case AnimatorControllerParameterType.Trigger:
                            if (ConvertToBool(val)) _anim.SetTrigger(p.hash);
                            break;
                    }
                }

                if (syncLayerWeights)
                {
                    for (int i = 0; i < _anim.layerCount; i++)
                    {
                        object wVal = stream.ReceiveNext($"lyr_{i}");
                        if (wVal != null) _anim.SetLayerWeight(i, ConvertToFloat(wVal));
                    }
                }
            }
        }

        private void Update()
        {
            if (!IsMine)
            {
                // Smoothly interpolate Floats
                foreach (var kvp in _floatCache)
                {
                    float current = _anim.GetFloat(kvp.Key);
                    float target = kvp.Value;
                    
                    if (Mathf.Abs(current - target) > 0.001f)
                    {
                        _anim.SetFloat(kvp.Key, Mathf.Lerp(current, target, Time.deltaTime * interpolationSpeed));
                    }
                }
            }
        }
        
        // Helpers
        private float ConvertToFloat(object o) => o != null ? System.Convert.ToSingle(o) : 0f;
        private int ConvertToInt(object o) => o != null ? System.Convert.ToInt32(o) : 0;
        private bool ConvertToBool(object o) => o != null ? System.Convert.ToBoolean(o) : false;
    }
}