using System;

namespace MechAffinity.Data
{
    internal class DeferringLogger
    {

        private readonly Logger logger;
        private bool isDebug = false;
        
        public DeferringLogger(string modDir, string fileName, bool enableDebug)
        {

            logger = new Logger(modDir, fileName);
            isDebug = enableDebug;
        }

        public void setDebug(bool debug)
        {
            isDebug = debug;
        }
        
        public Nullable<Logger> Debug
        {
            get { return isDebug ? (Nullable<Logger>)logger : null; }
            private set { }
        }

        public Nullable<Logger> Info
        {
            get { return (Nullable<Logger>)logger; }
            private set { }
        }

        public Nullable<Logger> Warn
        {
            get 
            {
                return (Nullable<Logger>)logger;
            }
            private set { }
        }

        public Nullable<Logger> Error
        {
            get
            {
                return (Nullable<Logger>)logger;
            }
            private set { }
        }
    }
}