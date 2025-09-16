// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Diagnostics;

namespace RunCat365
{
    public struct NetworkInfo
    {
        public string Name { get; set; }
        public float UploadSpeed { get; set; }
        public float DownloadSpeed { get; set; }
    }

    internal static class NetworkInfoExtension
    {
        internal static List<string> GenerateIndicator(this NetworkInfo networkInfo, NetworkSpeedUnit unit)
        {
            var resultLines = new List<string>
            {
                "Network:",
                $"   ├─ Sent: {networkInfo.UploadSpeed.ToNetworkSpeedFormatted(unit)}",
                $"   └─ Received: {networkInfo.DownloadSpeed.ToNetworkSpeedFormatted(unit)}"
            };
            return resultLines;
        }
    }

    internal class NetworkRepository
    {
        private PerformanceCounter? uploadCounter;
        private PerformanceCounter? downloadCounter;
        private string[]? instances;
        private string? instance;

        public NetworkRepository()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                instances = category.GetInstanceNames();
                if (instances.Length > 0)
                {
                    instance = instances.FirstOrDefault(i => i.Contains("Realtek", StringComparison.OrdinalIgnoreCase)) ?? instances[0];
                    uploadCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
                    downloadCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        public NetworkInfo GetNetworkInfo()
        {
            if (uploadCounter == null || downloadCounter == null || instance == null)
            {
                return new NetworkInfo();
            }

            return new NetworkInfo
            {
                Name = instance,
                UploadSpeed = uploadCounter.NextValue(),
                DownloadSpeed = downloadCounter.NextValue()
            };
        }

        public string[] GetInterfaces()
        {
            return instances ?? [];
        }

        public void SetInterface(string newInstance)
        {
            if (instance == newInstance) return;

            uploadCounter?.Dispose();
            downloadCounter?.Dispose();
            
            instance = newInstance;
            uploadCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
            downloadCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
        }
    }
}
