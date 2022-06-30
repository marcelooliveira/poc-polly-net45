using System;
using System.Collections.Generic;

namespace ILang.Util.Log
{
    public static class Logger
    {
        private readonly static object _lock = new object();
        private readonly static List<ILang.Util.Log.ILogService> _logServices = new List<Util.Log.ILogService>();

        public static int LogServiceCount { get { return _logServices.Count; } }

        public static void ClearLogServices()
        {
            lock (_lock)
            {
                _logServices.Clear();
            }
        }

        public static void AddLogService(ILogService log)
        {
            if (log == null)
            {
                return;
            }

            if (!Exists(log))
            {
                lock (_lock)
                {
                    if (!Exists(log))
                    {
                        _logServices.Add(log);
                    }
                }
            }
        }

        private static bool Exists(ILogService log)
        {
            if (_logServices.Contains(log))
            {
                return true;
            }

            if (_logServices.Exists(i => i.GetType().Name.Equals(log.GetType().Name)))
            {
                return true;
            }

            return false;
        }

        public static void LogException(Exception ex)
        {
            LogException(ex, null);
        }       



        public static void LogException(Exception ex, string metadata)
        {
            if (ex == null)
            {
                return;
            }

            _logServices.ForEach((item) =>
            {
                item.RegisterApplicationError(ex, metadata);
            });
        }

        public static void LogWarning(Exception ex)
        {
            LogWarning(ex, null);
        }

        public static void LogWarning(Exception ex, string metadata)
        {
            if (ex == null)
            {
                return;
            }

            _logServices.ForEach((item) =>
            {
                item.RegisterApplicationWarning(ex, metadata);
            });
        }
    }
}
