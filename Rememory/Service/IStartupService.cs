namespace Rememory.Service
{
    public interface IStartupService
    {
        bool IsStartupEnabled { get; set; }
        bool IsStartupAsAdministratorEnabled { get; set; }
    }
}
