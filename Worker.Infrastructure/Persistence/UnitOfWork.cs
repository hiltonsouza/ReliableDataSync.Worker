namespace Worker.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    readonly DbSession _session;

    public UnitOfWork(DbSession session)
    {
        _session = session;
    }

    public void BeginTransaction()
    {
        _session.Transaction = _session.Connection.BeginTransaction();
    }

    public void Commit()
    {
        _session.Transaction?.Commit();
        DisposeTransaction();
    }
    public void Rollback()
    {
        _session.Transaction?.Rollback();
        DisposeTransaction();
    }

    private void DisposeTransaction()
    {
        _session.Transaction?.Dispose();
        _session.Transaction = null;
    }
    public void Dispose()
    {
        DisposeTransaction();
    }

}

