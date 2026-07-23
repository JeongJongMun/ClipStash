namespace EasyClipStash;

static class Program
{
    private const string MutexName = @"Local\EasyClipStash_SingleInstance";

    /// <summary>업데이트가 띄운 인스턴스임을 알리는 인자. 이 경우 이전 인스턴스의 종료를 기다린다.</summary>
    internal const string AfterUpdateArgument = "--after-update";

    private static Mutex? _instanceMutex;

    [STAThread]
    static void Main(string[] args)
    {
        bool afterUpdate = args.Any(a => string.Equals(a, AfterUpdateArgument, StringComparison.OrdinalIgnoreCase));

        if (!TryAcquireSingleInstance(afterUpdate))
        {
            MessageBox.Show(L.AlreadyRunning, "EasyClipStash", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext());
        }
        finally
        {
            ReleaseInstanceLock();
        }
    }

    /// <summary>
    /// 단일 실행 잠금을 잡는다.
    /// 업데이트 직후라면 이전 인스턴스가 아직 종료 중일 수 있으므로 잠시 기다렸다 재시도한다.
    /// (그렇지 않으면 교체 직후 뜬 새 인스턴스가 "이미 실행 중"으로 튕겨 아무것도 남지 않는다)
    /// </summary>
    private static bool TryAcquireSingleInstance(bool afterUpdate)
    {
        DateTime deadline = DateTime.UtcNow + (afterUpdate ? TimeSpan.FromSeconds(15) : TimeSpan.Zero);

        while (true)
        {
            var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (createdNew)
            {
                _instanceMutex = mutex;
                return true;
            }

            mutex.Dispose();
            if (DateTime.UtcNow >= deadline)
                return false;
            Thread.Sleep(250);
        }
    }

    /// <summary>
    /// 단일 실행 잠금을 즉시 푼다.
    /// 업데이트가 새 인스턴스를 띄우기 직전에 호출해, 새 인스턴스가 잠금을 바로 잡을 수 있게 한다.
    /// </summary>
    internal static void ReleaseInstanceLock()
    {
        if (_instanceMutex is null) return;

        try { _instanceMutex.ReleaseMutex(); }
        catch (ApplicationException) { /* 다른 스레드에서 호출된 경우 — 아래 Dispose가 핸들을 닫아 해제된다 */ }

        _instanceMutex.Dispose();
        _instanceMutex = null;
    }
}
