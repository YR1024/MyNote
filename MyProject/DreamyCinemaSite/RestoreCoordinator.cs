public sealed class RestoreCoordinator
{
    private readonly object sync = new();
    private int activeWriters;
    private bool restoreInProgress;

    public bool IsRestoreInProgress
    {
        get
        {
            lock (sync)
            {
                return restoreInProgress;
            }
        }
    }

    public WriteLease? TryEnterWriter()
    {
        lock (sync)
        {
            if (restoreInProgress)
            {
                return null;
            }

            activeWriters++;
            return new WriteLease(this);
        }
    }

    public RestoreLease? TryBeginRestore()
    {
        lock (sync)
        {
            if (restoreInProgress || activeWriters > 0)
            {
                return null;
            }

            restoreInProgress = true;
            return new RestoreLease(this);
        }
    }

    private void ExitWriter()
    {
        lock (sync)
        {
            activeWriters = Math.Max(0, activeWriters - 1);
        }
    }

    private void EndRestore()
    {
        lock (sync)
        {
            restoreInProgress = false;
        }
    }

    public sealed class WriteLease(RestoreCoordinator owner) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                owner.ExitWriter();
            }
        }
    }

    public sealed class RestoreLease(RestoreCoordinator owner) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                owner.EndRestore();
            }
        }
    }
}

public sealed class BackupOperationGate
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
}
