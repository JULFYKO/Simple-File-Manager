using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
//  ____  _                 _        _____ _ _        __  __                                   
// / ___|(_)_ __ ___  _ __ | | ___  |  ___(_) | ___  |  \/  | __ _ _ __   __ _  __ _  ___ _ __ 
// \___ \| | '_ ` _ \| '_ \| |/ _ \ | |_  | | |/ _ \ | |\/| |/ _` | '_ \ / _` |/ _` |/ _ \ '__|
//  ___) | | | | | | | |_) | |  __/ |  _| | | |  __/ | |  | | (_| | | | | (_| | (_| |  __/ |   
// |____/|_|_| |_| |_| .__/|_|\___| |_|   |_|_|\___| |_|  |_|\__,_|_| |_|\__,_|\__, |\___|_|   
//                   |_|                                                       |___/           
namespace SimpleFileManager
{
    public partial class MainForm : Form
    {
        // Поточний шлях
        private string currentPath;
        // Список файлів
        private ListView listViewFiles;
        // Поле шляху
        private TextBox textBoxPath;
        // Поле пошуку
        private TextBox textBoxSearch;
        // Кнопка Help
        private Button buttonHelp;
        // Контекстне меню
        private ContextMenuStrip contextMenu;

        // Історія навігації
        private Stack<string> backHistory = new Stack<string>();
        private Stack<string> forwardHistory = new Stack<string>();

        public MainForm()
        {
            InitializeComponent();
            // Встановлюємо шлях
            currentPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            textBoxPath.Text = currentPath;
            // Завантажуємо файли
            LoadFiles(currentPath);
        }

        // Ініціалізація форми
        private void InitializeComponent()
        {
            this.Text = "Simple File Manager";
            this.Size = new Size(1000, 700);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Головний контейнер
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.RowCount = 2;
            mainLayout.ColumnCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.Controls.Add(mainLayout);

            // Верхня панель
            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;
            topPanel.Padding = new Padding(10);
            mainLayout.Controls.Add(topPanel, 0, 0);

            // Поле шляху
            textBoxPath = new TextBox();
            textBoxPath.Font = new Font("Segoe UI", 10);
            textBoxPath.Location = new Point(10, 10);
            textBoxPath.Size = new Size(700, 30);
            textBoxPath.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            textBoxPath.KeyDown += textBoxPath_KeyDown;
            topPanel.Controls.Add(textBoxPath);

            // Кнопка Help
            buttonHelp = new Button();
            buttonHelp.Font = new Font("Segoe UI", 10);
            buttonHelp.Text = "Help";
            buttonHelp.Size = new Size(220, 65);
            buttonHelp.Location = new Point(textBoxPath.Right + 10, 10);
            buttonHelp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonHelp.Click += buttonHelp_Click;
            topPanel.Controls.Add(buttonHelp);

            // Поле пошуку
            textBoxSearch = new TextBox();
            textBoxSearch.Font = new Font("Segoe UI", 10);
            textBoxSearch.Location = new Point(10, 50);
            textBoxSearch.Size = new Size(700, 30);
            textBoxSearch.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            textBoxSearch.Text = "Search folder...";
            textBoxSearch.ForeColor = Color.Gray;
            textBoxSearch.Enter += (s, e) =>
            {
                if (textBoxSearch.Text == "Search folder...")
                {
                    textBoxSearch.Text = "";
                    textBoxSearch.ForeColor = Color.Black;
                }
            };
            textBoxSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBoxSearch.Text))
                {
                    textBoxSearch.Text = "Search folder...";
                    textBoxSearch.ForeColor = Color.Gray;
                }
            };
            textBoxSearch.KeyDown += textBoxSearch_KeyDown;
            topPanel.Controls.Add(textBoxSearch);

            // ListView файлів
            listViewFiles = new ListView();
            listViewFiles.Font = new Font("Segoe UI", 10);
            listViewFiles.View = View.Details;
            listViewFiles.FullRowSelect = true;
            listViewFiles.GridLines = true;
            listViewFiles.Dock = DockStyle.Fill;
            listViewFiles.Columns.Add("Name", 500);
            listViewFiles.Columns.Add("Type", 150);
            listViewFiles.Columns.Add("Size", 150);
            listViewFiles.DoubleClick += listViewFiles_DoubleClick;
            listViewFiles.MouseUp += listViewFiles_MouseUp;
            mainLayout.Controls.Add(listViewFiles, 0, 1);

            // Контекстне меню
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Help", null, ContextMenu_Help);
            contextMenu.Items.Add("Rename", null, ContextMenu_Rename);
            contextMenu.Items.Add("Refresh", null, ContextMenu_Refresh);
            contextMenu.Items.Add("Open", null, ContextMenu_Open);
            contextMenu.Items.Add("Copy", null, ContextMenu_Copy);
            contextMenu.Items.Add("Move", null, ContextMenu_Move);
            contextMenu.Items.Add("Delete", null, ContextMenu_Delete);
            contextMenu.Items.Add("Back", null, ContextMenu_Back);
            contextMenu.Items.Add("Forward", null, ContextMenu_Forward);
            contextMenu.Items.Add("Up", null, ContextMenu_Up);
            listViewFiles.ContextMenuStrip = contextMenu;
        }

        // Завантаження файлів і папок
        private void LoadFiles(string path)
        {
            listViewFiles.Items.Clear();
            try
            {
                // Папки
                foreach (var dir in Directory.GetDirectories(path))
                {
                    string dirName = Path.GetFileName(dir);
                    ListViewItem item = new ListViewItem(dirName);
                    item.SubItems.Add("Folder");
                    item.SubItems.Add("");
                    listViewFiles.Items.Add(item);
                }
                // Файли
                foreach (var file in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(file);
                    FileInfo fi = new FileInfo(file);
                    ListViewItem item = new ListViewItem(fileName);
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".exe" || ext == ".png")
                    {
                        item.SubItems.Add(ext.Substring(1));
                    }
                    else
                    {
                        item.SubItems.Add("File");
                    }
                    item.SubItems.Add(fi.Length.ToString());
                    listViewFiles.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load error: " + ex.Message);
            }
        }

        // Двійний клік - відкрити елемент
        private void listViewFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;
            ListViewItem selectedItem = listViewFiles.SelectedItems[0];
            if (selectedItem.SubItems[1].Text == "Folder")
            {
                backHistory.Push(currentPath);
                currentPath = Path.Combine(currentPath, selectedItem.Text);
                textBoxPath.Text = currentPath;
                LoadFiles(currentPath);
            }
            else
            {
                string fullPath = Path.Combine(currentPath, selectedItem.Text);
                try
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Open error: " + ex.Message);
                }
            }
        }

        // Правий клік - меню
        private void listViewFiles_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewItem item = listViewFiles.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    item.Selected = true;
                    contextMenu.Show(listViewFiles, e.Location);
                }
            }
        }

        // Enter у полі шляху
        private void textBoxPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string path = textBoxPath.Text;
                if (Directory.Exists(path))
                {
                    backHistory.Push(currentPath);
                    currentPath = path;
                    LoadFiles(currentPath);
                }
                else
                {
                    MessageBox.Show("No dir");
                }
            }
        }

        // Enter у полі пошуку
        private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string search = textBoxSearch.Text.ToLower();
                if (string.IsNullOrWhiteSpace(search) || search == "search folder...")
                {
                    LoadFiles(currentPath);
                }
                else
                {
                    listViewFiles.Items.Clear();
                    // Пошук папок
                    foreach (var dir in Directory.GetDirectories(currentPath))
                    {
                        string dirName = Path.GetFileName(dir);
                        if (dirName.ToLower().Contains(search))
                        {
                            ListViewItem item = new ListViewItem(dirName);
                            item.SubItems.Add("Folder");
                            item.SubItems.Add("");
                            listViewFiles.Items.Add(item);
                        }
                    }
                    // Пошук файлів
                    foreach (var file in Directory.GetFiles(currentPath))
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName.ToLower().Contains(search))
                        {
                            FileInfo fi = new FileInfo(file);
                            ListViewItem item = new ListViewItem(fileName);
                            string ext = Path.GetExtension(file).ToLower();
                            if (ext == ".exe" || ext == ".png")
                            {
                                item.SubItems.Add(ext.Substring(1));
                            }
                            else
                            {
                                item.SubItems.Add("File");
                            }
                            item.SubItems.Add(fi.Length.ToString());
                            listViewFiles.Items.Add(item);
                        }
                    }
                }
            }
        }

        // Help кнопка
        private void buttonHelp_Click(object sender, EventArgs e)
        {
            ShowDetailedHelp();
        }

        // Показуємо детальну допомогу
        private void ShowDetailedHelp()
        {
            string helpInfo = "Simple File Manager\n\n" +
                              "Usage:\n" +
                              " - Enter a path in the path field and press Enter to load files.\n" +
                              " - Use the search box to filter files and folders.\n" +
                              " - Double-click a folder to open it, or a file to launch it.\n" +
                              " - Right-click an item for more options (Rename, Refresh, Open, Copy, Move, Delete).\n\n" +
                              "Keyboard Shortcuts:\n" +
                              " F1: Help\n" +
                              " F2: Rename\n" +
                              " F3: Refresh\n" +
                              " F4: Open\n" +
                              " F5: Copy\n" +
                              " F6: Move\n" +
                              " F7: Delete\n" +
                              " F8: Back\n" +
                              " F9: Forward\n" +
                              " F10: Up";
            MessageBox.Show(helpInfo, "Detailed Help");
        }

        // Help меню
        private void ContextMenu_Help(object sender, EventArgs e)
        {
            ShowDetailedHelp();
        }

        // Rename меню
        private void ContextMenu_Rename(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;
            ListViewItem selectedItem = listViewFiles.SelectedItems[0];
            string itemName = selectedItem.Text;
            bool isFolder = selectedItem.SubItems[1].Text == "Folder";
            string oldPath = Path.Combine(currentPath, itemName);
            string newName = Prompt.ShowDialog("Enter new name:", "Rename");
            if (!string.IsNullOrWhiteSpace(newName))
            {
                string newPath = Path.Combine(currentPath, newName);
                try
                {
                    if (isFolder)
                        Directory.Move(oldPath, newPath);
                    else
                        File.Move(oldPath, newPath);
                    LoadFiles(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Rename error: " + ex.Message);
                }
            }
        }

        // Refresh меню
        private void ContextMenu_Refresh(object sender, EventArgs e)
        {
            LoadFiles(currentPath);
        }

        // Open меню
        private void ContextMenu_Open(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;
            ListViewItem selectedItem = listViewFiles.SelectedItems[0];
            if (selectedItem.SubItems[1].Text == "Folder")
            {
                backHistory.Push(currentPath);
                currentPath = Path.Combine(currentPath, selectedItem.Text);
                textBoxPath.Text = currentPath;
                LoadFiles(currentPath);
            }
            else
            {
                string fullPath = Path.Combine(currentPath, selectedItem.Text);
                try
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Open error: " + ex.Message);
                }
            }
        }

        // Copy меню
        private void ContextMenu_Copy(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;
            ListViewItem selectedItem = listViewFiles.SelectedItems[0];
            bool isFolder = selectedItem.SubItems[1].Text == "Folder";
            string itemName = selectedItem.Text;
            string sourcePath = Path.Combine(currentPath, itemName);
            string destFolder = Prompt.ShowDialog("Destination path:", "Copy");
            if (Directory.Exists(destFolder))
            {
                string destPath = Path.Combine(destFolder, itemName);
                try
                {
                    if (isFolder)
                        CopyDirectory(sourcePath, destPath);
                    else
                        File.Copy(sourcePath, destPath, true);
                    MessageBox.Show("Copied", "Copy");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Copy error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("No dest dir.", "Copy");
            }
        }

        // Move меню
        private void ContextMenu_Move(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;
            ListViewItem selectedItem = listViewFiles.SelectedItems[0];
            bool isFolder = selectedItem.SubItems[1].Text == "Folder";
            string itemName = selectedItem.Text;
            string sourcePath = Path.Combine(currentPath, itemName);
            string destFolder = Prompt.ShowDialog("Destination path:", "Move");
            if (Directory.Exists(destFolder))
            {
                string destPath = Path.Combine(destFolder, itemName);
                try
                {
                    if (isFolder)
                        Directory.Move(sourcePath, destPath);
                    else
                        File.Move(sourcePath, destPath);
                    LoadFiles(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Move error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("No dest dir.", "Move");
            }
        }

        // Delete меню
        private void ContextMenu_Delete(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;
            ListViewItem selectedItem = listViewFiles.SelectedItems[0];
            bool isFolder = selectedItem.SubItems[1].Text == "Folder";
            string itemName = selectedItem.Text;
            string fullPath = Path.Combine(currentPath, itemName);
            DialogResult result = MessageBox.Show("Delete item?", "Delete", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                try
                {
                    if (isFolder)
                        Directory.Delete(fullPath, true);
                    else
                        File.Delete(fullPath);
                    LoadFiles(currentPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Delete error: " + ex.Message);
                }
            }
        }

        // Back меню
        private void ContextMenu_Back(object sender, EventArgs e)
        {
            NavigateBack();
        }

        // Forward меню
        private void ContextMenu_Forward(object sender, EventArgs e)
        {
            NavigateForward();
        }

        // Up меню
        private void ContextMenu_Up(object sender, EventArgs e)
        {
            NavigateUp();
        }

        // Функція Back
        private void NavigateBack()
        {
            if (backHistory.Count > 0)
            {
                forwardHistory.Push(currentPath);
                currentPath = backHistory.Pop();
                textBoxPath.Text = currentPath;
                LoadFiles(currentPath);
            }
        }

        // Функція Forward
        private void NavigateForward()
        {
            if (forwardHistory.Count > 0)
            {
                backHistory.Push(currentPath);
                currentPath = forwardHistory.Pop();
                textBoxPath.Text = currentPath;
                LoadFiles(currentPath);
            }
        }

        // Функція Up
        private void NavigateUp()
        {
            DirectoryInfo parent = Directory.GetParent(currentPath);
            if (parent != null)
            {
                backHistory.Push(currentPath);
                currentPath = parent.FullName;
                textBoxPath.Text = currentPath;
                LoadFiles(currentPath);
            }
        }

        // Рекурсивне копіювання папки
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destSubDir);
            }
        }

        // Гарячі клавіші
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    ContextMenu_Help(null, null);
                    return true;
                case Keys.F2:
                    ContextMenu_Rename(null, null);
                    return true;
                case Keys.F3:
                    ContextMenu_Refresh(null, null);
                    return true;
                case Keys.F4:
                    ContextMenu_Open(null, null);
                    return true;
                case Keys.F5:
                    ContextMenu_Copy(null, null);
                    return true;
                case Keys.F6:
                    ContextMenu_Move(null, null);
                    return true;
                case Keys.F7:
                    ContextMenu_Delete(null, null);
                    return true;
                case Keys.F8:
                    NavigateBack();
                    return true;
                case Keys.F9:
                    NavigateForward();
                    return true;
                case Keys.F10:
                    NavigateUp();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    // Клас для вводу
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            Label textLabel = new Label()
            {
                Left = 20,
                Top = 20,
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            TextBox textBox = new TextBox()
            {
                Left = 20,
                Top = 50,
                Width = 440,
                Font = new Font("Segoe UI", 10)
            };
            Button confirmation = new Button()
            {
                Text = "Ok",
                Left = 380,
                Width = 80,
                Top = 80,
                DialogResult = DialogResult.OK,
                Font = new Font("Segoe UI", 10)
            };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;
            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}