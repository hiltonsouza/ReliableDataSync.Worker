using Worker.Domain.Entities;
using Worker.Domain.Enums;

public interface IUnitOfWork : IDisposable
{
    void BeginTransaction();
    void Commit();
    void Rollback();
}
