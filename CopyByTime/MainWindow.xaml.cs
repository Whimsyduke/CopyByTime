using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Security.AccessControl;

namespace CopyByTime
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileTreeViewItem rootItem;
        public MainWindow()
        {
            InitializeComponent();
            TimeEndSet.SelectedDate = DateTime.Now;
            TimeEndSet.DisplayDate = DateTime.Now;
            TimeStartSet.SelectedDate = DateTime.Now.AddMonths(-1);
            TimeStartSet.DisplayDate = DateTime.Now.AddMonths(-1);
        }
        private DirectoryInfo SelectFolder(string startPath, string title)
        {
            if (!Directory.Exists(startPath))
            {
                startPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.Description = title;
            folderDialog.SelectedPath = startPath;
            folderDialog.ShowDialog();
            if (folderDialog.SelectedPath != String.Empty)
            {
                return new DirectoryInfo(folderDialog.SelectedPath);
            }
            else
            {
                return null;
            }
        }
        
        private void SetBasePath_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo folder = SelectFolder(BasePath.Text, "选择基础目录");
            if (folder == null) return;
            BasePath.Text = folder.FullName;
            e.Handled = true;
        }

        private void SetToPath_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo folder = SelectFolder(ToPath.Text, "选择输出目录");
            if (folder == null) return;
            ToPath.Text = folder.FullName;
            e.Handled = true;
        }
        
        private void ToPath_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void BasePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Directory.Exists(BasePath.Text))
            {
                return;
            }
            RefreshTreeView();
            e.Handled = true;
        }

        public class FileTreeViewItem : TreeViewItem
        {
            public enum Type
            {
                File,
                Dir,
                None
            }

            private Type baseType;
            public Type BaseType
            {
                get
                {
                    return baseType;
                }

                set
                {
                    baseType = value;
                }
            }

            private DirectoryInfo dir;
            public DirectoryInfo Dir
            {
                get
                {
                    return dir;
                }

                set
                {
                    dir = value;
                }
            }

            private FileInfo file;
            public FileInfo File
            {
                get
                {
                    return file;
                }

                set
                {
                    file = value;
                }
            }

            private FileTreeViewItem parentItem;
            public FileTreeViewItem ParentItem
            {
                get
                {
                    return parentItem;
                }

                set
                {
                    parentItem = value;
                }
            }

            private bool itemVisible = true;
            public bool ItemVisible
            {
                get
                {
                    return itemVisible;
                }

                set
                {
                    itemVisible = value;
                    if (itemVisible || parentItem == null)
                    {
                        Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Visibility = Visibility.Collapsed;
                    }
                    if (itemVisible && parentItem != null && parentItem.ItemVisible == false)
                    {
                        parentItem.ItemVisible = true;
                    }
                }
            }
            
            public FileTreeViewItem(Type type, object data, FileTreeViewItem parent)
            {
                baseType = type;
                switch (type)
                {
                    case Type.File:
                        file = data as FileInfo;
                        Header = file.Name;
                        break;
                    case Type.Dir:
                        dir = data as DirectoryInfo;
                        Header = dir.Name;
                        break;
                    case Type.None:
                        throw new ArgumentOutOfRangeException("错误的类型");
                    default:
                        throw new ArgumentOutOfRangeException("错误的类型");
                }
                parentItem = parent;
                if (parent != null)
                {
                    parent.Items.Add(this);
                }
            }

            public FileTreeViewItem(string str, FileTreeViewItem parent)
            {
                Header = str;
                baseType = Type.None;
                parentItem = parent;
                if (parent != null)
                {
                    parent.Items.Add(this);
                }
            }
            
            public void CheckModifyTime(MainWindow window)
            {
                DateTime modify;
                switch (baseType)
                {
                    case Type.File:
                        modify = file.LastWriteTime;
                        if (window.SelectFiles.IsChecked == true)
                        {
                            break; 
                        }
                        ItemVisible = parentItem.ItemVisible;
                        return;
                    case Type.Dir:
                        modify = dir.LastWriteTime;
                        break;
                    case Type.None:
                        ItemVisible = true;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException("错误的类型");
                }
                if (modify.CompareTo(window.TimeStartSet.SelectedDate) >= 0 && modify.CompareTo(window.TimeEndSet.SelectedDate) <= 0)
                {
                    ItemVisible = true;
                }
                else
                {
                    ItemVisible = false;
                }
                foreach (FileTreeViewItem select in Items)
                {
                    select.CheckModifyTime(window);
                }
            }
        }

        private void RefreshTreeView()
        {
            TreeList.Items.Clear();
            DirectoryInfo baseDir = new DirectoryInfo(BasePath.Text);
            rootItem = new FileTreeViewItem(FileTreeViewItem.Type.Dir, baseDir, null);
            AddDirToTreeView(rootItem, baseDir);
            TreeList.Items.Add(rootItem);
            rootItem.CheckModifyTime(this);
        }

        private void AddDirToTreeView(FileTreeViewItem item, DirectoryInfo dir)
        {
            try
            {
                DirectoryInfo[] dirList = dir.GetDirectories();
                FileInfo[] fileList = dir.GetFiles();
                foreach (DirectoryInfo select in dirList)
                {
                    FileTreeViewItem selectItem = new FileTreeViewItem(FileTreeViewItem.Type.Dir, select, item);
                    AddDirToTreeView(selectItem, select);
                }
                foreach (FileInfo select in fileList)
                {
                    FileTreeViewItem selectItem = new FileTreeViewItem(FileTreeViewItem.Type.File, select, item);
                }
            }
            catch
            {
                FileTreeViewItem selectItem = new FileTreeViewItem("无法获取权限", item);
            }
        }

        private void TimeStartSet_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootItem != null) rootItem.CheckModifyTime(this);
            e.Handled = true;
        }

        private void TimeEndSet_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootItem != null) rootItem.CheckModifyTime(this);
            e.Handled = true;
        }
    }
}
