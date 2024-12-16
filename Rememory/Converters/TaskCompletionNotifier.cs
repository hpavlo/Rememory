using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Rememory.Converters
{
    public class TaskCompletionNotifier<TResult> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public Task<TResult> Task { get; private set; }
        public TResult Result => Task.Status == TaskStatus.RanToCompletion ? Task.Result : default;
        public TaskStatus Status => Task.Status;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;
        public AggregateException Exception => Task.Exception;

        public TaskCompletionNotifier(Task<TResult> task)
        {
            Task = task;
            if (!task.IsCompleted)
            {
                var _ = WatchTaskAsync(task);
            }
        }

        private async Task WatchTaskAsync(Task<TResult> task)
        {
            try
            {
                await task;
            }
            catch { }
            finally
            {
                NotifyPropertyChanged(nameof(IsCompleted));
                NotifyPropertyChanged(nameof(IsNotCompleted));
                NotifyPropertyChanged(nameof(Result));
                NotifyPropertyChanged(nameof(Exception));
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
