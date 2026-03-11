namespace MarkdownPad
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = "MarkdownPad.SingleInstance.v1";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string[] startupFiles = [.. args.Where(arg => !string.IsNullOrWhiteSpace(arg))];

            using var singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew && SingleInstanceChannel.TrySendRequest(startupFiles))
                return;

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            using var activationChannel = createdNew ? new SingleInstanceChannel() : null;
            var mainForm = new frmMain();

            if (activationChannel is not null)
            {
                mainForm.Shown += (_, _) => activationChannel.Start();
                activationChannel.RequestReceived += (_, e) =>
                {
                    if (mainForm.IsDisposed)
                        return;

                    if (mainForm.IsHandleCreated)
                    {
                        mainForm.BeginInvoke(() => mainForm.OpenExternalDocuments(e.FilePaths));
                    }
                };
            }

            if (startupFiles.Length > 0)
            {
                mainForm.Shown += (_, _) => mainForm.OpenExternalDocuments(startupFiles);
            }

            Application.Run(mainForm);
        }
    }
}
