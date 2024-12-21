using Microsoft.Win32.TaskScheduler;
using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Windows.ApplicationModel;

namespace Rememory.Service
{
    public class TaskSchedulerStartupService : IStartupService
    {
        public bool IsStartupEnabled
        {
            get => _isStartupEnabled;
            set
            {
                if (_isStartupEnabled != value)
                {
                    _isStartupEnabled = value;
                    if (value)
                    {
                        CreateStartupTask();
                    }
                    else
                    {
                        DeleteStartupTask();
                    }
                }
            }
        }
        public bool IsStartupAsAdministratorEnabled
        {
            get => _isStartupAsAdministratorEnabled;
            set
            {
                if (_isStartupAsAdministratorEnabled != value)
                {
                    _isStartupAsAdministratorEnabled = value;
                    ChangeRunLevel(value ? TaskRunLevel.Highest : TaskRunLevel.LUA);
                }
            }
        }

        private TaskService _taskService = TaskService.Instance;
        private bool _isStartupEnabled;
        private bool _isStartupAsAdministratorEnabled;

        public TaskSchedulerStartupService()
        {
            _isStartupEnabled = IsStartupTaskEnabled();
            _isStartupAsAdministratorEnabled = IsHighestRunLevelEnabled();
        }

        private string TaskName => $"Autorun for {GetCurrentUserName()}";
        private string TaskFolderName => Package.Current.Id.FamilyName;
        private string AppAliasExePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "WindowsApps",
            Package.Current.Id.FamilyName,
            $"Rememory.exe");

        private void CreateStartupTask()
        {
            var task = _taskService.NewTask();
            task.RegistrationInfo.Description = "Autorun on startup";
            task.Triggers.Add(new LogonTrigger
            {
                Delay = TimeSpan.FromSeconds(3),
                UserId = WindowsIdentity.GetCurrent().Name
            });
            task.Actions.Add(AppAliasExePath, arguments: "-silent");
            task.Settings.StopIfGoingOnBatteries = false;
            task.Settings.DisallowStartIfOnBatteries = false;
            task.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            // Delete if task already created
            DeleteStartupTask();
            if (task.Validate())
            {
                _taskService.RootFolder.RegisterTaskDefinition($"{TaskFolderName}\\{TaskName}", task);
            }
        }

        private void DeleteStartupTask()
        {
            _taskService.RootFolder.DeleteTask($"{TaskFolderName}\\{TaskName}", false);
        }

        private void ChangeRunLevel(TaskRunLevel runLevel)
        {
            var task = GetStartupTask();
            if (task is not null)
            {
                task.Definition.Principal.RunLevel = runLevel;
                task.RegisterChanges();
            }
        }

        private bool IsStartupTaskEnabled()
        {
            var task = GetStartupTask();
            return task is not null && task.IsActive;
        }

        private bool IsHighestRunLevelEnabled()
        {
            var task = GetStartupTask();
            return task is not null && task.Definition.Principal.RunLevel == TaskRunLevel.Highest;
        }

        private Task? GetStartupTask()
        {
            var folder = _taskService.RootFolder.EnumerateFolders(folder => folder.Name.Equals(TaskFolderName)).FirstOrDefault();
            return folder?.EnumerateTasks(task => task.Name.Equals(TaskName)).FirstOrDefault();
        }

        private string GetCurrentUserName()
        {
            var userId = WindowsIdentity.GetCurrent().Name;
            return userId.Contains('\\') ? userId.Split('\\')[1] : userId;
        }
    }
}
