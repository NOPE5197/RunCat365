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
    struct CPUInfo
    {
        internal float Total { get; set; }
        internal float User { get; set; }
        internal float Kernel { get; set; }
        internal float Idle { get; set; }
    }

    internal static class CPUInfoExtension
    {
        internal static string GetDescription(this CPUInfo cpuInfo, string cpuInstance = "_Total")
        {
            var displayName = cpuInstance == "_Total" ? "CPU" : $"CPU {cpuInstance}";
            return $"{displayName}: {cpuInfo.Total:f1}%";
        }

        internal static List<string> GenerateIndicator(this CPUInfo cpuInfo)
        {
            var resultLines = new List<string>
            {
                $"CPU: {cpuInfo.Total:f1}%",
                $"   ├─ User: {cpuInfo.User:f1}%",
                $"   ├─ Kernel: {cpuInfo.Kernel:f1}%",
                $"   └─ Available: {cpuInfo.Idle:f1}%"
            };
            return resultLines;
        }
    }

    internal class CPURepository
    {
        private PerformanceCounter? totalCounter;
        private PerformanceCounter? userCounter;
        private PerformanceCounter? kernelCounter;
        private PerformanceCounter? idleCounter;
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;
        private string selectedCPU = "_Total";

        internal CPURepository()
        {
            InitializeCounters(selectedCPU);
        }

        internal CPURepository(string cpuInstance)
        {
            selectedCPU = cpuInstance;
            InitializeCounters(selectedCPU);
        }

        private void InitializeCounters(string cpuInstance)
        {
            try
            {
                totalCounter = new PerformanceCounter("Processor", "% Processor Time", cpuInstance);
                userCounter = new PerformanceCounter("Processor", "% User Time", cpuInstance);
                kernelCounter = new PerformanceCounter("Processor", "% Privileged Time", cpuInstance);
                idleCounter = new PerformanceCounter("Processor", "% Idle Time", cpuInstance);

                // Discards first return value
                _ = totalCounter.NextValue();
                _ = userCounter.NextValue();
                _ = kernelCounter.NextValue();
                _ = idleCounter.NextValue();
            }
            catch (Exception)
            {
                // Fallback to _Total if specific CPU instance is not available
                if (cpuInstance != "_Total")
                {
                    InitializeCounters("_Total");
                    selectedCPU = "_Total";
                }
                else
                {
                    throw;
                }
            }
        }

        internal void ChangeCPUInstance(string cpuInstance)
        {
            if (cpuInstance == selectedCPU) return;

            Close();
            selectedCPU = cpuInstance;
            InitializeCounters(selectedCPU);
        }

        internal static List<string> GetAvailableCPUInstances()
        {
            var instances = new List<string> { "_Total" };
            try
            {
                var category = new PerformanceCounterCategory("Processor");
                var instanceNames = category.GetInstanceNames();
                var cpuInstances = instanceNames
                    .Where(i => i != "_Total")
                    .OrderBy(i =>
                    {
                        if (int.TryParse(i, out int num))
                        {
                            return num;
                        }
                        return int.MaxValue;
                    })
                    .ToList();
                instances.AddRange(cpuInstances);
            }
            catch (Exception)
            {
                // If we can't get instances, just return _Total
            }
            return instances;
        }

        internal void Update()
        {
            try
            {
                if (totalCounter == null || userCounter == null || kernelCounter == null || idleCounter == null)
                {
                    return;
                }

                // Range of value: 0-100 (%)
                var idle = Math.Min(100, Math.Max(0, idleCounter.NextValue()));
                var total = 100 - idle;
                var user = Math.Min(100, Math.Max(0, userCounter.NextValue()));
                var kernel = Math.Min(100, Math.Max(0, kernelCounter.NextValue()));

                var cpuInfo = new CPUInfo
                {
                    Total = total,
                    User = user,
                    Kernel = kernel,
                    Idle = idle,
                };

                cpuInfoList.Add(cpuInfo);
                if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
                {
                    cpuInfoList.RemoveAt(0);
                }
            }
            catch (Exception)
            {
                // If there's an error reading CPU data, add a zero entry
                var cpuInfo = new CPUInfo
                {
                    Total = 0,
                    User = 0,
                    Kernel = 0,
                    Idle = 100,
                };

                cpuInfoList.Add(cpuInfo);
                if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
                {
                    cpuInfoList.RemoveAt(0);
                }
            }
        }

        internal CPUInfo Get()
        {
            if (cpuInfoList.Count == 0) return new CPUInfo();

            // Use the most recent reading for better accuracy
            // Individual CPU cores can show more accurate real-time usage
            var latest = cpuInfoList.LastOrDefault();
            
            return new CPUInfo
            {
                Total = latest.Total,
                User = latest.User,
                Kernel = latest.Kernel,
                Idle = latest.Idle
            };
        }

        internal void Close()
        {
            totalCounter?.Close();
            userCounter?.Close();
            kernelCounter?.Close();
            idleCounter?.Close();
        }
    }
}
