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

using RunCat365.Properties;
using System.ComponentModel;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> icons = [];
        private readonly object iconLock = new();
        private int current = 0;
        private EndlessGameForm? endlessGameForm;

        internal ContextMenuManager(
            Func<Runner> getRunner,
            Action<Runner> setRunner,
            Func<Theme> getSystemTheme,
            Func<Theme> getManualTheme,
            Action<Theme> setManualTheme,
            Func<FPSMaxLimit> getFPSMaxLimit,
            Action<FPSMaxLimit> setFPSMaxLimit,
            Func<bool> getLaunchAtStartup,
            Func<bool, bool> toggleLaunchAtStartup,
            Func<string> getSelectedCPU,
            Action<string> setSelectedCPU,
            Func<UpdateInterval> getUpdateInterval,
            Action<UpdateInterval> setUpdateInterval,
            Func<AnimationThreshold> getAnimationThreshold,
            Action<AnimationThreshold> setAnimationThreshold,
            Func<AnimationMultiplier> getAnimationMultiplier,
            Action<AnimationMultiplier> setAnimationMultiplier,
            Func<bool> getShowNetworkSpeed,
            Action<bool> setShowNetworkSpeed,
            Func<NetworkSpeedUnit> getNetworkSpeedUnit,
            Action<NetworkSpeedUnit> setNetworkSpeedUnit,
            Func<string?> getSelectedNetworkInterface,
            Action<string> setSelectedNetworkInterface,
            Func<string[]> getAvailableNetworkInterfaces,
            Action openRepository,
            Action onExit
        )
        {
            systemInfoMenu.Text = "-\n-\n-\n-\n-";
            systemInfoMenu.Enabled = false;

            var runnersMenu = new CustomToolStripMenuItem("Runners");
            runnersMenu.SetupSubMenusFromEnum<Runner>(
                r => r.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Runner>(
                        parent,
                        sender,
                        (string? s, out Runner r) => Enum.TryParse(s, out r),
                        r => setRunner(r)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                r => getRunner() == r,
                r => GetRunnerThumbnailBitmap(getSystemTheme(), r)
            );

            var themeMenu = new CustomToolStripMenuItem("Theme");
            themeMenu.SetupSubMenusFromEnum<Theme>(
                t => t.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Theme>(
                        parent,
                        sender,
                        (string? s, out Theme t) => Enum.TryParse(s, out t),
                        t => setManualTheme(t)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                t => getManualTheme() == t,
                _ => null
            );

            var updateIntervalMenu = new CustomToolStripMenuItem("Update Interval");
            updateIntervalMenu.SetupSubMenusFromEnum<UpdateInterval>(
                i => i.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<UpdateInterval>(
                        parent,
                        sender,
                        (string? s, out UpdateInterval i) => UpdateIntervalExtensions.TryParse(s, out i),
                        i => setUpdateInterval(i)
                    );
                },
                i => getUpdateInterval() == i,
                _ => null
            );

            var fpsMaxLimitMenu = new CustomToolStripMenuItem("FPS Max Limit");
            fpsMaxLimitMenu.SetupSubMenusFromEnum<FPSMaxLimit>(
                f => f.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<FPSMaxLimit>(
                        parent,
                        sender,
                        (string? s, out FPSMaxLimit f) => FPSMaxLimitExtension.TryParse(s, out f),
                        f => setFPSMaxLimit(f)
                    );
                },
                f => getFPSMaxLimit() == f,
                _ => null
            );

            var launchAtStartupMenu = new CustomToolStripMenuItem("Launch at startup")
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);

            var animationThresholdMenu = new CustomToolStripMenuItem("Animation Threshold");
            animationThresholdMenu.SetupSubMenusFromEnum<AnimationThreshold>(
                t => t.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<AnimationThreshold>(
                        parent,
                        sender,
                        (string? s, out AnimationThreshold t) => AnimationThresholdExtensions.TryParse(s, out t),
                        t => setAnimationThreshold(t)
                    );
                },
                t => getAnimationThreshold() == t,
                _ => null
            );

            var animationMultiplierMenu = new CustomToolStripMenuItem("Animation Multiplier");
            animationMultiplierMenu.SetupSubMenusFromEnum<AnimationMultiplier>(
                m => m.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<AnimationMultiplier>(
                        parent,
                        sender,
                        (string? s, out AnimationMultiplier m) => AnimationMultiplierExtensions.TryParse(s, out m),
                        m => setAnimationMultiplier(m)
                    );
                },
                m => getAnimationMultiplier() == m,
                _ => null
            );

            // CPU Selection Menu
            var cpuMenu = new CustomToolStripMenuItem("CPU Selection");
            var availableCPUs = CPURepository.GetAvailableCPUInstances();
            foreach (var cpu in availableCPUs)
            {
                var displayName = cpu == "_Total" ? "Total CPU" : $"CPU{cpu}";
                var cpuMenuItem = new CustomToolStripMenuItem(displayName)
                {
                    Checked = getSelectedCPU() == cpu,
                    Tag = cpu // Store the actual CPU instance name in Tag
                };
                cpuMenuItem.Click += (sender, e) =>
                {
                    if (sender is ToolStripMenuItem item && item.Tag is string actualCPU)
                    {
                        HandleMenuItemSelection<string>(
                            cpuMenu,
                            sender,
                            (string? s, out string result) => { result = actualCPU; return true; },
                            cpu => setSelectedCPU(cpu)
                        );
                    }
                };
                cpuMenu.DropDownItems.Add(cpuMenuItem);
            }

            var settingsMenu = new CustomToolStripMenuItem("Settings");
            settingsMenu.DropDownItems.AddRange(
                themeMenu,
                updateIntervalMenu,
                fpsMaxLimitMenu,
                cpuMenu
            );

            var networkMenu = new CustomToolStripMenuItem("Network");
            var showNetworkSpeedMenu = new CustomToolStripMenuItem("Show on Tray Icon")
            {
                Checked = getShowNetworkSpeed()
            };
            showNetworkSpeedMenu.Click += (sender, e) =>
            {
                if (sender is ToolStripMenuItem item)
                {
                    item.Checked = !item.Checked;
                    setShowNetworkSpeed(item.Checked);
                }
            };

            var networkSpeedUnitMenu = new CustomToolStripMenuItem("Speed Unit");
            networkSpeedUnitMenu.SetupSubMenusFromEnum<NetworkSpeedUnit>(
                u => u.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<NetworkSpeedUnit>(
                        parent,
                        sender,
                        (string? s, out NetworkSpeedUnit u) => Enum.TryParse(s, true, out u),
                        u => setNetworkSpeedUnit(u)
                    );
                },
                u => getNetworkSpeedUnit() == u,
                _ => null
            );

            var networkInterfaceMenu = new CustomToolStripMenuItem("Network Interface");
            var availableInterfaces = getAvailableNetworkInterfaces();
            foreach (var networkInterface in availableInterfaces)
            {
                var interfaceMenuItem = new CustomToolStripMenuItem(networkInterface)
                {
                    Checked = getSelectedNetworkInterface() == networkInterface,
                    Tag = networkInterface
                };
                interfaceMenuItem.Click += (sender, e) =>
                {
                    if (sender is ToolStripMenuItem item && item.Tag is string actualInterface)
                    {
                        HandleMenuItemSelection<string>(
                            networkInterfaceMenu,
                            sender,
                            (string? s, out string result) => { result = actualInterface; return true; },
                            i => setSelectedNetworkInterface(i)
                        );
                    }
                };
                networkInterfaceMenu.DropDownItems.Add(interfaceMenuItem);
            }
            
            networkMenu.DropDownItems.AddRange(
                showNetworkSpeedMenu,
                networkSpeedUnitMenu,
                networkInterfaceMenu
            );

            settingsMenu.DropDownItems.Add(networkMenu);
            settingsMenu.DropDownItems.Add(launchAtStartupMenu);
            settingsMenu.DropDownItems.Add(new ToolStripSeparator());
            settingsMenu.DropDownItems.AddRange(
                animationThresholdMenu,
                animationMultiplierMenu
            );

            var endlessGameMenu = new CustomToolStripMenuItem("Endless Game");
            endlessGameMenu.Click += (sender, e) => ShowOrActivateGameWindow(getSystemTheme);

            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var repositoryMenu = new CustomToolStripMenuItem("Open Repository");
            repositoryMenu.Click += (sender, e) => openRepository();

            var informationMenu = new CustomToolStripMenuItem("Information");
            informationMenu.DropDownItems.AddRange(
                appVersionMenu,
                repositoryMenu
            );

            var exitMenu = new CustomToolStripMenuItem("Exit");
            exitMenu.Click += (sender, e) => onExit();

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                systemInfoMenu,
                new ToolStripSeparator(),
                runnersMenu,
                new ToolStripSeparator(),
                settingsMenu,
                informationMenu,
                endlessGameMenu,
                new ToolStripSeparator(),
                exitMenu
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetIcons(getSystemTheme(), getManualTheme(), getRunner());

            notifyIcon.Text = "-";
            notifyIcon.Icon = icons[0];
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private static void HandleMenuItemSelection<T>(
            ToolStripMenuItem parentMenu,
            object? sender,
            CustomTryParseDelegate<T> tryParseMethod,
            Action<T> assignValueAction
        )
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem childItem in parentMenu.DropDownItems)
            {
                childItem.Checked = false;
            }
            item.Checked = true;
            if (tryParseMethod(item.Text, out T parsedValue))
            {
                assignValueAction(parsedValue);
            }
        }

        private static Bitmap? GetRunnerThumbnailBitmap(Theme systemTheme, Runner runner)
        {
            var runnerName = runner.GetString().ToLower();
            var iconName = $"{systemTheme.GetString().ToLower()}_{runnerName}_0";
            var obj = Resources.ResourceManager.GetObject(iconName);

            // Fallback to light theme if dark icon is not found for the thumbnail
            if (obj is null && systemTheme == Theme.Dark)
            {
                var fallbackIconName = $"light_{runnerName}_0";
                obj = Resources.ResourceManager.GetObject(fallbackIconName);
            }

            return obj is Icon icon ? icon.ToBitmap() : null;
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var theme = manualTheme == Theme.System ? systemTheme : manualTheme;
            var prefix = theme.GetString();
            var runnerName = runner.GetString();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var newList = new List<Icon>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{prefix}_{runnerName}_{i}".ToLower();
                var icon = rm.GetObject(iconName);

                // Fallback to light theme if dark icon is not found
                if (icon is null && theme == Theme.Dark)
                {
                    var fallbackIconName = $"light_{runnerName}_{i}".ToLower();
                    icon = rm.GetObject(fallbackIconName);
                }

                if (icon is not null)
                {
                    newList.Add((Icon)icon);
                }
            }

            // Only dispose old icons and switch to new ones if new icons were successfully loaded.
            if (newList.Count > 0)
            {
                lock (iconLock)
                {
                    icons.ForEach(icon => icon.Dispose());
                    icons.Clear();
                    icons.AddRange(newList);
                    current = 0;
                    // Immediately update the icon to a valid one from the new list
                    // to prevent the possibility of using a disposed icon.
                    if (notifyIcon is not null)
                    {
                        notifyIcon.Icon = icons[current];
                    }
                }
            }
        }

        private static void HandleStartupMenuClick(object? sender, Func<bool, bool> toggleLaunchAtStartup)
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            try
            {
                // The desired state is the opposite of the current state.
                if (toggleLaunchAtStartup(!item.Checked))
                {
                    item.Checked = !item.Checked;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void ShowOrActivateGameWindow(Func<Theme> getSystemTheme)
        {
            if (endlessGameForm is null)
            {
                endlessGameForm = new EndlessGameForm(getSystemTheme());
                endlessGameForm.FormClosed += (sender, e) =>
                {
                    endlessGameForm = null;
                };
                endlessGameForm.Show();
            }
            else
            {
                endlessGameForm.Activate();
            }
        }

        internal void ShowBalloonTip()
        {
            var message = "App has launched. " +
                "If the icon is not on the taskbar, it has been omitted, " +
                "so please move it manually and pin it.";
            notifyIcon.ShowBalloonTip(5000, "RunCat 365", message, ToolTipIcon.Info);
        }

        internal void AdvanceFrame()
        {
            lock (iconLock)
            {
                if (icons.Count == 0) return;
                if (icons.Count <= current) current = 0;
                notifyIcon.Icon = icons[current];
                current = (current + 1) % icons.Count;
            }
        }

        internal void SetSystemInfoMenuText(string text)
        {
            systemInfoMenu.Text = text;
        }

        internal void SetNotifyIconText(string text)
        {
            notifyIcon.Text = text;
        }

        internal void HideNotifyIcon()
        {
            notifyIcon.Visible = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (iconLock)
                {
                    icons.ForEach(icon => icon.Dispose());
                    icons.Clear();
                }

                if (notifyIcon is not null)
                {
                    notifyIcon.ContextMenuStrip?.Dispose();
                    notifyIcon.Dispose();
                }

                endlessGameForm?.Dispose();
            }
        }

        private delegate bool CustomTryParseDelegate<T>(string? value, out T result);
    }
}
