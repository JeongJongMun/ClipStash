namespace ClipStash;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Local\ClipStash_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(L.AlreadyRunning, "ClipStash", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
