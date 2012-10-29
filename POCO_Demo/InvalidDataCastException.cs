using System;

namespace POCO_Demo
{
    public class InvalidDataCastException : Exception
    {
        public InvalidDataCastException(Type from, Type to, Exception innerException = null):base(null, innerException)
        {
            _from = from;
            _to = to;
        }

        public int? ColumnOrdinal { get; set; }
        public string ColumnName { get; set; }
        
        public override string Message
        {
            get
            {
                var msg = string.Format("Cannot cast from {0} to {1}.", _from.FullName, _to.FullName);
                if (ColumnOrdinal.HasValue)
                    msg += " Ordinal: " + ColumnOrdinal + ".";
                if (!string.IsNullOrEmpty(ColumnName))
                    msg += " Name: " + ColumnName + ".";

                return msg;
            }
        }

        #region Non-Public
        private Type _from;
        private Type _to;
        private string _ordinal;
        private string _name;
        #endregion
    }
}
