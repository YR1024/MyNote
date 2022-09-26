namespace DesktopIcon
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.NotIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.NotMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.显示ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.隐藏ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.关闭ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.隐藏任务栏ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NotMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // NotIcon
            // 
            this.NotIcon.ContextMenuStrip = this.NotMenuStrip;
            this.NotIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("NotIcon.Icon")));
            this.NotIcon.Text = "DesktopIconTool";
            this.NotIcon.Visible = true;
            // 
            // NotMenuStrip
            // 
            this.NotMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.隐藏任务栏ToolStripMenuItem,
            this.显示ToolStripMenuItem,
            this.隐藏ToolStripMenuItem,
            this.关闭ToolStripMenuItem});
            this.NotMenuStrip.Name = "NotMenuStrip";
            this.NotMenuStrip.Size = new System.Drawing.Size(181, 114);
            this.NotMenuStrip.Text = "桌面图标自动隐藏工具";
            // 
            // 显示ToolStripMenuItem
            // 
            this.显示ToolStripMenuItem.Checked = true;
            this.显示ToolStripMenuItem.CheckOnClick = true;
            this.显示ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.显示ToolStripMenuItem.Name = "显示ToolStripMenuItem";
            this.显示ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.显示ToolStripMenuItem.Text = "运行";
            this.显示ToolStripMenuItem.Click += new System.EventHandler(this.运行ToolStripMenuItem_Click);
            // 
            // 隐藏ToolStripMenuItem
            // 
            this.隐藏ToolStripMenuItem.Checked = true;
            this.隐藏ToolStripMenuItem.CheckOnClick = true;
            this.隐藏ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.隐藏ToolStripMenuItem.Name = "隐藏ToolStripMenuItem";
            this.隐藏ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.隐藏ToolStripMenuItem.Text = "开机启动";
            this.隐藏ToolStripMenuItem.Click += new System.EventHandler(this.开机启动ToolStripMenuItem_Click);
            // 
            // 关闭ToolStripMenuItem
            // 
            this.关闭ToolStripMenuItem.Name = "关闭ToolStripMenuItem";
            this.关闭ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.关闭ToolStripMenuItem.Text = "退出";
            this.关闭ToolStripMenuItem.Click += new System.EventHandler(this.关闭ToolStripMenuItem_Click);
            // 
            // 隐藏任务栏ToolStripMenuItem
            // 
            this.隐藏任务栏ToolStripMenuItem.CheckOnClick = true;
            this.隐藏任务栏ToolStripMenuItem.Name = "隐藏任务栏ToolStripMenuItem";
            this.隐藏任务栏ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.隐藏任务栏ToolStripMenuItem.Text = "隐藏任务栏";
            this.隐藏任务栏ToolStripMenuItem.Click += new System.EventHandler(this.隐藏任务栏ToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "DesktopIconTool";
            this.NotMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon NotIcon;
        private System.Windows.Forms.ContextMenuStrip NotMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem 显示ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 隐藏ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 关闭ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 隐藏任务栏ToolStripMenuItem;
    }
}

