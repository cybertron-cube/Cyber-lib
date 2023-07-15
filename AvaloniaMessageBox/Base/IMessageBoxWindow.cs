using Avalonia.Controls;
using System.Threading.Tasks;

namespace AvaloniaMessageBox.Base
{
    public interface IMessageBoxWindow<T>
    {
        Task<T> ShowDialog(Window owner);
        Task<T> Show();
        Task<T> Show(Window owner);
    }
}
