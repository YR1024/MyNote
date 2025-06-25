using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopIconTool
{
    public partial class PluginManageWin : Form
    {
        private List<PluginModel> pluginList = new List<PluginModel>();
        public List<PluginModel> PluginModels
        {
            get
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.Plugins))
                {
                    return new List<PluginModel>();
                }
                return JsonConvert.DeserializeObject<List<PluginModel>>(Properties.Settings.Default.Plugins);
            }
            set
            {
                Properties.Settings.Default.Plugins = JsonConvert.SerializeObject(value);
            }
        }

        public static PluginManageWin Instacne;
        private PluginManageWin()
        {
            InitializeComponent();
            GridStyle();

            // 加载保存的 pluginList 到 dataGridView1
            if (PluginModels != null)
            {
                pluginList = PluginModels;
                UpdateDataGridView();
            }
            FormClosed += PluginManageWin_FormClosed;
        }

        public static void CreateInstance()
        {
            Instacne = new PluginManageWin();
        }

        private void PluginManageWin_FormClosed(object sender, FormClosedEventArgs e)
        {
            Instacne = null;
        }

        void GridStyle()
        {
            dataGridView1.AllowUserToAddRows = false;
            //dataGridView1.SelectionMode = DataGridViewSelectionMode.c;
        }


        /// <summary>
        /// 添加插件
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable Files (*.exe)|*.exe|Dynamic Link Library Files (*.dll)|*.dll";
            openFileDialog.Title = "Select a File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);
                // 创建一个 PluginModel 对象
                PluginModel plugin = new PluginModel
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(filePath),
                    Path = filePath,
                    Description = "", // 这里你可以添加描述
                    Status = false,     // 这里你可以设置状态
                };

                // 将 PluginModel 对象添加到 pluginList 中
                pluginList.Add(plugin);
            }

            // 更新 dataGridView1 的显示
            UpdateDataGridView();
            // 保存 pluginList 到 Properties.Settings.Default.Plugins
            PluginModels = pluginList;
            Properties.Settings.Default.Save();
        }

        private void UpdateDataGridView()
        {
            // 清空 dataGridView1 的行
            dataGridView1.Rows.Clear();

            // 遍历 pluginList，将每个 PluginModel 对象添加到 dataGridView1 中
            foreach (PluginModel plugin in pluginList)
            {
                dataGridView1.Rows.Add(plugin.Name, plugin.Path, plugin.Description, plugin.Status);
            }
        }

        /// <summary>
        /// 删除插件
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            // 获取选中的行
            if (dataGridView1.SelectedRows.Count > 0)
            {
                // 获取选中行的索引
                int selectedIndex = dataGridView1.SelectedRows[0].Index;

                // 从 pluginList 中移除对应的 PluginModel 对象
                pluginList.RemoveAt(selectedIndex);

                // 更新 dataGridView1 的显示
                UpdateDataGridView();

                // 保存 pluginList 到 Properties.Settings.Default.Plugins
                PluginModels = pluginList;
                Properties.Settings.Default.Save();
            }
        }

       
    }

    public class PluginModel
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public string Description { get; set; }

        public bool Status { get; set; }
    }
}
