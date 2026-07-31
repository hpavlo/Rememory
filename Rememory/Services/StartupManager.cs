using Microsoft.Win32.TaskScheduler;
using Rememory.Helper;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using Windows.ApplicationModel;

namespace Rememory.Services
{
    public class StartupManager
    {
        #region Startup task

#if DEBUG
        private const string StartupTaskId = "RememoryDevStartupTask";
#else
        private const string StartupTaskId = "RememoryStartupTask";
#endif
        public StartupTask StartupTask { get; private set; }
        public bool IsStartupEnabled => StartupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        public bool IsDisabledByUser => StartupTask.State == StartupTaskState.DisabledByUser;

        public static async System.Threading.Tasks.Task<StartupManager> CreateAsync() => new() { StartupTask = await StartupTask.GetAsync(StartupTaskId) };

        #endregion

        private StartupManager() { }

        #region Elevated task

        private static readonly TaskService _taskSchedulerService = TaskService.Instance;
        private static readonly string ElevatedTaskName = $"Elevated run for {GetCurrentUserName()}";
        private static readonly string ElevatedTaskFolderName = Package.Current.Id.FamilyName;
        private static readonly string AppAliasExePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WindowsApps",
            Package.Current.Id.FamilyName,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.exe");

        public static void EnableElevatedStartup()
        {
            var task = _taskSchedulerService.NewTask();
            task.RegistrationInfo.Description = "Starts app with highest run level";
            task.Actions.Add(AppAliasExePath, arguments: "-silent");
            task.Settings.StopIfGoingOnBatteries = false;
            task.Settings.DisallowStartIfOnBatteries = false;
            task.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            // Delete if task already created
            DisableElevatedStartup();
            if (task.Validate() && AdministratorHelper.IsAppRunningAsAdministrator())
            {
                var createdTask = _taskSchedulerService.RootFolder.RegisterTaskDefinition($"{ElevatedTaskFolderName}\\{ElevatedTaskName}", task);
                createdTask.Definition.Principal.RunLevel = TaskRunLevel.Highest;
                createdTask.RegisterChanges();
            }
        }
        public static void DisableElevatedStartup()
        {
            try
            {
                _taskSchedulerService.RootFolder.DeleteTask($"{ElevatedTaskFolderName}\\{ElevatedTaskName}", false);
                _taskSchedulerService.RootFolder.DeleteFolder($"{ElevatedTaskFolderName}", false);
            }
            catch { }
        }

        public static bool IsElevatedTaskEnabled(out Task? elevatedTask)
        {
            elevatedTask = GetElevatedTask();
            return elevatedTask is not null && elevatedTask.Definition.Principal.RunLevel == TaskRunLevel.Highest;
        }

        private static Task? GetElevatedTask()
        {
            var folder = _taskSchedulerService.RootFolder.EnumerateFolders(folder => folder.Name.Equals(ElevatedTaskFolderName)).FirstOrDefault();
            return folder?.EnumerateTasks(task => task.Name.Equals(ElevatedTaskName)).FirstOrDefault();
        }

        private static string GetCurrentUserName()
        {
            var userId = WindowsIdentity.GetCurrent().Name;
            return userId.Contains('\\') ? userId.Split('\\')[1] : userId;
        }

        #endregion
    }

    [Obsolete("Used for startup migration to StartupTask")]
    public class LegacyTaskSchedulerManager
    {
        private static readonly TaskService _taskService = TaskService.Instance;

        private static readonly string TaskName = $"Autorun for {GetCurrentUserName()}";
        private static readonly string TaskFolderName = Package.Current.Id.FamilyName;

        public static void DeleteStartupTask()
        {
            try
            {
                _taskService.RootFolder.DeleteTask($"{TaskFolderName}\\{TaskName}", false);
                _taskService.RootFolder.DeleteFolder($"{TaskFolderName}", false);
            }
            catch { }
        }

        public static bool IsHighestRunLevelEnabled()
        {
            var task = GetStartupTask();
            return task is not null && task.Definition.Principal.RunLevel == TaskRunLevel.Highest;
        }

        private static Task? GetStartupTask()
        {
            var folder = _taskService.RootFolder.EnumerateFolders(folder => folder.Name.Equals(TaskFolderName)).FirstOrDefault();
            return folder?.EnumerateTasks(task => task.Name.Equals(TaskName)).FirstOrDefault();
        }

        private static string GetCurrentUserName()
        {
            var userId = WindowsIdentity.GetCurrent().Name;
            return userId.Contains('\\') ? userId.Split('\\')[1] : userId;
        }
    }
}
