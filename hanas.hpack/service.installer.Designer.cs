namespace hanas.hpack
{
    partial class service_installer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.service_task = new System.ServiceProcess.ServiceProcessInstaller();
            this.service_task_datatransferproc = new System.ServiceProcess.ServiceInstaller();
            // 
            // service_task
            // 
            this.service_task.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.service_task.Password = null;
            this.service_task.Username = null;
            // 
            // service_task_datatransferproc
            // 
            this.service_task_datatransferproc.Description = "Hanas Data Transfer Processor";
            this.service_task_datatransferproc.DisplayName = "hanas.hpack";
            this.service_task_datatransferproc.ServiceName = "hanas.hpack";
            this.service_task_datatransferproc.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // service_installer
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.service_task,
            this.service_task_datatransferproc});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller service_task;
        private System.ServiceProcess.ServiceInstaller service_task_datatransferproc;
    }
}