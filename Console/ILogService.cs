using System;

namespace ILang.Util.Log
{
    public interface ILogService
    {
        void RegisterApplicationError(Exception ex);
        void RegisterApplicationError(Exception ex, string metadata);
        void RegisterApplicationWarning(Exception ex);
        void RegisterApplicationWarning(Exception ex, string metadata = null);
    }
}
