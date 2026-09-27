#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Net.Http;
using System.Text;
using ArcWeb3;
using System.Threading.Tasks;

namespace ArcWeb3.EditorTools
{
    [CustomEditor(typeof(ArcWeb3Config))]
    public class ArcWeb3ConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ArcWeb3Config config = (ArcWeb3Config)target;

            GUILayout.Space(15);
            if (GUILayout.Button("Validate RPC URL", GUILayout.Height(30)))
            {
                ValidateRPC(config.rpcUrl);
            }
        }

        private async void ValidateRPC(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("[ArcWeb3] RPC URL is empty.");
                return;
            }

            Debug.Log($"[ArcWeb3] Pinging {url}...");
            using (HttpClient client = new HttpClient())
            {
                string payload = "{\"jsonrpc\":\"2.0\",\"method\":\"eth_blockNumber\",\"params\":[],\"id\":1}";
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode && result.Contains("result"))
                    {
                        Debug.Log($"<color=green>[ArcWeb3] Success! RPC is online. Node Response: {result}</color>");
                    }
                    else
                    {
                        Debug.LogError($"[ArcWeb3] RPC responded but might be invalid: {result}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ArcWeb3] RPC Validation Failed: {e.Message}");
                }
            }
        }
    }
}
#endif