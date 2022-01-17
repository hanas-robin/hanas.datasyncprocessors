namespace hanas.datasynccontroller
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
            this.service_task00 = new System.ServiceProcess.ServiceProcessInstaller();
            this.service_task01_datatransfercontroller = new System.ServiceProcess.ServiceInstaller();
            // 
            // service_task00
            // 
            this.service_task00.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.service_task00.Password = null;
            this.service_task00.Username = null;
            // 
            // service_task01_datatransfercontroller
            // 
            this.service_task01_datatransfercontroller.Description = "Hanas Data Sync Controller";
            this.service_task01_datatransfercontroller.DisplayName = "hanas.datasynccontroller";
            this.service_task01_datatransfercontroller.ServiceName = "hanas.datasynccontroller";
            this.service_task01_datatransfercontroller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // service_installer
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.service_task00,
            this.service_task01_datatransfercontroller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller service_task00;
        private System.ServiceProcess.ServiceInstaller service_task01_datatransfercontroller;
    }
}