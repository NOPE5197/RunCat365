// Copyright 2020 Takuto Nakamura
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

using Microsoft.Win32;
using RunCat365.Properties;
using System.Diagnostics;
using FormsTimer = System.Windows.Forms.Timer;

namespace RunCat365
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Terminate RunCat365 if there's any existing instance.
            using var procMutex = new Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result) return;

            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                ApplicationConfiguration.Initialize();
                Application.SetColorMode(SystemColorMode.System);
                Application.Run(new RunCat365ApplicationContext());
            }
            finally
            {
                procMutex?.ReleaseMutex();
            }
        }
    }

    internal class RunCat365ApplicationContext : ApplicationContext
    {
        private const int FETCH_TIMER_DEFAULT_INTERVAL = 1000;
        private const int FETCH_COUNTER_SIZE = 5;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly CPURepository cpuRepository;
        private readonly MemoryRepository memoryRepository;
        private readonly StorageRepository storageRepository;
        private readonly LaunchAtStartupManager launchAtStartupManager;
        private readonly ContextMenuManager contextMenuManager;
        private readonly FormsTimer fetchTimer;
        private readonly FormsTimer animateTimer;
        private Runner runner = Runner.Cat;
        private Theme manualTheme = Theme.System;
        private FPSMaxLimit fpsMaxLimit = FPSMaxLimit.FPS40;
        private string selectedCPU = "_Total";
        private int fetchCounter = 5;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            _ = Enum.TryParse(UserSettings.Default.Runner, out runner);
            _ = Enum.TryParse(UserSettings.Default.Theme, out manualTheme);
            _ = Enum.TryParse(UserSettings.Default.FPSMaxLimit, out fpsMaxLimit);
            selectedCPU = UserSettings.Default.SelectedCPU ?? "_Total";

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuRepository = new CPURepository(selectedCPU);
            memoryRepository = new MemoryRepository();
            storageRepository = new StorageRepository();
            launchAtStartupManager = new LaunchAtStartupManager();

            contextMenuManager = new ContextMenuManager(
                () => runner,
                r => ChangeRunner(r),
                () => GetSystemTheme(),
                () => manualTheme,
                t => ChangeManualTheme(t),
                () => fpsMaxLimit,
                f => ChangeFPSMaxLimit(f),
                () => launchAtStartupManager.GetStartup(),
                s => launchAtStartupManager.SetStartup(s),
                () => selectedCPU,
                cpu => ChangeSelectedCPU(cpu),
                () => OpenRepository(),
                () => Application.Exit()
            );

            animateTimer = new FormsTimer
            {
                Interval = ANIMATE_TIMER_DEFAULT_INTERVAL
            };
            animateTimer.Tick += new EventHandler(AnimationTick);
            animateTimer.Start();

            fetchTimer = new FormsTimer
            {
                Interval = FETCH_TIMER_DEFAULT_INTERVAL
            };
            fetchTimer.Tick += new EventHandler(FetchTick);
            fetchTimer.Start();

            ShowBalloonTip();
        }

        private static Theme GetSystemTheme()
        {
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return Theme.Light;
            var value = rKey.GetValue("SystemUsesLightTheme");
            if (value is null) return Theme.Light;
            return (int)value == 0 ? Theme.Dark : Theme.Light;
        }

        private void ShowBalloonTip()
        {
            if (UserSettings.Default.FirstLaunch)
            {
                contextMenuManager.ShowBalloonTip();
                UserSettings.Default.FirstLaunch = false;
                UserSettings.Default.Save();
            }
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                var systemTheme = GetSystemTheme();
                contextMenuManager.SetIcons(systemTheme, manualTheme, runner);
            }
        }

        private static void OpenRepository()
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "https://github.com/Kyome22/RunCat365.git",
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        private void ChangeRunner(Runner r)
        {
            runner = r;
            UserSettings.Default.Runner = runner.ToString();
            UserSettings.Default.Save();
        }

        private void ChangeManualTheme(Theme t)
        {
            manualTheme = t;
            UserSettings.Default.Theme = manualTheme.ToString();
            UserSettings.Default.Save();
        }

        private void ChangeFPSMaxLimit(FPSMaxLimit f)
        {
            fpsMaxLimit = f;
            UserSettings.Default.FPSMaxLimit = fpsMaxLimit.ToString();
            UserSettings.Default.Save();
        }

        private void ChangeSelectedCPU(string cpu)
        {
            selectedCPU = cpu;
            UserSettings.Default.SelectedCPU = selectedCPU;
            UserSettings.Default.Save();
            cpuRepository.ChangeCPUInstance(selectedCPU);
        }

        private void AnimationTick(object? sender, EventArgs e)
        {
            contextMenuManager.AdvanceFrame();
        }

        private void FetchSystemInfo(
            CPUInfo cpuInfo,
            MemoryInfo memoryInfo,
            List<StorageInfo> storageValue
        )
        {
            contextMenuManager.SetNotifyIconText(cpuInfo.GetDescription(selectedCPU));

            var systemInfoValues = new List<string>();
            systemInfoValues.AddRange(cpuInfo.GenerateIndicator());
            systemInfoValues.AddRange(memoryInfo.GenerateIndicator());
            systemInfoValues.AddRange(storageValue.GenerateIndicator());
            contextMenuManager.SetSystemInfoMenuText(string.Join("\n", [.. systemInfoValues]));
        }

        private int CalculateInterval(float cpuTotalValue)
        {
            // Range of interval: 25-500 (ms) = 2-40 (fps)

            float speed;
            int frameCount = runner.GetFrameNumber();

            // Apply special animation logic for runners with 30 or more frames
            if (frameCount >= 30)
            {
                if (cpuTotalValue <= 50.0f)
                {
                    // Scale speed from 0% to 100% as CPU goes from 0% to 50%
                    // This means at 50% CPU, speed is already at normal max.
                    speed = (float)Math.Max(1.0f, (cpuTotalValue / 50.0f * 5.0f) * fpsMaxLimit.GetRate());
                }
                else
                {
                    // Scale speed from 100% to 200% as CPU goes from 50% to 100%
                    // Speed starts at normal max (5.0f) and goes up to double (10.0f)
                    float cpuUsageAbove50 = cpuTotalValue - 50.0f;
                    float extraSpeed = (cpuUsageAbove50 / 50.0f) * 5.0f; // Additional speed up to 5.0f
                    speed = (float)Math.Max(1.0f, (5.0f + extraSpeed) * fpsMaxLimit.GetRate());
                }
            }
            else
            {
                // Original logic for other runners
                speed = (float)Math.Max(1.0f, (cpuTotalValue / 5.0f) * fpsMaxLimit.GetRate());
            }

            // The original logic seems to scale speed based on cpu/5. Let's adjust for that.
            // Original: speed = (cpu/5) * rate. So at 100% cpu, speed is 20 * rate.
            // Let's re-evaluate the logic based on the original formula to keep consistency.

            if (frameCount >= 30)
            {
                if (cpuTotalValue <= 50.0f)
                {
                    // At 50% CPU, we want speed equivalent to 100% CPU in the old formula.
                    // Old formula at 100% CPU: speed = (100 / 5) = 20.
                    // New formula: we need to map 0-50 CPU to a speed of 1-20.
                    // A simple mapping: speed = 1 + (cpuTotalValue / 50.0f) * 19
                    speed = 1.0f + (cpuTotalValue / 50.0f) * 19.0f;
                }
                else
                {
                    // At 100% CPU, we want double the speed (40).
                    // We need to map 50-100 CPU to a speed of 20-40.
                    float cpuUsageAbove50 = cpuTotalValue - 50.0f;
                    speed = 20.0f + (cpuUsageAbove50 / 50.0f) * 20.0f;
                }
                speed *= fpsMaxLimit.GetRate();
            }
            else
            {
                speed = (float)Math.Max(1.0f, (cpuTotalValue / 5.0f) * fpsMaxLimit.GetRate());
            }

            return (int)(500.0f / Math.Max(1.0f, speed));
        }

        private void FetchTick(object? state, EventArgs e)
        {
            cpuRepository.Update();
            fetchCounter += 1;
            if (fetchCounter < FETCH_COUNTER_SIZE) return;
            fetchCounter = 0;

            var cpuInfo = cpuRepository.Get();
            var memoryInfo = memoryRepository.Get();
            var storageInfo = storageRepository.Get();
            FetchSystemInfo(cpuInfo, memoryInfo, storageInfo);

            animateTimer.Stop();
            animateTimer.Interval = CalculateInterval(cpuInfo.Total);
            animateTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;

                animateTimer?.Stop();
                animateTimer?.Dispose();
                fetchTimer?.Stop();
                fetchTimer?.Dispose();

                cpuRepository?.Close();

                contextMenuManager?.HideNotifyIcon();
                contextMenuManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
