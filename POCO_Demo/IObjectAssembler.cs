using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using EF.Frameworks.Common.ConfigurationEF;
using EF.Frameworks.Common.DataEF.SqlClientEF;

namespace POCO_Demo
{
    public interface IObjectAssembler<T>
    {
        T Create(SqlDataReader dr);
        T Create(IConfigurationContext config, IEFSqlCommand cmd);
        IEnumerable<T> CreateList(SqlDataReader dr);
        IEnumerable<T> CreateList(IConfigurationContext config, IEFSqlCommand cmd);
    }
}
