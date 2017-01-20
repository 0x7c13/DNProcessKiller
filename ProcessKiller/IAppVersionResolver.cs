
namespace ProcessKiller
{
    using System;

    public interface IAppVersionResolver
    {
        Version GetLatestVersion();
    }
}