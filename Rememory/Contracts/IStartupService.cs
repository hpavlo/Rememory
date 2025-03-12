namespace Rememory.Contracts
{
    public interface IStartupService
    {
        bool IsStartupEnabled { get; set; }
        bool IsStartupAsAdministratorEnabled { get; set; }
    }
}
