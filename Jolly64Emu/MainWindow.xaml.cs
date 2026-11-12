using Jolly64Emu.Core.CIA6526;
using Jolly64Emu.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Jolly64Emu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _closeRequested = false;
        C64ViewModel _C64ViewModel;

        public MainWindow()
        {
            InitializeComponent();
            _C64ViewModel = new C64ViewModel();
            DataContext = _C64ViewModel;


        }

        private void LoadPrg_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load C64 PRG",
                Filter = "C64 programs (*.prg)|*.prg|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != true)
                return;
            try
            {
                var result = _C64ViewModel.LoadPrg(dialog.FileName);
                // BASIC PRGs are automatically started through the C64 keyboard buffer.
                // Raw machine-code PRGs need a known entry point or BASIC SYS stub.
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "PRG load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadT64_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load C64 T64 tape image",
                Filter = "C64 tape images (*.t64)|*.t64|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                byte[] image = System.IO.File.ReadAllBytes(dialog.FileName);
                var entries = Core.T64Loader.ReadDirectory(image);
                Core.T64Entry entry = entries[0];

                if (entries.Count > 1)
                {
                    var selection = new System.Windows.Window
                    {
                        Title = "Select T64 program",
                        Owner = this,
                        Width = 440,
                        Height = 320,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    var panel = new System.Windows.Controls.DockPanel { Margin = new Thickness(12) };
                    var list = new System.Windows.Controls.ListBox
                    {
                        ItemsSource = entries,
                        DisplayMemberPath = "Name",
                        SelectedIndex = 0
                    };
                    var load = new System.Windows.Controls.Button
                    {
                        Content = "Load selected program",
                        Margin = new Thickness(0, 10, 0, 0),
                        Padding = new Thickness(10, 5, 10, 5),
                        IsDefault = true
                    };
                    load.Click += (_, _) => { selection.DialogResult = true; };
                    System.Windows.Controls.DockPanel.SetDock(load, System.Windows.Controls.Dock.Bottom);
                    panel.Children.Add(load);
                    panel.Children.Add(list);
                    selection.Content = panel;
                    if (selection.ShowDialog() != true)
                        return;
                    entry = (Core.T64Entry)list.SelectedItem;
                }

                _C64ViewModel.LoadPrgData(Core.T64Loader.ExtractPrg(image, entry));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "T64 load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadD64_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load C64 D64 disk image",
                Filter = "C64 disk images (*.d64)|*.d64|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                byte[] image = System.IO.File.ReadAllBytes(dialog.FileName);
                var entries = Core.D64Loader.ReadDirectory(image);
                Core.D64Entry entry = entries[0];

                if (entries.Count > 1)
                {
                    var selection = new Window
                    {
                        Title = "Select D64 program",
                        Owner = this,
                        Width = 460,
                        Height = 360,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    var panel = new System.Windows.Controls.DockPanel { Margin = new Thickness(12) };
                    var list = new System.Windows.Controls.ListBox
                    {
                        ItemsSource = entries,
                        SelectedIndex = 0
                    };
                    var load = new System.Windows.Controls.Button
                    {
                        Content = "Load selected program",
                        Margin = new Thickness(0, 10, 0, 0),
                        Padding = new Thickness(10, 5, 10, 5),
                        IsDefault = true
                    };
                    load.Click += (_, _) => { if (list.SelectedItem != null) selection.DialogResult = true; };
                    System.Windows.Controls.DockPanel.SetDock(load, System.Windows.Controls.Dock.Bottom);
                    panel.Children.Add(load);
                    panel.Children.Add(list);
                    selection.Content = panel;
                    if (selection.ShowDialog() != true)
                        return;
                    entry = (Core.D64Entry)list.SelectedItem;
                }

                _C64ViewModel.LoadPrgData(Core.D64Loader.ExtractPrg(image, entry));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "D64 load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _C64ViewModel.OnKeyDown(e.Key);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _C64ViewModel.OnKeyUp(e.Key);
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_closeRequested)
                return;

            e.Cancel = true;
            _closeRequested = true;

            await ((C64ViewModel)DataContext).RequestShutdownAsync();

            // Let WPF continue closing AFTER this handler exits
            Dispatcher?.BeginInvoke(new Action(() =>
            {
                Application.Current.Shutdown();
            }));
        }

        private void ExecutingAssembler_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            AssemblyScroller.ScrollToEnd();
        }
    }
}