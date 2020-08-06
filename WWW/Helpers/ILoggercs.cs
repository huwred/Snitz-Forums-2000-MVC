using System;
using log4net;

namespace WWW.Helpers
{
    public interface ILogger
    {
        void Error(Exception ex);
        void Info(object msg);
        void Debug(string msg);
        void Error(string msg, Exception ex);
    }

    public class Logger : ILogger
    {
        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Error(Exception ex)
        {
            _log.Error(ex);
        }

        void ILogger.Info(object msg)
        {
            _log.Info(msg);
        }

        public void Debug(string msg)
        {
            _log.Debug(msg);
        }

        public void Error(string msg, Exception ex)
        {
            _log.Error(msg,ex);
        }
    }
}
