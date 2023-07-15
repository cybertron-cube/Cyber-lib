using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace AvaloniaMessageBox.Base
{
    internal class MsgBoxWindowBase<W, T> : IMessageBoxWindow<T> where W : Window, IWindowGetResult<T>
    {
        private readonly W _window;
        internal MsgBoxWindowBase(W window)
        {
            _window = window;
        }
        public Task<T> Show()
        {
            TaskCompletionSource<T> tcs = new();
            _window.Closed += delegate
            {
                tcs.TrySetResult(_window.GetResult());
            };
            _window.Show();
            return tcs.Task;
        }
        public Task<T> Show(Window ownerWindow)
        {
            TaskCompletionSource<T> tcs = new();
            _window.Closed += delegate
            {
                tcs.TrySetResult(_window.GetResult());
            };
            _window.Show(ownerWindow);
            return tcs.Task;
        }
        public Task<T> ShowDialog(Window ownerWindow)
        {
            TaskCompletionSource<T> tcs = new();
            _window.Closed += delegate
            {
                tcs.TrySetResult(_window.GetResult());
            };
            _window.ShowDialog(ownerWindow);
            return tcs.Task;
        }
    }
}
